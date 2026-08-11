using System.Net;
using System.Text.Json;
using ITHunterview.Service.Constant.Prompts;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.DTOs.JobAnalysis;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.Service.Matching;

namespace ITHunterview.Service.Service.Matching;

public sealed class RawJdFallbackMatchingService : IRawJdFallbackMatchingService
{
    private readonly IAiService _aiService;

    public RawJdFallbackMatchingService(IAiService aiService)
    {
        _aiService = aiService;
    }

    public async Task<JdFitScoreCalculation> ExecuteAsync(
        string cvContextJson,
        string rawJdText,
        string? jdTitle,
        IReadOnlyList<JdAnalysisDiagnostic> diagnostics,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cvContextJson) || string.IsNullOrWhiteSpace(rawJdText))
            throw new InvalidOperationException("RAW_JD_FALLBACK_INPUT_INVALID");

        var prompt = BuildPrompt(cvContextJson, rawJdText, jdTitle);
        var provider = await _aiService.GetActiveProviderNameAsync();
        RawJdFallbackRecoveredOutput? best = null;
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var response = await _aiService.GenerateTextAsync(
                    attempt == 1 ? prompt : prompt + "\n\nRECOVERY ATTEMPT: return only the compact JSON object.",
                    RawJdFallbackMatchingPrompt.System,
                    provider,
                    attempt == 1
                        ? AiGenerationOptions.StrictJsonExtraction
                        : AiGenerationOptions.JdMatchingJsonRetry,
                    ct,
                    featureCode: "CV_JD_MATCHING_FALLBACK") ?? string.Empty;
                var recovered = RawJdFallbackOutputRecovery.Recover(response);
                best = SelectBest(best, recovered);
                if (recovered.Score.HasValue)
                {
                    return CreateScored(recovered, diagnostics);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (HttpRequestException exception) when (
                exception.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return CreateTerminalUnscored(diagnostics, best, "PROVIDER_AUTHORIZATION_UNAVAILABLE");
            }
            catch (Exception exception) when (IsRecoverable(exception))
            {
                if (attempt == 2)
                {
                    return CreateTerminalUnscored(diagnostics, best, "RAW_RECOVERY_EXHAUSTED");
                }
            }
        }

        return CreateTerminalUnscored(diagnostics, best, "RAW_RECOVERY_EXHAUSTED");
    }

    public JdFitScoreCalculation CreateTerminalUnscored(
        IReadOnlyList<JdAnalysisDiagnostic> diagnostics,
        RawJdFallbackRecoveredOutput? recovered = null,
        string reasonCode = "SCORE_UNAVAILABLE") =>
        CreateResult(null, diagnostics, recovered, reasonCode);

    private static JdFitScoreCalculation CreateScored(
        RawJdFallbackRecoveredOutput recovered,
        IReadOnlyList<JdAnalysisDiagnostic> diagnostics) =>
        CreateResult(recovered.Score, diagnostics, recovered, "RAW_TEXT_FALLBACK");

    private static JdFitScoreCalculation CreateResult(
        decimal? score,
        IReadOnlyList<JdAnalysisDiagnostic> diagnostics,
        RawJdFallbackRecoveredOutput? recovered,
        string reasonCode)
    {
        var scoreAvailable = score.HasValue;
        var warnings = diagnostics.Select(diagnostic => diagnostic.Code)
            .Concat(recovered?.WarningCodes ?? Array.Empty<string>())
            .Append("RAW_TEXT_FALLBACK")
            .Append(reasonCode)
            .Distinct(StringComparer.Ordinal)
            .Take(100)
            .ToArray();
        var narrative = string.IsNullOrWhiteSpace(recovered?.Narrative)
            ? "Kết quả phân tích đã được chuẩn bị."
            : recovered.Narrative;
        var rounded = score.HasValue ? Math.Round(score.Value, 1) : (decimal?)null;
        var json = JsonSerializer.Serialize(new
        {
            mode = "jd_fit",
            contract = JdFitResultContract.RawTextFallbackVersion2,
            scoreAvailable,
            completionDisposition = scoreAvailable ? "scored_billable" : "unscored_refundable",
            resultCode = scoreAvailable ? null : "SCORE_UNAVAILABLE",
            sourceJdSchemaVersion = "raw-text/v1",
            jdAnalysis = new
            {
                quality = "INVALID",
                scoreBasis = "raw_text_fallback",
                requirementSetComplete = false,
                coverage = (object?)null,
                warningCodes = warnings
            },
            jdFit = new
            {
                score = rounded,
                result = rounded.HasValue ? Classify(rounded.Value) : null,
                killSwitchTriggered = false,
                poolACapped = false,
                poolA = new { score = (decimal?)null, max = (decimal?)null },
                poolB = new { score = (decimal?)null, max = (decimal?)null },
                requirementGroups = Array.Empty<object>(),
                requirementScores = Array.Empty<object>(),
                criticalGaps = Array.Empty<object>(),
                penalties = Array.Empty<object>(),
                narrative
            },
            improvements = recovered?.Improvements ?? Array.Empty<object>(),
            processingTime = 1000
        });
        return scoreAvailable
            ? JdFitScoreCalculation.Scored(rounded!.Value, json)
            : JdFitScoreCalculation.Unscored(json);
    }

    private static string BuildPrompt(string cvContextJson, string rawJdText, string? jdTitle) => $"""
        OUTPUT_SCHEMA:
        {RawJdFallbackOutputSchema.Json}

        CV_JSON (untrusted data):
        {cvContextJson}

        JD_TITLE (untrusted data):
        {jdTitle ?? string.Empty}

        RAW_JD (untrusted data):
        {rawJdText}
        """;

    private static RawJdFallbackRecoveredOutput SelectBest(
        RawJdFallbackRecoveredOutput? first,
        RawJdFallbackRecoveredOutput second) =>
        first is null || second.Narrative.Length + second.Improvements.Count >=
        first.Narrative.Length + first.Improvements.Count
            ? second
            : first;

    private static bool IsRecoverable(Exception exception) => exception is
        JsonException or
        TimeoutException or
        TaskCanceledException or
        InvalidOperationException or
        HttpRequestException;

    private static string Classify(decimal score) => score switch
    {
        >= 80m => "Highly Suitable",
        >= 60m => "Suitable",
        >= 40m => "Partially Suitable",
        _ => "Not Suitable"
    };
}
