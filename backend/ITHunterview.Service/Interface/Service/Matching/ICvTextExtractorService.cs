using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.Service.Matching
{
    public interface ICvTextExtractorService
    {
        Task<string> ExtractTextFromUrlAsync(string fileUrl);
        Task<string> ExtractTextFromBytesAsync(byte[] fileBytes, string contentType, string fileName);
        
        Task<string> ExtractParsedDataFromBytesAsync(byte[] fileBytes, string contentType, string fileName);
        Task<string> ExtractParsedDataFromUrlAsync(string fileUrl, string rawTextFallback);
        Task<string> ExtractParsedDataFromRawTextAsync(string rawText, string sourceType = "pasted_text", string? fileName = null);
        
        List<CvChunkDto> ChunkCvText(string rawCvText);
    }
}
