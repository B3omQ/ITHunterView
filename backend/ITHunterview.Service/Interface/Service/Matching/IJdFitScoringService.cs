using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.Service.Matching
{
    public interface IJdFitScoringService
    {
        Task<List<RequirementScoreDto>> ScoreRequirementsAsync(
            JdExtractionResultDto jdData,
            Dictionary<string, List<CvChunkDto>> topChunksPerReq);

        Task<List<PenaltyResultDto>> CheckPenaltiesAsync(
            string rawCvText,
            JdExtractionResultDto jdData);
    }
}
