using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Service.Matching;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace ITHunterview.Service.Service.Matching
{
    public class CvTextExtractorService : ICvTextExtractorService
    {
        private readonly ILogger<CvTextExtractorService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public CvTextExtractorService(ILogger<CvTextExtractorService> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> ExtractTextFromUrlAsync(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl)) return string.Empty;

            try
            {
                // Sử dụng Uri thẳng, Cloudinary tự encode rồi.
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

                // Xác định loại file: ưu tiên Content-Type, sau đó mới dùng URL extension
                var fileType = DetermineFileType(contentType, fileUrl);

                _logger.LogInformation("Extracting text from file. ContentType={ContentType}, DetectedType={Type}, URL={Url}", contentType, fileType, fileUrl);

                var extracted = fileType switch
                {
                    "pdf" => SafeExtractPdf(fileBytes, fileUrl),
                    "docx" => SafeExtractDocx(fileBytes, fileUrl),
                    _ => throw new Exception($"Unsupported content type returned from Cloudinary: {contentType}. Cannot extract text.")
                };

                if (string.IsNullOrWhiteSpace(extracted)) {
                    throw new Exception("File was downloaded but text extraction resulted in empty string (possibly image-based PDF without text layer or invalid format).");
                }
                
                return extracted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract text from URL: {Url}", fileUrl);
                throw; // Ném thẳng ra ngoài để ProcessMatchingJobAsync hứng
            }
        }

        private string DetermineFileType(string contentType, string fileUrl)
        {
            // 1. Ưu tiên Content-Type nếu rõ ràng
            if (contentType.Contains("pdf")) return "pdf";
            if (contentType.Contains("word") || contentType.Contains("openxmlformats")) return "docx";

            // 2. Nếu Content-Type là image/unknown → không cố extract như document
            if (contentType.StartsWith("image/") || contentType.StartsWith("video/"))
            {
                _logger.LogWarning("URL returned non-document content type: {ContentType}, URL: {Url}", contentType, fileUrl);
                return "unsupported";
            }

            // 3. Fallback sang URL extension khi Content-Type không rõ (application/octet-stream)
            try
            {
                var path = new Uri(fileUrl).AbsolutePath.ToLowerInvariant();
                if (path.EndsWith(".pdf")) return "pdf";
                if (path.EndsWith(".docx") || path.EndsWith(".doc")) return "docx";
            }
            catch { }

            return "unknown";
        }

        private string SafeExtractPdf(byte[] fileBytes, string fileUrl)
        {
            // Kiểm tra PDF magic bytes: %PDF-
            if (fileBytes.Length < 4 || fileBytes[0] != 0x25 || fileBytes[1] != 0x50 || fileBytes[2] != 0x44 || fileBytes[3] != 0x46)
            {
                _logger.LogWarning("File does not appear to be a valid PDF (missing magic bytes). URL: {Url}", fileUrl);
                return string.Empty;
            }
            return ExtractTextFromPdf(fileBytes);
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

        public List<CvChunkDto> ChunkCvText(string rawCvText)
        {
            throw new NotImplementedException("Giai đoạn 3: Implement logic chia đoạn text CV.");
        }
    }
}
