using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.Cv.Matching;

public sealed record CvJdMatchingExecutionResult(
    decimal Score,
    string MatchDetails,
    string? SfiaExtractResult,
    CvAnalysisQuality? CvAnalysisQuality = null,
    CvAnalysisCoverage? CvAnalysisCoverage = null,
    IReadOnlyList<CvAnalysisDiagnostic>? CvAnalysisDiagnostics = null);
