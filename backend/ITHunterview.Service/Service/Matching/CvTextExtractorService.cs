using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ITHunterview.Service.Config;
using ITHunterview.Service.Constant.Prompts;
using ITHunterview.Service.DTOs.Cv.Matching;
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

        public CvTextExtractorService(
            ILogger<CvTextExtractorService> logger, 
            IHttpClientFactory httpClientFactory,
            IOptions<AiSettings> settings,
            IAiService aiService,
            ISystemConfigRepository systemConfigRepository)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _settings = settings;
            _aiService = aiService;
            _systemConfigRepository = systemConfigRepository;
        }

        private bool IsTextGarbage(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return true;
            if (text.Length > 500 && !text.Contains("\n")) return true;
            var whitespaceRatio = text.Count(char.IsWhiteSpace) / (double)Math.Max(1, text.Length);
            if (whitespaceRatio < 0.05) return true;
            return false;
        }

        public async Task<string> ExtractTextFromUrlAsync(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl)) return string.Empty;

            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                using var response = await client.GetAsync(fileUrl);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to download file from Cloudinary (Status: {response.StatusCode}).");
                }

                var contentType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant() ?? "";
                var fileBytes = await response.Content.ReadAsByteArrayAsync();

                return await ExtractTextInternalAsync(fileBytes, contentType, fileUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract text from URL: {Url}", fileUrl);
                throw; 
            }
        }

        public async Task<string> ExtractTextFromBytesAsync(byte[] fileBytes, string contentType, string fileName)
        {
            if (fileBytes == null || fileBytes.Length == 0) return string.Empty;
            
            try
            {
                return await ExtractTextInternalAsync(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract text from file bytes: {FileName}", fileName);
                throw;
            }
        }

        public async Task<string> ExtractParsedDataFromBytesAsync(byte[] fileBytes, string contentType, string fileName)
        {
            if (fileBytes == null || fileBytes.Length == 0) return string.Empty;

            var fileType = DetermineFileType(contentType, fileName);
            
            if (fileType == "pdf")
            {
                // Ưu tiên trích xuất Raw Text bằng PdfPig trước
                var rawText = SafeExtractPdf(fileBytes, fileName);
                
                if (!string.IsNullOrWhiteSpace(rawText) && !IsTextGarbage(rawText))
                {
                    _logger.LogInformation("Successfully extracted clean text from PDF using PdfPig. Calling Gemini Text API.");
                    var textPrompt = CvParsingPrompt.GetPrompt(rawText);
                    return await ExtractJsonWithGeminiTextAsync(textPrompt);
                }

                _logger.LogWarning("PdfPig extracted garbage or empty text. PDF is likely a scanned image. Falling back to Gemini Vision OCR.");
                
                // Mặc định ném PDF vào Gemini Vision để lấy thẳng JSON (ParsedData)
                var prompt = CvParsingPrompt.SystemPrompt + "\n\nExtract the CV into the required JSON format directly from the provided document.";
                var json = await ExtractWithGeminiVisionAsync(fileBytes, "application/pdf", prompt);
                
                try
                {
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        using (JsonDocument.Parse(json)) 
                        { 
                            return json; 
                        }
                    }
                }
                catch { }

                _logger.LogWarning("Gemini Vision failed to return valid JSON for Scanned PDF.");
                return string.Empty;
            }
            else // docx, image, unknown
            {
                // Vẫn dùng C# lấy RawText trước vì Vision không hoạt động với DOCX
                var rawText = await ExtractTextInternalAsync(fileBytes, contentType, fileName);
                if (string.IsNullOrWhiteSpace(rawText)) return string.Empty;

                var textPrompt = CvParsingPrompt.GetPrompt(rawText);
                return await ExtractJsonWithGeminiTextAsync(textPrompt);
            }
        }

        public async Task<string> ExtractParsedDataFromUrlAsync(string fileUrl, string rawTextFallback)
        {
            if (string.IsNullOrWhiteSpace(fileUrl)) return string.Empty;

            // FAST PATH: Nếu đã có Text sạch từ quá trình Upload, gọi Gemini Text ngay lập tức!
            if (!string.IsNullOrWhiteSpace(rawTextFallback) && !IsTextGarbage(rawTextFallback))
            {
                _logger.LogInformation("Fast path: Using provided RawTextFallback for {Url}. Skipping download and Vision OCR.", fileUrl);
                var textPrompt = CvParsingPrompt.GetPrompt(rawTextFallback);
                return await ExtractJsonWithGeminiTextAsync(textPrompt);
            }

            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                using var response = await client.GetAsync(fileUrl);

                if (response.IsSuccessStatusCode)
                {
                    var contentType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant() ?? "";
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();
                    
                    return await ExtractParsedDataFromBytesAsync(fileBytes, contentType, fileUrl);
                }
                else
                {
                    _logger.LogWarning("Failed to download file from URL for background parsing. Falling back to RawText.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error downloading file from URL. Falling back to RawText.");
            }

            // Fallback cuối cùng nếu không tải được File: Dùng RawText gọi Gemini Text
            if (!string.IsNullOrWhiteSpace(rawTextFallback))
            {
                var textPrompt = CvParsingPrompt.GetPrompt(rawTextFallback);
                return await ExtractJsonWithGeminiTextAsync(textPrompt);
            }

            return string.Empty;
        }

        public async Task<string> ExtractParsedDataFromRawTextAsync(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText) || IsTextGarbage(rawText))
                return string.Empty;

            var textPrompt = CvParsingPrompt.GetPrompt(rawText);
            return await ExtractJsonWithGeminiTextAsync(textPrompt);
        }

        private async Task<string> ExtractJsonWithGeminiTextAsync(string prompt)
        {
            var systemPrompt = CvParsingPrompt.SystemPrompt;
            var aiResponse = await _aiService.GenerateTextAsync(prompt, systemPrompt);

            string jsonString = aiResponse;
            if (jsonString.Contains("```json"))
            {
                int start = jsonString.IndexOf("```json") + 7;
                int end = jsonString.LastIndexOf("```");
                if (end > start) jsonString = jsonString.Substring(start, end - start);
            }
            else if (jsonString.Contains("```"))
            {
                int start = jsonString.IndexOf("```") + 3;
                int end = jsonString.LastIndexOf("```");
                if (end > start) jsonString = jsonString.Substring(start, end - start);
            }
            return jsonString.Trim();
        }

        private async Task<string> ExtractTextInternalAsync(byte[] fileBytes, string contentType, string identifier)
        {
            var fileType = DetermineFileType(contentType, identifier);
            _logger.LogInformation("Bắt đầu ExtractTextInternalAsync cho {Identifier}. ContentType: {ContentType}. Nhận diện loại file: {FileType}", identifier, contentType, fileType);
            
            var extracted = string.Empty;

            try
            {
                if (fileType == "pdf")
                {
                    _logger.LogInformation("Gọi SafeExtractPdf cho {Identifier}...", identifier);
                    extracted = SafeExtractPdf(fileBytes, identifier);
                    _logger.LogInformation("SafeExtractPdf hoàn thành. Độ dài raw text: {Length}", extracted?.Length ?? 0);
                }
                else if (fileType == "docx")
                {
                    _logger.LogInformation("Gọi SafeExtractDocx cho {Identifier}...", identifier);
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
                _logger.LogWarning(ex, "Lỗi khi dùng thư viện C# extract cho file {Identifier}. Error: {Message}", identifier, ex.Message);
            }

            if (string.IsNullOrWhiteSpace(extracted) || IsTextGarbage(extracted))
            {
                _logger.LogWarning("Kết quả extract bị rỗng hoặc là rác cho identifier {Identifier}. Trả về chuỗi rỗng.", identifier);
                return string.Empty;
            }

            _logger.LogInformation("ExtractTextInternalAsync thành công cho {Identifier}. Trả về {Length} ký tự.", identifier, extracted.Length);
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
                _logger.LogWarning(ex, "Failed to determine file type from URL: {Url}", fileUrl);
            }

            return "unknown";
        }

        private string SafeExtractPdf(byte[] fileBytes, string fileUrl)
        {
            if (fileBytes.Length < 4 || fileBytes[0] != 0x25 || fileBytes[1] != 0x50 || fileBytes[2] != 0x44 || fileBytes[3] != 0x46)
            {
                _logger.LogWarning("File does not appear to be a valid PDF (missing magic bytes). URL: {Url}", fileUrl);
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
                _logger.LogWarning(ex, "PdfPig failed to extract PDF content. Falling back to OCR. URL: {Url}", fileUrl);
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
                _logger.LogWarning(ex, "Failed to extract DOCX content. URL: {Url}", fileUrl);
                return string.Empty;
            }
        }

        private async Task<string> ExtractWithGeminiVisionAsync(byte[] fileBytes, string mimeType, string customPrompt = null)
        {
            try
            {
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

                var response = await client.SendAsync(requestMessage);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Gemini Vision API call failed: {StatusCode} {Error}", response.StatusCode, errorContent);
                    return string.Empty;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in ExtractWithGeminiVisionAsync");
                return string.Empty;
            }
        }

        public List<CvChunkDto> ChunkCvText(string rawCvText)
        {
            throw new NotImplementedException("Giai đoạn 3: Implement logic chia đoạn text CV.");
        }
    }
}
