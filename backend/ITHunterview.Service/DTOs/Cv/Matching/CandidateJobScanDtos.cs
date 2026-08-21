using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.Cv.Matching;

public sealed record CandidateJobScanAcceptedDto(Guid RunId, string Status);

public sealed record CandidateJobScanResultDto(
    Guid Id,
    Guid RunId,
    Guid JobId,
    string JobTitle,
    decimal? MatchScore,
    string MatchDetails,
    CvAnalysisQuality? CvAnalysisQuality,
    string? CvAnalysisCoverageJson,
    string? CvAnalysisDiagnosticsJson,
    int Rank);
