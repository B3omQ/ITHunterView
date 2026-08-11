using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ITHunterview.Service.Service.Matching;

public sealed record RawJdFallbackRecoveredOutput(
    decimal? Score,
    string Narrative,
    IReadOnlyList<object> Improvements,
    IReadOnlyList<string> WarningCodes);

public static partial class RawJdFallbackOutputRecovery
{
    private const int MaximumOutputLength = 1_000_000;
    private const int MaximumNarrativeLength = 4_000;
    private const int MaximumImprovements = 20;
    private const int MaximumImprovementFieldLength = 500;

    public static RawJdFallbackRecoveredOutput Recover(string? providerOutput)
    {
        var candidate = StripFence(providerOutput);
        if (candidate.Length == 0 || candidate.Length > MaximumOutputLength)
        {
            return Empty(candidate.Length == 0 ? "EMPTY_MODEL_OUTPUT" : "PAYLOAD_TOO_LARGE");
        }

        if (TryParse(candidate, out var complete))
        {
            using (complete)
            {
                return ReadDocument(complete.RootElement, Array.Empty<string>());
            }
        }

        if (TryExtractBalancedRoot(candidate, out var root) && TryParse(root, out var extracted))
        {
            using (extracted)
            {
                return ReadDocument(extracted.RootElement, new[] { "EXTRACTED_COMPLETE_JSON_OBJECT" });
            }
        }

        var warnings = new HashSet<string>(StringComparer.Ordinal) { "OUTPUT_TRUNCATED" };
        decimal? score = TryExtractScalar(candidate, ScoreRegex(), out var rawScore) &&
                    TryParseScore(rawScore, out var parsedScore)
            ? parsedScore
            : null;
        if (!score.HasValue) warnings.Add("SCORE_MISSING_OR_INVALID");
        var narrative = TryExtractScalar(candidate, NarrativeRegex(), out var rawNarrative)
            ? Bound(rawNarrative, MaximumNarrativeLength)
            : string.Empty;
        if (narrative.Length == 0) warnings.Add("NARRATIVE_MISSING_OR_INVALID");
        return new RawJdFallbackRecoveredOutput(
            score,
            narrative,
            Array.Empty<object>(),
            warnings.OrderBy(code => code, StringComparer.Ordinal).ToArray());
    }

