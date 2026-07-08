using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.Service.Matching
{
    public interface ICvQualityScoringService
    {
        Task<CvQualityResultDto> ScoreAsync(
            string rawCvText,
            List<CvChunkDto> cvChunks);
    }
}
