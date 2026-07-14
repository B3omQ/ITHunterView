using System;

namespace ITHunterview.Service.DTOs.LearningPath
{
    public class GenerateFromCvJdRequestDto
    {
        public Guid? MatchScoreId { get; set; }
        public int TimeframeInWeeks { get; set; } = 12;
    }
}
