namespace ITHunterview.Service.DTOs.Cv.Matching;

public enum CvAnalysisValidationOrigin
{
    ProviderOutput,
    RecoveredProviderOutput,
    StoredCanonical
}

public sealed record CvAnalysisValidationContext(
    CvAnalysisValidationOrigin Origin,
    bool WasTruncated = false,
    CvAnalysisRecoveryMode? RecoveryMode = null,
    CvAnalysisCoverage? RecoveryCoverage = null,
    IReadOnlyList<CvAnalysisDiagnostic>? RecoveryDiagnostics = null);