    private static RawJdFallbackRecoveredOutput ReadDocument(
        JsonElement root,
        IReadOnlyList<string> initialWarnings)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return Empty("ROOT_NOT_OBJECT");
        }

        var warnings = new HashSet<string>(initialWarnings, StringComparer.Ordinal);
        decimal? score = null;
        if (root.TryGetProperty("score", out var scoreElement) &&
            TryReadScore(scoreElement, out var parsedScore))
        {
            score = parsedScore;
        }
        else
        {
            warnings.Add("SCORE_MISSING_OR_INVALID");
        }

        var narrative = root.TryGetProperty("narrative", out var narrativeElement) &&
                        narrativeElement.ValueKind == JsonValueKind.String
            ? Bound(narrativeElement.GetString(), MaximumNarrativeLength)
            : string.Empty;
        if (narrative.Length == 0) warnings.Add("NARRATIVE_MISSING_OR_INVALID");

        var improvements = new List<object>();
        if (root.TryGetProperty("improvements", out var improvementArray) &&
            improvementArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in improvementArray.EnumerateArray().Take(MaximumImprovements))
            {
                if (TryReadImprovement(item, out var improvement))
                {
                    improvements.Add(improvement!);
                }
                else
                {
                    warnings.Add("IMPROVEMENT_ITEM_DISCARDED");
                }
            }
        }

        return new RawJdFallbackRecoveredOutput(
            score,
            narrative,
            improvements,
            warnings.OrderBy(code => code, StringComparer.Ordinal).ToArray());
    }

    private static bool TryReadScore(JsonElement element, out decimal score)
    {
        score = 0m;
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var number))
        {
            return number is >= 0m and <= 100m && (score = number) >= 0m;
        }

        return element.ValueKind == JsonValueKind.String &&
               TryParseScore(element.GetString(), out score);
    }

    private static bool TryParseScore(string? value, out decimal score)
    {
        score = 0m;
        return !string.IsNullOrWhiteSpace(value) &&
               decimal.TryParse(
                   value.Trim(),
                   NumberStyles.AllowDecimalPoint,
                   CultureInfo.InvariantCulture,
                   out score) &&
               score is >= 0m and <= 100m;
    }

    private static bool TryReadImprovement(JsonElement item, out object? improvement)
    {
        improvement = null;
        if (item.ValueKind != JsonValueKind.Object ||
            !TryReadBounded(item, "priority", out var priority) ||
            priority is not ("high" or "medium" or "low") ||
            !TryReadBounded(item, "category", out var category) ||
            !TryReadBounded(item, "issue", out var issue) ||
            !TryReadBounded(item, "action", out var action))
        {
            return false;
        }

        improvement = new { priority, category, issue, action };
        return true;
    }

    private static bool TryReadBounded(JsonElement item, string propertyName, out string value)
    {
        value = string.Empty;
        if (!item.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = Bound(property.GetString(), MaximumImprovementFieldLength);
        return value.Length > 0;
    }

    private static bool TryParse(string value, out JsonDocument document)
    {
        try
        {
            document = JsonDocument.Parse(value, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
                MaxDepth = 64
            });
            return true;
        }
        catch (JsonException)
        {
            document = null!;
            return false;
        }
    }

    private static bool TryExtractBalancedRoot(string value, out string root)
    {
        var start = value.IndexOf('{');
        if (start < 0)
        {
            root = string.Empty;
            return false;
        }

        var depth = 0;
        var inString = false;
        var escaped = false;
        for (var index = start; index < value.Length; index++)
        {
            var character = value[index];
            if (inString)
            {
                if (escaped) escaped = false;
                else if (character == '\\') escaped = true;
                else if (character == '"') inString = false;
                continue;
            }

            if (character == '"') inString = true;
            else if (character == '{') depth++;
            else if (character == '}' && --depth == 0)
            {
                root = value[start..(index + 1)];
                return true;
            }
        }

        root = string.Empty;
        return false;
    }

    private static bool TryExtractScalar(
        string source,
        Regex regex,
        out string value)
    {
        value = string.Empty;
        var match = regex.Match(source);
        if (!match.Success) return false;
        var group = match.Groups["value"];
        if (!group.Success) return false;
        value = group.Value;
        return true;
    }

    private static string StripFence(string? value)
    {
        var candidate = value?.Trim() ?? string.Empty;
        if (!candidate.StartsWith("```", StringComparison.Ordinal)) return candidate;
        var firstNewline = candidate.IndexOf('\n');
        if (firstNewline < 0) return candidate;
        candidate = candidate[(firstNewline + 1)..];
        if (candidate.TrimEnd().EndsWith("```", StringComparison.Ordinal))
        {
            candidate = candidate.TrimEnd()[..^3];
        }
        return candidate.Trim();
    }

    private static string Bound(string? value, int maximumLength)
    {
        var normalized = value?.Trim() ?? string.Empty;
        return normalized.Length <= maximumLength ? normalized : normalized[..maximumLength];
    }

    private static RawJdFallbackRecoveredOutput Empty(string warning) =>
        new(null, string.Empty, Array.Empty<object>(), new[] { warning });

    [GeneratedRegex("\\\"score\\\"\\s*:\\s*(?:\\\"(?<value>[0-9]+(?:\\.[0-9]+)?)\\\"|(?<value>[0-9]+(?:\\.[0-9]+)?))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ScoreRegex();

    [GeneratedRegex("\\\"narrative\\\"\\s*:\\s*\\\"(?<value>[^\\\"\\r\\n]{1,4000})\\\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NarrativeRegex();
}
