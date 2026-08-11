using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Service.Matching;

public sealed record JdStageTwoValidationResult(
    JdStageTwoOutputQuality Quality,
    IReadOnlyDictionary<string, JdStageTwoItemScore> ItemScores,
    JdStageTwoOutputCoverage Coverage,
    string Narrative,
    JsonElement Improvements,
    IReadOnlyList<JdStageTwoPenalty> Penalties,
    IReadOnlyList<string> WarningCodes);

/// <summary>
/// Validates the mechanical matching-output contract. Invalid individual
/// items are discarded without invalidating other usable scores. It never
/// changes IDs, handler codes, scores, or requirement semantics.
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

    public static JdStageTwoValidationResult Validate(
        JsonDocument response,
        JdRequirementProjection projection,
        bool isCompleteJson = true,
        bool wasTruncated = false,
        IReadOnlyList<string>? recoveryWarnings = null)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(projection);

        var expectedItems = projection.Groups.SelectMany(group => group.Items).ToArray();
        var expectedIds = expectedItems.Select(item => item.ItemId).ToHashSet(StringComparer.Ordinal);
        var categoryByItem = expectedItems
            .Where(item => !string.IsNullOrWhiteSpace(item.ItemId))
            .GroupBy(item => item.ItemId, StringComparer.Ordinal)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single().Category, StringComparer.Ordinal);
        if (expectedIds.Count == 0 || expectedIds.Count != expectedItems.Length || categoryByItem.Count != expectedItems.Length)
        {
            throw new InvalidOperationException(InvalidStageTwoResponse);
        }

        var warnings = new HashSet<string>(recoveryWarnings ?? Array.Empty<string>(), StringComparer.Ordinal);
        var scores = new Dictionary<string, JdStageTwoItemScore>(StringComparer.Ordinal);
        var inputScoreCount = 0;
        var discardedScoreCount = 0;
        var root = response.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("scores", out var scoreArray) ||
            scoreArray.ValueKind != JsonValueKind.Array)
        {
            warnings.Add("SCORES_ARRAY_MISSING_OR_INVALID");
        }
        else
        {
            inputScoreCount = scoreArray.GetArrayLength();
            if (inputScoreCount > MaxTopLevelItems)
            {
                discardedScoreCount += inputScoreCount - MaxTopLevelItems;
                warnings.Add("SCORE_ITEM_LIMIT_EXCEEDED");
            }

            foreach (var score in scoreArray.EnumerateArray().Take(MaxTopLevelItems))
            {
                if (!TryReadScore(score, categoryByItem, scores, out var itemScore, out var warning))
                {
                    discardedScoreCount++;
                    warnings.Add(warning);
                    continue;
                }

                scores.Add(itemScore!.ItemId, itemScore);
            }
        }

        var missingScoreCount = Math.Max(0, expectedIds.Count - scores.Count);
        if (missingScoreCount > 0)
        {
            warnings.Add("MISSING_REQUIREMENT_SCORES");
        }

        var narrative = ReadOptionalString(root, "narrative", MaxNarrativeLength, warnings, "NARRATIVE_INVALID");
        var improvements = ReadImprovements(root, warnings);
        var penalties = ReadPenalties(root, warnings);

        var quality = scores.Count == 0
            ? JdStageTwoOutputQuality.INVALID
            : isCompleteJson && !wasTruncated && missingScoreCount == 0 && discardedScoreCount == 0 && warnings.Count == 0
                ? JdStageTwoOutputQuality.COMPLETE
                : JdStageTwoOutputQuality.PARTIAL;

        return new JdStageTwoValidationResult(
            quality,
            scores,
            new JdStageTwoOutputCoverage(
                expectedIds.Count,
                inputScoreCount,
                scores.Count,
                discardedScoreCount,
                missingScoreCount,
                wasTruncated),
            narrative,
            improvements,
            penalties,
            warnings.OrderBy(code => code, StringComparer.Ordinal).ToArray());
    }

    private static bool TryReadScore(
        JsonElement element,
        IReadOnlyDictionary<string, string> categoryByItem,
        IReadOnlyDictionary<string, JdStageTwoItemScore> accepted,
        out JdStageTwoItemScore? score,
        out string warning)
    {
        score = null;
        warning = "INVALID_SCORE_ITEM";
        if (element.ValueKind != JsonValueKind.Object ||
            !TryReadRequiredString(element, "reqId", out var reqId) ||
            !TryReadRequiredString(element, "handlerCode", out var handlerCode) ||
            !element.TryGetProperty("handlerScore", out var scoreValue) ||
            scoreValue.ValueKind != JsonValueKind.Number ||
            !scoreValue.TryGetDecimal(out var handlerScore) ||
            handlerScore is < 0m or > 1m)
        {
            return false;
        }

        if (!categoryByItem.TryGetValue(reqId, out var category))
        {
            warning = "UNKNOWN_REQUIREMENT_ID";
            return false;
        }

        if (accepted.ContainsKey(reqId))
        {
            warning = "DUPLICATE_REQUIREMENT_ID";
            return false;
        }

        if (!MatchingHandlerCodePolicy.IsValid(category, handlerCode))
        {
            warning = "HANDLER_CODE_CATEGORY_MISMATCH";
            return false;
        }

        var reasoning = ReadOptionalString(element, "reasoning", MaxReasoningLength);
        var confidence = ReadConfidence(element);
        var evidence = ReadEvidence(element);
        score = new JdStageTwoItemScore(reqId, handlerCode, handlerScore, reasoning, confidence, evidence);
        warning = string.Empty;
        return true;
    }

    private static string ReadConfidence(JsonElement score)
    {
        if (!score.TryGetProperty("confidence", out var confidence) || confidence.ValueKind == JsonValueKind.Null)
        {
            return "unknown";
        }

        if (confidence.ValueKind != JsonValueKind.String)
        {
            return "unknown";
        }

        var value = confidence.GetString()?.Trim() ?? string.Empty;
        return value is "high" or "medium" or "low" ? value : "unknown";
    }

    private static IReadOnlyList<string> ReadEvidence(JsonElement score)
    {
        if (!score.TryGetProperty("evidence", out var evidence) || evidence.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return evidence.EnumerateArray()
            .Where(value => value.ValueKind == JsonValueKind.String)
            .Select(value => value.GetString()?.Trim() ?? string.Empty)
            .Where(value => value.Length is > 0 and <= MaxEvidenceLength)
            .Distinct(StringComparer.Ordinal)
            .Take(MaxEvidenceItems)
            .ToArray();
    }

    private static JsonElement ReadImprovements(JsonElement root, ISet<string> warnings)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("ROOT_NOT_OBJECT");
            return JsonSerializer.SerializeToElement(Array.Empty<object>());
        }

        if (!root.TryGetProperty("improvements", out var improvements) || improvements.ValueKind == JsonValueKind.Null)
        {
            return JsonSerializer.SerializeToElement(Array.Empty<object>());
        }

        if (improvements.ValueKind != JsonValueKind.Array)
        {
            warnings.Add("IMPROVEMENTS_INVALID");
            return JsonSerializer.SerializeToElement(Array.Empty<object>());
        }

        var accepted = improvements.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.Object)
            .Take(MaxTopLevelItems)
            .Select(item => item.Clone())
            .ToArray();
        if (accepted.Length != improvements.GetArrayLength())
        {
            warnings.Add("IMPROVEMENTS_PARTIALLY_DISCARDED");
        }
        return JsonSerializer.SerializeToElement(accepted);
    }

    private static IReadOnlyList<JdStageTwoPenalty> ReadPenalties(JsonElement root, ISet<string> warnings)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("ROOT_NOT_OBJECT");
            return Array.Empty<JdStageTwoPenalty>();
        }

        if (!root.TryGetProperty("penalties", out var penalties) || penalties.ValueKind == JsonValueKind.Null)
        {
            return Array.Empty<JdStageTwoPenalty>();
        }

        if (penalties.ValueKind != JsonValueKind.Array)
        {
            warnings.Add("PENALTIES_INVALID");
            return Array.Empty<JdStageTwoPenalty>();
        }

        var result = new List<JdStageTwoPenalty>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var penalty in penalties.EnumerateArray().Take(MaxTopLevelItems))
        {
            if (penalty.ValueKind != JsonValueKind.Object ||
                !TryReadRequiredString(penalty, "code", out var code) ||
                !string.Equals(code, CredibilityPenaltyCode, StringComparison.Ordinal) ||
                !seen.Add(code) ||
                !penalty.TryGetProperty("triggered", out var triggered) ||
                triggered.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
            {
                warnings.Add("PENALTY_ITEM_DISCARDED");
                continue;
            }

            var evidence = ReadOptionalString(penalty, "evidence", MaxPenaltyEvidenceLength);
            if (triggered.GetBoolean() && evidence.Length == 0)
            {
                warnings.Add("PENALTY_ITEM_DISCARDED");
                continue;
            }
            result.Add(new JdStageTwoPenalty(code, triggered.GetBoolean(), evidence));
        }
        return result;
    }

    private static bool TryReadRequiredString(JsonElement element, string property, out string value)
    {
        value = string.Empty;
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(property, out var propertyValue) ||
            propertyValue.ValueKind != JsonValueKind.String)
        {
            return false;
        }
        value = propertyValue.GetString()?.Trim() ?? string.Empty;
        return value.Length is > 0 and <= MaxNarrativeLength;
    }

    private static string ReadOptionalString(JsonElement element, string property, int maximumLength) =>
        ReadOptionalString(element, property, maximumLength, null, string.Empty);

    private static string ReadOptionalString(
        JsonElement element,
        string property,
        int maximumLength,
        ISet<string>? warnings,
        string warning)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(property, out var value) ||
            value.ValueKind == JsonValueKind.Null)
        {
            return string.Empty;
        }
        if (value.ValueKind != JsonValueKind.String)
        {
            if (warning.Length > 0) warnings?.Add(warning);
            return string.Empty;
        }
        var text = value.GetString()?.Trim() ?? string.Empty;
        if (text.Length <= maximumLength)
        {
            return text;
        }
        if (warning.Length > 0) warnings?.Add(warning);
        return text[..maximumLength];
    }
}
