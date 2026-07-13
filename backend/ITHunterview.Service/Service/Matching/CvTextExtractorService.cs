using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using ITHunterview.Service.Config;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Service.Matching;
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

        public CvTextExtractorService(
            ILogger<CvTextExtractorService> logger, 
            IHttpClientFactory httpClientFactory,
            IOptions<AiSettings> settings)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _settings = settings;
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
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to download file from Cloudinary (Status: {response.StatusCode}). Details: {errorDetails}");
                }

                var contentType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant() ?? "";
                var fileBytes = await response.Content.ReadAsByteArrayAsync();

                var fileType = DetermineFileType(contentType, fileUrl);

                _logger.LogInformation("Extracting text from file. ContentType={ContentType}, DetectedType={Type}, URL={Url}", contentType, fileType, fileUrl);

                var extracted = string.Empty;

                if (fileType == "pdf")
                {
                    extracted = SafeExtractPdf(fileBytes, fileUrl);
                }
                else if (fileType == "docx")
                {
                    extracted = SafeExtractDocx(fileBytes, fileUrl);
                }

                // If PdfPig fails (empty) or it's an image, fallback to Gemini OCR
                if (string.IsNullOrWhiteSpace(extracted) && (fileType == "pdf" || fileType == "image" || fileType == "unknown"))
                {
                    var mimeTypeForGemini = fileType == "pdf" ? "application/pdf" : contentType;
                    if (string.IsNullOrEmpty(mimeTypeForGemini) || !mimeTypeForGemini.Contains("/")) 
                        mimeTypeForGemini = "image/jpeg"; // default fallback

                    _logger.LogInformation("Falling back to Gemini Vision OCR for URL: {Url}", fileUrl);
                    extracted = await ExtractTextWithGeminiVisionAsync(fileBytes, mimeTypeForGemini);
                }

                if (string.IsNullOrWhiteSpace(extracted)) {
                    throw new Exception("File was downloaded but text extraction resulted in empty string (possibly image-based PDF without text layer or invalid format). OCR fallback also failed.");
                }
                
                return extracted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract text from URL: {Url}", fileUrl);
                throw; 
            }
        }

        private string DetermineFileType(string contentType, string fileUrl)
        {
            if (contentType.Contains("pdf")) return "pdf";
            if (contentType.Contains("word") || contentType.Contains("openxmlformats")) return "docx";
            if (contentType.StartsWith("image/")) return "image";

            if (contentType.StartsWith("video/"))
            {
                _logger.LogWarning("URL returned video content type: {ContentType}, URL: {Url}", contentType, fileUrl);
                return "unsupported";
            }

            try
            {
                var path = new Uri(fileUrl).AbsolutePath.ToLowerInvariant();
                if (path.EndsWith(".pdf")) return "pdf";
                if (path.EndsWith(".docx") || path.EndsWith(".doc")) return "docx";
                if (path.EndsWith(".jpg") || path.EndsWith(".jpeg") || path.EndsWith(".png") || path.EndsWith(".webp")) return "image";
            }
            catch { }

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
                return ExtractTextFromPdf(fileBytes);
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
                return ExtractTextFromDocx(fileBytes);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract DOCX content. URL: {Url}", fileUrl);
                return string.Empty;
            }
        }

        private string ExtractTextFromPdf(byte[] pdfBytes)
        {
            var textBuilder = new StringBuilder();
            using var document = PdfDocument.Open(pdfBytes);
            foreach (var page in document.GetPages())
            {
                textBuilder.AppendLine(page.Text);
            }
            return textBuilder.ToString().Trim();
        }

        private string ExtractTextFromDocx(byte[] docxBytes)
        {
            var textBuilder = new StringBuilder();
            using var stream = new MemoryStream(docxBytes);
            using var wordDocument = WordprocessingDocument.Open(stream, false);
            var body = wordDocument.MainDocumentPart?.Document.Body;
            if (body != null)
            {
                textBuilder.AppendLine(body.InnerText);
            }
            return textBuilder.ToString().Trim();
        }

        private async Task<string> ExtractTextWithGeminiVisionAsync(byte[] fileBytes, string mimeType)
        {
            var config = _settings.Value.Providers.TryGetValue("Gemini", out var c) ? c : new ProviderConfig();
            if (string.IsNullOrEmpty(config.ApiKey) || config.ApiKey == "YOUR_GEMINI_API_KEY")
            {
                _logger.LogWarning("Gemini API Key is not configured. Cannot perform OCR.");
                return string.Empty;
            }

            // Using the same default logic as GeminiProvider
            var model = string.IsNullOrEmpty(config.Model) ? "gemini-flash-latest" : config.Model;
            var baseEndpoint = string.IsNullOrEmpty(config.Endpoint)
                ? "https://generativelanguage.googleapis.com/v1beta/models"
                : config.Endpoint.TrimEnd('/');
                
            var endpoint = $"{baseEndpoint}/{model}:generateContent?key={config.ApiKey}";

            using var client = _httpClientFactory.CreateClient();
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint);

            var base64Data = Convert.ToBase64String(fileBytes);
            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = "Extract all text from this resume/CV. Return ONLY the raw text exactly as it appears. Do not add any conversational filler, markdown formatting blocks, or comments." },
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
                return text.GetString()?.Trim() ?? string.Empty;
            }

            return string.Empty;
        }

        public List<CvChunkDto> ChunkCvText(string rawCvText)
        {
            throw new NotImplementedException("Giai đoạn 3: Implement logic chia đoạn text CV.");
        }
    }
}
