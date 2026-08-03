using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ITHunterview.Service.Config;
using ITHunterview.Service.Constant.Prompts;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Exceptions;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;

namespace ITHunterview.Service.Service.Matching
{
    public class CvTextExtractorService : ICvTextExtractorService
    {
        private readonly ILogger<CvTextExtractorService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptions<AiSettings> _settings;
        private readonly IAiService _aiService;
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly IPromptManagementService _promptManagementService;
        private readonly ICvAnalysisResponseValidator _cvAnalysisResponseValidator;

        public CvTextExtractorService(
            ILogger<CvTextExtractorService> logger, 
            IHttpClientFactory httpClientFactory,
            IOptions<AiSettings> settings,
            IAiService aiService,
            ISystemConfigRepository systemConfigRepository,
            IPromptManagementService promptManagementService,
            ICvAnalysisResponseValidator cvAnalysisResponseValidator)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _settings = settings;
            _aiService = aiService;
            _systemConfigRepository = systemConfigRepository;
            _promptManagementService = promptManagementService;
            _cvAnalysisResponseValidator = cvAnalysisResponseValidator;
        }

        private bool IsTextGarbage(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return true;
            if (text.Length > 500 && !text.Contains("\n")) return true;
            var whitespaceRatio = text.Count(char.IsWhiteSpace) / (double)Math.Max(1, text.Length);
            if (whitespaceRatio < 0.05) return true;
            return false;
        }

        public Task<string> ExtractTextFromUrlAsync(string fileUrl)
            => ExtractTextFromUrlAsync(fileUrl, CancellationToken.None);

        public async Task<string> ExtractTextFromUrlAsync(string fileUrl, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(fileUrl)) return string.Empty;

            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                using var response = await client.GetAsync(fileUrl, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to download file from Cloudinary (Status: {response.StatusCode}).");
                }

                var contentType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant() ?? "";
                var fileBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

