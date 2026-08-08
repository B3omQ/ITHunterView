using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.JobAnalysis;

namespace ITHunterview.Service.DTOs.Cv.Matching;

public sealed record CvAnalysisPersistenceIntent(
    Guid CvId,
    Guid OwnerId,
    string ExpectedSourceHash,
    string ExpectedAnalysisHash,
    string CanonicalJson,
    CvAnalysisQuality Quality,
    string? CoverageJson,
    string? DiagnosticsJson);

public sealed record PreparedCvMatchingInput(
    string CanonicalJson,
    CvAnalysisQuality Quality,
    CvAnalysisCoverage? Coverage,
    IReadOnlyList<CvAnalysisDiagnostic> Diagnostics,
    CvAnalysisPersistenceIntent? PersistenceIntent);

public sealed record JdAnalysisPersistenceIntent(
    Guid JobId,
    string ExpectedSourceHash,
    string ExpectedAnalysisHash,
    int? ExpectedRevision,
    string? CanonicalJson,
    JdAnalysisQuality Quality,
    string? CoverageJson,
    string? DiagnosticsJson,
    string? FailureCode);

public abstract record PreparedJdMatchingInput(
    JdAnalysisQuality Quality,
    JdAnalysisCoverage? Coverage,
    IReadOnlyList<JdAnalysisDiagnostic> Diagnostics,
    JdAnalysisPersistenceIntent? PersistenceIntent);

public sealed record PreparedStructuredJdMatchingInput(
    string EffectiveJson,
    JdRequirementProjection Projection,
    JdAnalysisQuality Quality,
    JdAnalysisCoverage? Coverage,
    IReadOnlyList<JdAnalysisDiagnostic> Diagnostics,
    JdAnalysisPersistenceIntent? PersistenceIntent)
    : PreparedJdMatchingInput(Quality, Coverage, Diagnostics, PersistenceIntent);

public sealed record PreparedRawJdMatchingInput(
    string RawText,
    string? Title,
    IReadOnlyList<JdAnalysisDiagnostic> Diagnostics,
    JdAnalysisPersistenceIntent? PersistenceIntent)
    : PreparedJdMatchingInput(JdAnalysisQuality.INVALID, null, Diagnostics, PersistenceIntent);
