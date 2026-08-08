using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Service.Matching;

/// <summary>
/// Validates only the mechanical shape of the approved scores/reqId output.
/// It deliberately does not judge whether the model's evidence or reasoning
/// is semantically correct.
/// </summary>
public static class JdMatchingResponseValidator
{
    public const string InvalidStageTwoResponse = "INVALID_STAGE_TWO_RESPONSE";

    private const string CredibilityPenaltyCode = "PNL_TC1_01";
    private const int MaxReasoningLength = 2_000;
    private const int MaxEvidenceItems = 5;
    private const int MaxEvidenceLength = 500;
    private const int MaxNarrativeLength = 4_000;
    private const int MaxPenaltyEvidenceLength = 1_000;
    private const int MaxTopLevelItems = 100;

    public static void Validate(JsonDocument response, JdRequirementProjection projection)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(projection);

        try
        {
            var root = response.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("scores", out var scoresElement) ||
                scoresElement.ValueKind != JsonValueKind.Array)
            {
                throw Invalid();
            }

            var expectedItems = projection.Groups.SelectMany(group => group.Items).ToArray();
            var expectedIds = expectedItems.Select(item => item.ItemId).ToHashSet(StringComparer.Ordinal);
            var categoryByItem = expectedItems.ToDictionary(item => item.ItemId, item => item.Category, StringComparer.Ordinal);
            if (expectedIds.Count == 0 || expectedIds.Count != expectedItems.Length)
            {
                throw Invalid();
            }

