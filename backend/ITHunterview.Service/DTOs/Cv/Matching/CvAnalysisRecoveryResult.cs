using System.Collections.Generic;

namespace ITHunterview.Service.DTOs.Cv.Matching;

public enum CvAnalysisRecoveryMode
{
    COMPLETE_JSON,
    NORMALIZED_JSON,
    EXTRACTED_COMPLETE_OBJECT,
    RECOVERED_PARTIAL,
    INVALID
}

public sealed record CvAnalysisRecoveryResult(
    CvAnalysisRecoveryMode Mode,
    bool WasTruncated,
    string? Json,
    CvAnalysisCoverage? Coverage,
    IReadOnlyList<CvAnalysisDiagnostic> Diagnostics)
{
    public bool HasCandidateJson => !string.IsNullOrWhiteSpace(Json);
}
