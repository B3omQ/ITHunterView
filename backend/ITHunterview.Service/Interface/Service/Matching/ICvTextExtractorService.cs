using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.Service.Matching
{
    public interface ICvTextExtractorService
    {
        Task<string> ExtractTextFromUrlAsync(string fileUrl);
        List<CvChunkDto> ChunkCvText(string rawCvText);
    }
}
