using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Service.Matching;

/// <summary>
/// Mechanically maps provider fields without inferring missing scores or
/// changing requirement semantics.
/// </summary>
public sealed class JdMatchingResponseAdapter
{
    public JdStageTwoValidatedResponse Adapt(
        JsonDocument response,
        JdRequirementProjection projection,
        bool isCompleteJson = true,
        bool wasTruncated = false,
        IReadOnlyList<string>? recoveryWarnings = null)
    {
        var validated = JdMatchingResponseValidator.Validate(
            response,
            projection,
            isCompleteJson,
            wasTruncated,
            recoveryWarnings);

        return new JdStageTwoValidatedResponse(
            validated.ItemScores,
            validated.Narrative,
            validated.Improvements,
            validated.Penalties,
            validated.Quality,
            validated.Coverage,
            validated.WarningCodes);
    }

    public JdStageTwoValidatedResponse MergePartialAttempts(
        JdStageTwoValidatedResponse first,
        JdStageTwoValidatedResponse second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        if (first.Quality == JdStageTwoOutputQuality.INVALID)
        {
            return second;
        }
        if (second.Quality == JdStageTwoOutputQuality.INVALID)
        {
            return first;
        }
        if (second.Quality == JdStageTwoOutputQuality.COMPLETE)
        {
            return second;
        }

        var scores = new Dictionary<string, JdStageTwoItemScore>(first.ItemScores, StringComparer.Ordinal);
        foreach (var score in second.ItemScores)
        {
            scores[score.Key] = score.Value;
        }

        var expected = Math.Max(first.Coverage?.ExpectedScoreCount ?? 0, second.Coverage?.ExpectedScoreCount ?? 0);
        var input = (first.Coverage?.InputScoreCount ?? first.ItemScores.Count) +
                    (second.Coverage?.InputScoreCount ?? second.ItemScores.Count);
        var discarded = (first.Coverage?.DiscardedScoreCount ?? 0) +
                        (second.Coverage?.DiscardedScoreCount ?? 0);
        var missing = Math.Max(0, expected - scores.Count);
        var warnings = (first.WarningCodes ?? Array.Empty<string>())
            .Concat(second.WarningCodes ?? Array.Empty<string>())
            .Where(code => missing > 0 || !string.Equals(code, "MISSING_REQUIREMENT_SCORES", StringComparison.Ordinal))
            .Append("MERGED_PARTIAL_ATTEMPTS")
            .Distinct(StringComparer.Ordinal)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();

        var improvements = HasItems(second.Improvements) ? second.Improvements : first.Improvements;
        return new JdStageTwoValidatedResponse(
            scores,
            string.IsNullOrWhiteSpace(second.Narrative) ? first.Narrative : second.Narrative,
            improvements,
            second.Penalties.Count > 0 ? second.Penalties : first.Penalties,
            JdStageTwoOutputQuality.PARTIAL,
            new JdStageTwoOutputCoverage(
                expected,
                input,
                scores.Count,
                discarded,
                missing,
                (first.Coverage?.WasTruncated ?? false) || (second.Coverage?.WasTruncated ?? false)),
            warnings);
    }

    private static bool HasItems(JsonElement value) =>
        value.ValueKind == JsonValueKind.Array && value.GetArrayLength() > 0;
}
