using System.Text.Json;
using ITHunterview.Service.Constant.Prompts;
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

        var prompt = $"""
            OUTPUT_SCHEMA:
            {RawJdFallbackOutputSchema.Json}

            CV_JSON (untrusted data):
            {cvContextJson}

            JD_TITLE (untrusted data):
            {jdTitle ?? string.Empty}

            RAW_JD (untrusted data):
            {rawJdText}
            """;
        var provider = await _aiService.GetActiveProviderNameAsync();
        var response = await _aiService.GenerateTextAsync(
            prompt,
            RawJdFallbackMatchingPrompt.System,
            provider,
            AiGenerationOptions.StrictJsonExtraction,
            ct,
            featureCode: "CV_JD_MATCHING_FALLBACK") ?? string.Empty;
        var output = Parse(response);
        var score = Math.Clamp(output.Score, 0m, 100m);
        var warnings = diagnostics.Select(diagnostic => diagnostic.Code)
            .Append("RAW_TEXT_FALLBACK")
            .Distinct(StringComparer.Ordinal)
            .Take(100)
            .ToArray();
        var json = JsonSerializer.Serialize(new
        {
            mode = "jd_fit",
            contract = JdFitResultContract.RawTextFallback,
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
                score = Math.Round(score, 1),
                result = Classify(score),
                killSwitchTriggered = false,
                poolACapped = false,
                poolA = new { score = (decimal?)null, max = (decimal?)null },
                poolB = new { score = (decimal?)null, max = (decimal?)null },
                requirementGroups = Array.Empty<object>(),
                requirementScores = Array.Empty<object>(),
                criticalGaps = Array.Empty<object>(),
                penalties = Array.Empty<object>(),
                narrative = output.Narrative
            },
            improvements = output.Improvements,
            processingTime = 1000
        });
        return new JdFitScoreCalculation(score, json);
    }

    private static RawJdFallbackOutput Parse(string response)
    {
        using var document = JsonDocument.Parse(StripFence(response));
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            root.EnumerateObject().Any(property => property.Name is not ("score" or "narrative" or "improvements")) ||
            !root.TryGetProperty("score", out var score) || !score.TryGetDecimal(out var parsedScore) || parsedScore is < 0m or > 100m ||
            !root.TryGetProperty("narrative", out var narrative) || narrative.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(narrative.GetString()) || narrative.GetString()!.Length > 4000 ||
            !root.TryGetProperty("improvements", out var improvements) || improvements.ValueKind != JsonValueKind.Array || improvements.GetArrayLength() > 20)
            throw new InvalidOperationException("RAW_JD_FALLBACK_OUTPUT_INVALID");

        var validated = new List<object>();
        foreach (var improvement in improvements.EnumerateArray())
        {
            if (improvement.ValueKind != JsonValueKind.Object ||
                improvement.EnumerateObject().Any(property => property.Name is not ("priority" or "category" or "issue" or "action")) ||
                !ReadBounded(improvement, "priority", 500, out var priority) ||
                priority is not ("high" or "medium" or "low") ||
                !ReadBounded(improvement, "category", 500, out var category) ||
                !ReadBounded(improvement, "issue", 500, out var issue) ||
                !ReadBounded(improvement, "action", 500, out var action))
                throw new InvalidOperationException("RAW_JD_FALLBACK_OUTPUT_INVALID");
            validated.Add(new { priority, category, issue, action });
        }

        return new RawJdFallbackOutput(parsedScore, narrative.GetString()!.Trim(), validated);
    }

    private static bool ReadBounded(JsonElement element, string name, int maximumLength, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.String) return false;
        value = property.GetString()?.Trim() ?? string.Empty;
        return value.Length > 0 && value.Length <= maximumLength;
    }

    private static string StripFence(string value)
    {
        var candidate = value.Trim();
        if (!candidate.StartsWith("```", StringComparison.Ordinal) || !candidate.EndsWith("```", StringComparison.Ordinal)) return candidate;
        var firstNewline = candidate.IndexOf('\n');
        return firstNewline < 0 ? candidate : candidate[(firstNewline + 1)..^3].Trim();
    }

    private static string Classify(decimal score) => score switch
    {
        >= 80m => "Highly Suitable",
        >= 60m => "Suitable",
        >= 40m => "Partially Suitable",
        _ => "Not Suitable"
    };

    private sealed record RawJdFallbackOutput(decimal Score, string Narrative, IReadOnlyList<object> Improvements);
}
