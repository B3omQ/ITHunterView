using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.JobAnalysis;

namespace ITHunterview.Service.DTOs.Cv.Matching;

public sealed record CvJdMatchingExecutionResult(
    decimal Score,
    string MatchDetails,
    string? SfiaExtractResult,
    CvAnalysisQuality? CvAnalysisQuality = null,
    CvAnalysisCoverage? CvAnalysisCoverage = null,
    IReadOnlyList<CvAnalysisDiagnostic>? CvAnalysisDiagnostics = null,
    JdAnalysisQuality? JdAnalysisQuality = null,
    JdAnalysisCoverage? JdAnalysisCoverage = null,
    IReadOnlyList<JdAnalysisDiagnostic>? JdAnalysisDiagnostics = null,
    CvAnalysisPersistenceIntent? CvPersistenceIntent = null,
    JdAnalysisPersistenceIntent? JdPersistenceIntent = null);