                return await ExtractTextInternalAsync(fileBytes, contentType, fileUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Failed to extract text from CV source. SourceHash={SourceHash}; ErrorType={ErrorType}",
                    HashIdentifier(fileUrl),
                    ex.GetType().Name);
                throw; 
            }
        }

        public Task<string> ExtractTextFromBytesAsync(byte[] fileBytes, string contentType, string fileName)
            => ExtractTextFromBytesAsync(fileBytes, contentType, fileName, CancellationToken.None);

        public async Task<string> ExtractTextFromBytesAsync(byte[] fileBytes, string contentType, string fileName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (fileBytes == null || fileBytes.Length == 0) return string.Empty;
            
            try
            {
                return await ExtractTextInternalAsync(fileBytes, contentType, fileName, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Failed to extract text from file bytes. SourceHash={SourceHash}; ErrorType={ErrorType}",
                    HashIdentifier(fileName),
                    ex.GetType().Name);
                throw;
            }
        }

        public Task<string> ExtractParsedDataFromBytesAsync(byte[] fileBytes, string contentType, string fileName)
            => ExtractParsedDataFromBytesAsync(fileBytes, contentType, fileName, CancellationToken.None);

        public async Task<string> ExtractParsedDataFromBytesAsync(byte[] fileBytes, string contentType, string fileName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (fileBytes == null || fileBytes.Length == 0) return string.Empty;

            var fileType = DetermineFileType(contentType, fileName);
            
            if (fileType == "pdf")
            {
                // Ưu tiên trích xuất Raw Text bằng PdfPig trước
                var rawText = SafeExtractPdf(fileBytes, fileName);
                
                if (!string.IsNullOrWhiteSpace(rawText) && !IsTextGarbage(rawText))
                {
                    _logger.LogInformation("Successfully extracted clean text from PDF using PdfPig. Calling Gemini Text API.");
                    return await ExtractJsonWithGeminiTextAsync(rawText, "pdf_text", fileName, cancellationToken);
                }

                _logger.LogWarning("PdfPig extracted garbage or empty text. PDF is likely a scanned image. Falling back to Gemini Vision OCR.");
                
                // Mặc định ném PDF vào Gemini Vision để lấy thẳng JSON (ParsedData)
                // Vision produces OCR text only. Parsed JSON always follows the same
                // active prompt and typed-validation path as every other CV source.
                var ocrText = await ExtractWithGeminiVisionAsync(fileBytes, "application/pdf", cancellationToken: cancellationToken);
                if (!string.IsNullOrWhiteSpace(ocrText) && !IsTextGarbage(ocrText))
                {
                    return await ExtractJsonWithGeminiTextAsync(ocrText, "ocr", fileName, cancellationToken);
                }

                _logger.LogWarning("Gemini Vision failed to return usable OCR text for scanned PDF.");
                return string.Empty;
            }
            else // docx, image, unknown
            {
                // Vẫn dùng C# lấy RawText trước vì Vision không hoạt động với DOCX
                var rawText = await ExtractTextInternalAsync(fileBytes, contentType, fileName, cancellationToken);
                if (string.IsNullOrWhiteSpace(rawText)) return string.Empty;

                return await ExtractJsonWithGeminiTextAsync(rawText, fileType == "docx" ? "docx_text" : "ocr", fileName, cancellationToken);
            }
        }

        public Task<string> ExtractParsedDataFromUrlAsync(string fileUrl, string rawTextFallback)
            => ExtractParsedDataFromUrlAsync(fileUrl, rawTextFallback, CancellationToken.None);

        public async Task<string> ExtractParsedDataFromUrlAsync(string fileUrl, string rawTextFallback, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(fileUrl)) return string.Empty;

            // FAST PATH: Nếu đã có Text sạch từ quá trình Upload, gọi Gemini Text ngay lập tức!
            if (!string.IsNullOrWhiteSpace(rawTextFallback) && !IsTextGarbage(rawTextFallback))
            {
                _logger.LogInformation("Fast path: Using provided RawTextFallback. SourceHash={SourceHash}; skipping download and Vision OCR.", HashIdentifier(fileUrl));
                return await ExtractJsonWithGeminiTextAsync(rawTextFallback, SourceTypeFromIdentifier(fileUrl), fileUrl, cancellationToken);
            }

            byte[]? fileBytes = null;
            var contentType = string.Empty;

            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                using var response = await client.GetAsync(fileUrl, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    contentType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant() ?? string.Empty;
                    fileBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                }
                else
                {
                    _logger.LogWarning(
                        "CV source download rejected. SourceHash={SourceHash}; StatusCode={StatusCode}",
                        HashIdentifier(fileUrl),
                        (int)response.StatusCode);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "CV source download timed out. SourceHash={SourceHash}",
                    HashIdentifier(fileUrl));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(
                    "CV source download failed. SourceHash={SourceHash}; ErrorType={ErrorType}",
                    HashIdentifier(fileUrl),
                    ex.GetType().Name);
            }

            if (fileBytes is { Length: > 0 })
            {
                var parsed = await ExtractParsedDataFromBytesAsync(
                    fileBytes,
                    contentType,
                    fileUrl,
                    cancellationToken);
                if (!string.IsNullOrWhiteSpace(parsed)) return parsed;
            }

            if (!string.IsNullOrWhiteSpace(rawTextFallback) && !IsTextGarbage(rawTextFallback))
            {
                return await ExtractJsonWithGeminiTextAsync(rawTextFallback, SourceTypeFromIdentifier(fileUrl), fileUrl, cancellationToken);
            }

            return string.Empty;
        }

        public Task<string> ExtractParsedDataFromRawTextAsync(string rawText, string sourceType = "pasted_text", string? fileName = null)
            => ExtractParsedDataFromRawTextAsync(rawText, sourceType, fileName, CancellationToken.None);

        public async Task<string> ExtractParsedDataFromRawTextAsync(string rawText, string sourceType, string? fileName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(rawText) || IsTextGarbage(rawText))
                return string.Empty;

            return await ExtractJsonWithGeminiTextAsync(rawText, sourceType, fileName, cancellationToken);
        }

        private async Task<string> ExtractJsonWithGeminiTextAsync(string rawCvText, string sourceType, string? fileName, CancellationToken cancellationToken)
        {
            var prompts = await GetCvPromptPairAsync();
            var input = new CvAnalysisInputSnapshot(
                rawCvText,
                sourceType,
                fileName,
                DateOnly.FromDateTime(DateTime.UtcNow));
            var userPrompt = BuildUserPrompt(prompts.User.Content, SerializeInput(input));
            var provider = await _aiService.GetActiveProviderNameAsync();
            var aiResponse = await _aiService.GenerateTextAsync(
                userPrompt,
                prompts.System.Content,
                provider,
                AiGenerationOptions.CvAnalysisJsonExtraction,
                cancellationToken);
            var validation = _cvAnalysisResponseValidator.ValidateAndCanonicalize(NormalizeJsonResponse(aiResponse), input);
            if (validation.IsValid)
            {
                return validation.CanonicalJson;
            }

            _logger.LogWarning(
                "CV analysis response rejected. Provider={Provider}; SourceType={SourceType}; InputLength={InputLength}; " +
                "ResponseLength={ResponseLength}; ResponseHash={ResponseHash}; FailureCode={FailureCode}; " +
                "DiagnosticCode={DiagnosticCode}; JsonPath={JsonPath}",
                provider,
                sourceType,
                rawCvText.Length,
                aiResponse?.Length ?? 0,
                HashIdentifier(aiResponse),
                validation.FailureCode,
                validation.DiagnosticCode,
                validation.JsonPath);
            throw new CvAnalysisValidationException(validation);
        }

        private static string SerializeInput(CvAnalysisInputSnapshot input) => JsonSerializer.Serialize(new
        {
            raw_text = input.RawText,
            source_type = input.SourceType,
            file_name = input.FileName ?? string.Empty,
            analysis_date = input.AnalysisDate.ToString("yyyy-MM-dd")
        });

        private static string StripMarkdownFence(string? value)
        {
            var jsonString = value ?? string.Empty;
            if (jsonString.Contains("```json", StringComparison.OrdinalIgnoreCase))
            {
                var start = jsonString.IndexOf("```json", StringComparison.OrdinalIgnoreCase) + 7;
                var end = jsonString.LastIndexOf("```", StringComparison.Ordinal);
                if (end > start) return jsonString[start..end].Trim();
            }
            else if (jsonString.Contains("```", StringComparison.Ordinal))
            {
                var start = jsonString.IndexOf("```", StringComparison.Ordinal) + 3;
                var end = jsonString.LastIndexOf("```", StringComparison.Ordinal);
                if (end > start) return jsonString[start..end].Trim();
            }

            return jsonString.Trim();
        }

        private static string NormalizeJsonResponse(string? value)
        {
            var candidate = StripMarkdownFence(value);
            if (candidate.Length == 0 || candidate[0] is '{' or '[')
            {
                return candidate;
            }

            return TryExtractBalancedJsonObject(candidate, out var jsonObject)
                ? jsonObject
                : candidate;
        }

        private static bool TryExtractBalancedJsonObject(string value, out string jsonObject)
        {
            jsonObject = string.Empty;
            var start = value.IndexOf('{');
            if (start < 0) return false;

            var depth = 0;
            var inString = false;
            var escaping = false;

            for (var index = start; index < value.Length; index++)
            {
                var current = value[index];
                if (inString)
                {
                    if (escaping)
                    {
                        escaping = false;
                    }
                    else if (current == '\\')
                    {
                        escaping = true;
                    }
                    else if (current == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (current == '"')
                {
                    inString = true;
                }
                else if (current == '{')
                {
                    depth++;
                }
                else if (current == '}' && --depth == 0)
                {
                    jsonObject = value[start..(index + 1)];
                    return true;
                }
            }

            return false;
        }

        private string SourceTypeFromIdentifier(string identifier)
        {
            return DetermineFileType(string.Empty, identifier) switch
            {
                "pdf" => "pdf_text",
                "docx" => "docx_text",
                _ => "ocr"
            };
        }

        private static string HashIdentifier(string? value)
        {
            var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            return Convert.ToHexString(SHA256.HashData(bytes))[..16].ToLowerInvariant();
        }

        private async Task<PromptPairSnapshotDto> GetCvPromptPairAsync()
        {
            var prompts = await _promptManagementService.GetActivePromptPairSnapshotAsync(
                CvAnalysisPromptContract.SystemPromptKey,
                CvAnalysisPromptContract.UserPromptKey);

            _logger.LogInformation(
                "Using CV analysis prompt pair. System={SystemPromptKey}:{SystemVersionTag} ({SystemVersionId}); User={UserPromptKey}:{UserVersionTag} ({UserVersionId}); Contract={Contract}",
                prompts.System.PromptKey,
                prompts.System.VersionTag,
                prompts.System.VersionId,
                prompts.User.PromptKey,
                prompts.User.VersionTag,
                prompts.User.VersionId,
                prompts.Contract);

            return prompts;
        }

        private static string BuildUserPrompt(string userPromptTemplate, string cvText)
        {
            if (string.IsNullOrWhiteSpace(userPromptTemplate) ||
                !userPromptTemplate.Contains(CvAnalysisPromptContract.UserPlaceholder, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("PROMPT_CONFIGURATION_INVALID: CV analysis user prompt is missing [CV_TEXT].");
            }

            return userPromptTemplate.Replace(CvAnalysisPromptContract.UserPlaceholder, cvText, StringComparison.Ordinal);
        }

        private async Task<string> ExtractTextInternalAsync(
            byte[] fileBytes,
            string contentType,
            string identifier,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fileType = DetermineFileType(contentType, identifier);
            _logger.LogInformation("Bắt đầu ExtractTextInternalAsync. SourceHash={SourceHash}; ContentType={ContentType}; FileType={FileType}", HashIdentifier(identifier), contentType, fileType);
            
            var extracted = string.Empty;

            try
            {
                if (fileType == "pdf")
                {
                    _logger.LogInformation("Gọi SafeExtractPdf cho SourceHash={SourceHash}...", HashIdentifier(identifier));
                    extracted = SafeExtractPdf(fileBytes, identifier);
                    _logger.LogInformation("SafeExtractPdf hoàn thành. Độ dài raw text: {Length}", extracted?.Length ?? 0);
                }
                else if (fileType == "docx")
                {
                    _logger.LogInformation("Gọi SafeExtractDocx cho SourceHash={SourceHash}...", HashIdentifier(identifier));
                    extracted = SafeExtractDocx(fileBytes, identifier);
                    _logger.LogInformation("SafeExtractDocx hoàn thành. Độ dài raw text: {Length}", extracted?.Length ?? 0);
                }
                else
                {
                    _logger.LogWarning("Loại file không được hỗ trợ bởi C# extractor ({FileType}). Không thể lấy RawText.", fileType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "Lỗi khi dùng thư viện C# extract cho file SourceHash={SourceHash}. ErrorType={ErrorType}",
                    HashIdentifier(identifier),
                    ex.GetType().Name);
            }

            if (string.IsNullOrWhiteSpace(extracted) || IsTextGarbage(extracted))
            {
                _logger.LogWarning("Kết quả extract bị rỗng hoặc là rác cho SourceHash={SourceHash}. Trả về chuỗi rỗng.", HashIdentifier(identifier));
                return string.Empty;
            }

            _logger.LogInformation("ExtractTextInternalAsync thành công cho SourceHash={SourceHash}. Trả về {Length} ký tự.", HashIdentifier(identifier), extracted.Length);
            return extracted ?? string.Empty;
        }

        private string DetermineFileType(string contentType, string fileUrl)
        {
            if (contentType.Contains("pdf")) return "pdf";
            if (contentType.Contains("word") || contentType.Contains("openxmlformats")) return "docx";
            if (contentType.StartsWith("image/")) return "image";
            if (contentType.StartsWith("video/")) return "unsupported";

            try
            {
                var urlLower = fileUrl.ToLowerInvariant();
                // Check if it's a valid URI first to strip query parameters if any (for Cloudinary URLs)
                if (Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri))
                {
                    urlLower = uri.AbsolutePath.ToLowerInvariant();
                }

                if (urlLower.EndsWith(".pdf")) return "pdf";
                if (urlLower.EndsWith(".docx") || urlLower.EndsWith(".doc")) return "docx";
                if (urlLower.EndsWith(".jpg") || urlLower.EndsWith(".jpeg") || urlLower.EndsWith(".png") || urlLower.EndsWith(".webp")) return "image";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "Failed to determine CV file type. SourceHash={SourceHash}; ErrorType={ErrorType}",
                    HashIdentifier(fileUrl),
                    ex.GetType().Name);
            }

            return "unknown";
        }

        private string SafeExtractPdf(byte[] fileBytes, string fileUrl)
        {
            if (fileBytes.Length < 4 || fileBytes[0] != 0x25 || fileBytes[1] != 0x50 || fileBytes[2] != 0x44 || fileBytes[3] != 0x46)
            {
                _logger.LogWarning(
                    "File does not appear to be a valid PDF (missing magic bytes). SourceHash={SourceHash}",
                    HashIdentifier(fileUrl));
                return string.Empty;
            }
            
            try 
            {
                var textBuilder = new StringBuilder();
                using var document = PdfDocument.Open(fileBytes);
                foreach (var page in document.GetPages())
                {
                    var pageText = UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor.ContentOrderTextExtractor.GetText(page);
                    textBuilder.AppendLine(pageText);
                }
                return textBuilder.ToString().Trim();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "PdfPig failed to extract PDF content. Falling back to OCR. SourceHash={SourceHash}; ErrorType={ErrorType}",
                    HashIdentifier(fileUrl),
                    ex.GetType().Name);
                return string.Empty;
            }
        }

        private string SafeExtractDocx(byte[] fileBytes, string fileUrl)
        {
            try
            {
                var textBuilder = new StringBuilder();
                using var stream = new MemoryStream(fileBytes);
                using var wordDocument = WordprocessingDocument.Open(stream, false);
                var body = wordDocument.MainDocumentPart?.Document.Body;
                if (body != null)
                {
                    foreach (var para in body.Elements<Paragraph>())
                    {
                        textBuilder.AppendLine(para.InnerText);
                    }
                }
                return textBuilder.ToString().Trim();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "Failed to extract DOCX content. SourceHash={SourceHash}; ErrorType={ErrorType}",
                    HashIdentifier(fileUrl),
                    ex.GetType().Name);
                return string.Empty;
            }
        }

        private async Task<string> ExtractWithGeminiVisionAsync(
            byte[] fileBytes,
            string mimeType,
            string customPrompt = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var config = _settings.Value.Providers.TryGetValue("Gemini", out var c) ? c : new ProviderConfig();
                var dbKeyConfig = await _systemConfigRepository.GetByKeyAsync("AiApiKey_Gemini");
                var apiKey = dbKeyConfig?.ConfigValue ?? config.ApiKey;

                if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_GEMINI_API_KEY")
                {
                    _logger.LogWarning("Gemini API Key is not configured in DB or settings. Cannot perform OCR.");
                    return string.Empty;
                }

                var model = string.IsNullOrEmpty(config.Model) ? "gemini-flash-latest" : config.Model;
                var baseEndpoint = string.IsNullOrEmpty(config.Endpoint)
                    ? "https://generativelanguage.googleapis.com/v1beta/models"
                    : config.Endpoint.TrimEnd('/');
                    
                var endpoint = $"{baseEndpoint}/{model}:generateContent?key={apiKey}";

                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(45);
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint);

                var base64Data = Convert.ToBase64String(fileBytes);
                var textPrompt = customPrompt ?? "Extract all text from this resume/CV. Return ONLY the raw text exactly as it appears. Do not add any conversational filler, markdown formatting blocks, or comments.";
                
                var payload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new { text = textPrompt },
                                new
                                {
                                    inlineData = new
                                    {
                                        mimeType = mimeType,
                                        data = base64Data
                                    }
                                }
                            }
                        }
                    }
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                requestMessage.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                using var response = await client.SendAsync(
                    requestMessage,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await BoundedHttpContentReader.ReadAsStringAsync(
                        response.Content,
                        BoundedHttpContentReader.DefaultMaxBytes,
                        cancellationToken);
                    _logger.LogError(
                        "Gemini Vision API call failed: StatusCode={StatusCode}; ErrorBodyLength={ErrorBodyLength}",
                        response.StatusCode,
                        errorContent.Length);
                    return string.Empty;
                }

                var responseContent = await BoundedHttpContentReader.ReadAsStringAsync(
                    response.Content,
                    BoundedHttpContentReader.DefaultMaxBytes,
                    cancellationToken);
                using var doc = JsonDocument.Parse(responseContent);

                if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                    candidates.GetArrayLength() > 0 &&
                    candidates[0].TryGetProperty("content", out var content) &&
                    content.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0 &&
                    parts[0].TryGetProperty("text", out var text))
                {
                    var result = text.GetString()?.Trim() ?? string.Empty;
                    
                    // Nhanh chóng loại bỏ mác JSON markdown nếu LLM trả về thừa
                    if (result.Contains("```json"))
                    {
                        int start = result.IndexOf("```json") + 7;
                        int end = result.LastIndexOf("```");
                        if (end > start) result = result.Substring(start, end - start);
                    }
                    else if (result.Contains("```"))
                    {
                        int start = result.IndexOf("```") + 3;
                        int end = result.LastIndexOf("```");
                        if (end > start) result = result.Substring(start, end - start);
                    }
                    
                    return result.Trim();
                }

                return string.Empty;
            }
            catch (InvalidOperationException ex) when (ex.Message == "AI_RESPONSE_TOO_LARGE")
            {
                throw;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Exception in ExtractWithGeminiVisionAsync. ErrorType={ErrorType}",
                    ex.GetType().Name);
                return string.Empty;
            }
        }

        public List<CvChunkDto> ChunkCvText(string rawCvText)
        {
            throw new NotImplementedException("Giai đoạn 3: Implement logic chia đoạn text CV.");
        }
    }
}
