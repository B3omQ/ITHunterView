using System.Text.Json;

namespace ITHunterview.Service.Service.Matching;

/// <summary>
/// Validates the legacy flat Stage 2 contract before scoring. The calculator
/// may assume a complete response only after this gate has passed.
/// </summary>
public static class LegacyJdStageTwoResponseValidator
{
    public const string InvalidStageTwoResponse = "INVALID_STAGE_TWO_RESPONSE";

    public static void Validate(
        JsonDocument response,
        IReadOnlyCollection<string> requirementIds,
        IReadOnlyDictionary<string, string>? categoryByRequirement = null)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(requirementIds);

        var expectedIds = requirementIds
            .Select(id => id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);
        if (expectedIds.Count != requirementIds.Count || expectedIds.Count == 0)
            throw Invalid();

        var root = response.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("scores", out var scores) ||
            scores.ValueKind != JsonValueKind.Array)
            throw Invalid();

        var actualIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var score in scores.EnumerateArray())
        {
            if (score.ValueKind != JsonValueKind.Object ||
                !score.TryGetProperty("reqId", out var reqId) ||
                reqId.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(reqId.GetString()) ||
                !score.TryGetProperty("handlerCode", out var handlerCode) ||
                handlerCode.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(handlerCode.GetString()) ||
                !score.TryGetProperty("handlerScore", out var handlerScore) ||
                handlerScore.ValueKind != JsonValueKind.Number ||
                !handlerScore.TryGetDecimal(out var numericScore) ||
                numericScore is < 0m or > 1m)
            {
                throw Invalid();
            }

            var id = reqId.GetString()!.Trim();
            var code = handlerCode.GetString()!.Trim();
            if (!expectedIds.Contains(id) || !actualIds.Add(id))
                throw Invalid();
            if (!MatchingHandlerCodePolicy.IsKnown(code))
                throw Invalid();
            if (categoryByRequirement is not null &&
                (!categoryByRequirement.TryGetValue(id, out var category) ||
                 !MatchingHandlerCodePolicy.IsValid(category, code)))
            {
                throw Invalid();
            }

            if (score.TryGetProperty("flag", out var flag) &&
                flag.ValueKind != JsonValueKind.Null &&
                (flag.ValueKind != JsonValueKind.String ||
                 !string.Equals(flag.GetString(), "CRITICAL_GAP", StringComparison.Ordinal)))
            {
                throw Invalid();
            }
        }

        if (!actualIds.SetEquals(expectedIds))
            throw Invalid();
    }

    private static InvalidOperationException Invalid() => new(InvalidStageTwoResponse);
}
