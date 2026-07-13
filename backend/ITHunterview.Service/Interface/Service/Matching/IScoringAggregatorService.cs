using System.Collections.Generic;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.Service.Matching
{
    public interface IScoringAggregatorService
    {
        JdFitResultDto AggregateJdFit(
            JdExtractionResultDto jdData,
            List<RequirementScoreDto> requirementScores,
            List<PenaltyResultDto> penalties);
    }
}
