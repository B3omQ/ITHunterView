using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Service.Matching;
using Microsoft.Extensions.Logging;

namespace ITHunterview.Service.Service.Matching
{
    public class JdFitScoringService : IJdFitScoringService
    {
        private readonly ILogger<JdFitScoringService> _logger;

        public JdFitScoringService(ILogger<JdFitScoringService> logger)
        {
            _logger = logger;
        }

        public Task<List<RequirementScoreDto>> ScoreRequirementsAsync(
            JdExtractionResultDto jdData,
            Dictionary<string, List<CvChunkDto>> topChunksPerReq)
        {
            throw new NotImplementedException("Giai đoạn 3: Gọi LLM Judge.");
        }

        public Task<List<PenaltyResultDto>> CheckPenaltiesAsync(
            string rawCvText,
            JdExtractionResultDto jdData)
        {
            throw new NotImplementedException("Giai đoạn 3: Gọi LLM Penalty Check.");
        }
    }
}
