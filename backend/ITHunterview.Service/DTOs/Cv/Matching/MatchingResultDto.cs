using System;
using System.Collections.Generic;

namespace ITHunterview.Service.DTOs.Cv.Matching
{
    public class MatchingResultDto
    {
        public Guid Id { get; set; }
        public Guid? CvId { get; set; }
        public string? CvFileName { get; set; }
        public Guid? JobId { get; set; }
        public string? JdTitle { get; set; }
        public MatchingMode Mode { get; set; }
        public int ProcessingTimeMs { get; set; }
        
        public string Status { get; set; } = "Pending";
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public bool CanRetry { get; set; }
        public string? MatchDetails { get; set; }

        public JdFitResultDto? JdFit { get; set; }
        public CvQualityResultDto? CvQuality { get; set; }
        public SummaryFeedbackDto? Summary { get; set; }
    }

    public class JdFitResultDto
    {
        public decimal Score { get; set; }
        public string Result { get; set; } = string.Empty;
        public decimal PoolAScore { get; set; }
        public decimal PoolBScore { get; set; }
        public List<RequirementScoreDto> RequirementScores { get; set; } = new();
        public List<CriticalGapDto> CriticalGaps { get; set; } = new();
        public List<PenaltyResultDto> Penalties { get; set; } = new();
        public string Narrative { get; set; } = string.Empty;
    }

    public class CriticalGapDto
    {
        public string Requirement { get; set; } = string.Empty;
        public string GapDescription { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Suggestion { get; set; } = string.Empty;
    }

    public class CvQualityResultDto
    {
        public decimal Score { get; set; }
        public string Feedback { get; set; } = string.Empty;
    }

    public class SummaryFeedbackDto
    {
        public string Overview { get; set; } = string.Empty;
        public string Pros { get; set; } = string.Empty;
        public string Cons { get; set; } = string.Empty;
    }
}
