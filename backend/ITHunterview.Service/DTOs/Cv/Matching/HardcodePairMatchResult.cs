using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.Cv.Matching;

public sealed record HardcodePairMatchResult(
    decimal? MatchScore,
    string MatchDetails,
    CvAnalysisQuality? CvAnalysisQuality,
    string? CvAnalysisCoverageJson,
    string? CvAnalysisDiagnosticsJson);
