using System;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.DTOs.Cv
{
    public class CvResponseDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int? FileSize { get; set; }
        public string FileType { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public string ParsedData { get; set; } = string.Empty;
        public string ParseStatus { get; set; } = "PENDING";
        public string? ParseError { get; set; }
        public CvAnalysisQuality? AnalysisQuality { get; set; }
        public CvAnalysisCoverage? AnalysisCoverage { get; set; }
        public List<string> AnalysisWarningCodes { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? WarningMessage { get; set; }
    }
}
