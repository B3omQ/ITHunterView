using System;

namespace ITHunterview.Service.DTOs.Cv.Matching
{
    public class MatchHistoryDto
    {
        public Guid JobId { get; set; }
        public Guid? CvId { get; set; }
        public string? CvFileName { get; set; }
        public Guid? SourceJobId { get; set; }
        public string? JdTitle { get; set; }
        public decimal? MatchScore { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public string? MatchType { get; set; }
        public string? FileUrl { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
