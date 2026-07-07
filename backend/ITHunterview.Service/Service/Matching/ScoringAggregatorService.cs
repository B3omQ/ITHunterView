using System;
using System.Collections.Generic;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Service.Matching;
using Microsoft.Extensions.Logging;

namespace ITHunterview.Service.Service.Matching
{
    public class ScoringAggregatorService : IScoringAggregatorService
    {
        private readonly ILogger<ScoringAggregatorService> _logger;

        public ScoringAggregatorService(ILogger<ScoringAggregatorService> logger)
        {
            _logger = logger;
        }

        public JdFitResultDto AggregateJdFit(
            JdExtractionResultDto jdData,
            List<RequirementScoreDto> requirementScores,
            List<PenaltyResultDto> penalties)
        {
            throw new NotImplementedException("Giai đoạn 3: Implement C# logic tổng hợp điểm.");
        }
    }
}