            var actualIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var score in scoresElement.EnumerateArray())
            {
                if (score.ValueKind != JsonValueKind.Object)
                {
                    throw Invalid();
                }

                var reqId = RequiredString(score, "reqId");
                var handlerCode = RequiredString(score, "handlerCode");
                var handlerScore = RequiredDecimal(score, "handlerScore");
                if (handlerScore is < 0m or > 1m ||
                    !expectedIds.Contains(reqId) ||
                    !actualIds.Add(reqId) ||
                    !categoryByItem.TryGetValue(reqId, out var category) ||
                    !MatchingHandlerCodePolicy.IsValid(category, handlerCode))
                {
                    throw Invalid();
                }

                ReadOptionalBoundedString(score, "reasoning", MaxReasoningLength);
                ReadConfidence(score);
                ReadEvidence(score);
                ReadFlag(score);
            }

            if (!actualIds.SetEquals(expectedIds))
            {
                throw Invalid();
            }

            ReadOptionalTopLevelString(root, "narrative", MaxNarrativeLength);
            ReadCriticalGaps(root);
            ReadPenalties(root);
            ReadImprovements(root);
        }
        catch (InvalidOperationException exception) when (exception.Message == InvalidStageTwoResponse)
        {
            throw;
        }
        catch (Exception)
        {
            throw Invalid();
        }
    }

    public static IReadOnlyList<JdStageTwoPenalty> ReadPenalties(JsonElement root)
    {
        if (!root.TryGetProperty("penalties", out var penaltiesElement) ||
            penaltiesElement.ValueKind == JsonValueKind.Null)
        {
            return Array.Empty<JdStageTwoPenalty>();
        }

        if (penaltiesElement.ValueKind != JsonValueKind.Array || penaltiesElement.GetArrayLength() > MaxTopLevelItems)
        {
            throw Invalid();
        }

        var penalties = new List<JdStageTwoPenalty>();
        var seenCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var penalty in penaltiesElement.EnumerateArray())
        {
            if (penalty.ValueKind != JsonValueKind.Object)
            {
                throw Invalid();
            }

            var code = RequiredString(penalty, "code");
            if (!string.Equals(code, CredibilityPenaltyCode, StringComparison.Ordinal) || !seenCodes.Add(code))
            {
                throw Invalid();
            }

            if (!penalty.TryGetProperty("triggered", out var triggered) ||
                (triggered.ValueKind is not JsonValueKind.True and not JsonValueKind.False))
            {
                throw Invalid();
            }

            if (penalty.TryGetProperty("deduction", out var deduction) &&
                deduction.ValueKind != JsonValueKind.Null &&
                (deduction.ValueKind != JsonValueKind.Number ||
                 !deduction.TryGetDecimal(out var numericDeduction) ||
                 numericDeduction is < 0m or > 100m))
            {
                throw Invalid();
            }

            var evidence = ReadOptionalBoundedString(penalty, "evidence", MaxPenaltyEvidenceLength);
            if (triggered.GetBoolean() && evidence.Length == 0)
            {
                throw Invalid();
            }

            penalties.Add(new JdStageTwoPenalty(code, triggered.GetBoolean(), evidence));
        }

        return penalties;
    }

    private static void ReadCriticalGaps(JsonElement root)
    {
        if (!root.TryGetProperty("criticalGaps", out var gaps) || gaps.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        if (gaps.ValueKind != JsonValueKind.Array || gaps.GetArrayLength() > MaxTopLevelItems)
        {
            throw Invalid();
        }

        foreach (var gap in gaps.EnumerateArray())
        {
            if (gap.ValueKind != JsonValueKind.Object)
            {
                throw Invalid();
            }

            ReadOptionalBoundedString(gap, "requirement", MaxNarrativeLength);
            ReadOptionalBoundedString(gap, "gapDescription", MaxNarrativeLength);
            var severity = ReadOptionalBoundedString(gap, "severity", 32);
            if (severity.Length > 0 && severity is not ("high" or "medium"))
            {
                throw Invalid();
            }
            ReadOptionalBoundedString(gap, "suggestion", MaxNarrativeLength);
        }
    }

    private static void ReadImprovements(JsonElement root)
    {
        if (!root.TryGetProperty("improvements", out var improvements) || improvements.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        if (improvements.ValueKind != JsonValueKind.Array || improvements.GetArrayLength() > MaxTopLevelItems)
        {
            throw Invalid();
        }

        foreach (var improvement in improvements.EnumerateArray())
        {
            if (improvement.ValueKind != JsonValueKind.Object)
            {
                throw Invalid();
            }
        }
    }

    private static void ReadFlag(JsonElement score)
    {
        if (!score.TryGetProperty("flag", out var flag) || flag.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        if (flag.ValueKind != JsonValueKind.String ||
            !string.Equals(flag.GetString(), "CRITICAL_GAP", StringComparison.Ordinal))
        {
            throw Invalid();
        }
    }

    private static string ReadConfidence(JsonElement score)
    {
        if (!score.TryGetProperty("confidence", out var confidence) || confidence.ValueKind == JsonValueKind.Null)
        {
            return "unknown";
        }

        if (confidence.ValueKind != JsonValueKind.String)
        {
            throw Invalid();
        }

        var value = confidence.GetString()?.Trim() ?? string.Empty;
        if (value is not ("high" or "medium" or "low"))
        {
            throw Invalid();
        }

        return value;
    }

    private static IReadOnlyList<string> ReadEvidence(JsonElement score)
    {
        if (!score.TryGetProperty("evidence", out var evidence) || evidence.ValueKind == JsonValueKind.Null)
        {
            return Array.Empty<string>();
        }

        if (evidence.ValueKind != JsonValueKind.Array || evidence.GetArrayLength() > MaxEvidenceItems)
        {
            throw Invalid();
        }

        var values = new List<string>();
        foreach (var value in evidence.EnumerateArray())
        {
            if (value.ValueKind != JsonValueKind.String)
            {
                throw Invalid();
            }

            var text = value.GetString()?.Trim() ?? string.Empty;
            if (text.Length == 0 || text.Length > MaxEvidenceLength)
            {
                throw Invalid();
            }

            values.Add(text);
        }

        return values.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static string RequiredString(JsonElement element, string property)
    {
        var value = ReadOptionalBoundedString(element, property, MaxNarrativeLength);
        return value.Length > 0 ? value : throw Invalid();
    }

    private static string ReadOptionalBoundedString(JsonElement element, string property, int maximumLength)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return string.Empty;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            throw Invalid();
        }

        var text = value.GetString()?.Trim() ?? string.Empty;
        if (text.Length > maximumLength)
        {
            throw Invalid();
        }

        return text;
    }

    private static void ReadOptionalTopLevelString(JsonElement root, string property, int maximumLength) =>
        ReadOptionalBoundedString(root, property, maximumLength);

    private static decimal RequiredDecimal(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) ||
            value.ValueKind != JsonValueKind.Number ||
            !value.TryGetDecimal(out var result))
        {
            throw Invalid();
        }

        return result;
    }

    private static InvalidOperationException Invalid() => new(InvalidStageTwoResponse);
}
