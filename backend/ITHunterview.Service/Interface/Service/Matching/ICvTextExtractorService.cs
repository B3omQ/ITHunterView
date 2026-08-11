using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.Service.Matching
{
    public interface ICvTextExtractorService
    {
        Task<string> ExtractTextFromUrlAsync(string fileUrl);
        Task<string> ExtractTextFromUrlAsync(string fileUrl, CancellationToken cancellationToken);
        Task<string> ExtractTextFromBytesAsync(byte[] fileBytes, string contentType, string fileName);
        Task<string> ExtractTextFromBytesAsync(byte[] fileBytes, string contentType, string fileName, CancellationToken cancellationToken);
        
        Task<string> ExtractParsedDataFromBytesAsync(byte[] fileBytes, string contentType, string fileName);
        Task<string> ExtractParsedDataFromBytesAsync(byte[] fileBytes, string contentType, string fileName, CancellationToken cancellationToken);
        Task<string> ExtractParsedDataFromUrlAsync(string fileUrl, string rawTextFallback);
        Task<string> ExtractParsedDataFromUrlAsync(string fileUrl, string rawTextFallback, CancellationToken cancellationToken);
        Task<string> ExtractParsedDataFromRawTextAsync(string rawText, string sourceType = "pasted_text", string? fileName = null);
        Task<string> ExtractParsedDataFromRawTextAsync(string rawText, string sourceType, string? fileName, CancellationToken cancellationToken);
        
        List<CvChunkDto> ChunkCvText(string rawCvText);
    }
}
