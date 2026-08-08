using ITHunterview.Service.DTOs.JobAnalysis;
using ITHunterview.Service.Service.Matching;

namespace ITHunterview.Service.Interface.Service.Matching;

public interface IRawJdFallbackMatchingService
{
    Task<JdFitScoreCalculation> ExecuteAsync(
        string cvContextJson,
        string rawJdText,
        string? jdTitle,
        IReadOnlyList<JdAnalysisDiagnostic> diagnostics,
        CancellationToken ct = default);
}
