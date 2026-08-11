using System;
using ITHunterview.Service.DTOs.JobAnalysis;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.Cv.Matching
{
    public class MatchHistoryDto
    {
        public Guid JobId { get; set; }
        public Guid? CvId { get; set; }
        public Guid? CandidateId { get; set; }
        public string? CvFileName { get; set; }
        public Guid? SourceJobId { get; set; }
        public string? JdTitle { get; set; }
        public decimal? MatchScore { get; set; }
        public decimal? ScorePercent { get; set; }
        public bool ScoreAvailable { get; set; }
        public string ReportKind { get; set; } = MatchReportKinds.LegacySummary;
        public string MatchMethod { get; set; } = MatchMethodCodes.LegacyUnknown;
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public string? MatchType { get; set; }
        public string? JdAnalysisQuality { get; set; }
        public string? JdAnalysisScoreBasis { get; set; }
        public JdAnalysisCoverage? JdAnalysisCoverage { get; set; }
        public CvAnalysisQuality? CvAnalysisQuality { get; set; }
        public string? CvAnalysisScoreBasis { get; set; }
        public CvAnalysisCoverage? CvAnalysisCoverage { get; set; }
        public string? FileUrl { get; set; }
        public bool IsUnlocked { get; set; } = true;
        public int UnlockCost { get; set; } = 50;
        public DateTime UpdatedAt { get; set; }
    }
}
