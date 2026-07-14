using System;

namespace ITHunterview.Service.DTOs.LearningPath
{
    public class GenerateFromInterviewRequestDto
    {
        public Guid? SessionId { get; set; }
        public int TimeframeInWeeks { get; set; } = 12;
    }
}
