namespace ITHunterview.Service.Service.Matching;

public sealed record MatchingHandlerResolution(
    string Category,
    string HandlerCode,
    string MatchLevel,
    decimal Score,
    string OutputStatus);

public sealed record MatchingResultBand(
    string ResultCode,
    string Label,
    decimal LowerInclusive,
    decimal UpperInclusive);

/// <summary>
/// Application-owned numeric policy transcribed from Điểm số Matching (1).xlsx.
/// The provider chooses an enum; only this class assigns its numeric meaning.
/// </summary>
public static class MatchingScorePolicy
{
    public const string Version = "matching-score-policy/v1";

    private static readonly IReadOnlyDictionary<string, decimal> CategoryWeights =
        new Dictionary<string, decimal>(StringComparer.Ordinal)
        {
            ["tech_skill"] = 1m,
            ["experience"] = 0.9m,
            ["domain_knowledge"] = 0.7m,
            ["language"] = 0.6m,
            ["education"] = 0.5m,
            ["soft_skill"] = 0.4m
        };

    private static readonly IReadOnlyDictionary<string, decimal> ImportanceMultipliers =
        new Dictionary<string, decimal>(StringComparer.Ordinal)
        {
            ["must_have"] = 1m,
            ["nice_to_have"] = 0.5m
        };

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, MatchingHandlerResolution>> Handlers =
        BuildHandlers();

    private static readonly IReadOnlyDictionary<string, MatchingHandlerResolution> ScoringHandlersByCode =
        BuildScoringHandlersByCode();

    private static readonly IReadOnlyList<MatchingResultBand> ResultBands =
    [
        new("VERY_SUITABLE", "Rất phù hợp", 85m, 100m),
        new("QUITE_SUITABLE", "Khá phù hợp", 70m, 84.9m),
        new("PARTIAL_FIT", "Phù hợp một phần", 55m, 69.9m),
        new("LIMITED_FIT", "Độ phù hợp còn hạn chế", 40m, 54.9m),
        new("LOW_FIT", "Độ phù hợp thấp", 0m, 39.9m)
    ];

    public static bool TryResolveHandler(
        string category,
        string handlerCode,
        out MatchingHandlerResolution resolution)
    {
        resolution = null!;
        if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(handlerCode))
        {
            return false;
        }

        return Handlers.TryGetValue(category.Trim(), out var categoryHandlers)
            && categoryHandlers.TryGetValue(handlerCode.Trim(), out resolution!);
    }

    public static bool TryResolveHandlerCode(
        string? handlerCode,
        out MatchingHandlerResolution resolution)
    {
        resolution = null!;
        return !string.IsNullOrWhiteSpace(handlerCode)
            && ScoringHandlersByCode.TryGetValue(handlerCode.Trim(), out resolution!);
    }

    public static decimal GetCategoryWeight(string category) =>
        CategoryWeights.TryGetValue(category?.Trim() ?? string.Empty, out var value)
            ? value
            : throw new KeyNotFoundException($"Unsupported matching category '{category}'.");

    public static decimal GetImportanceMultiplier(string importance) =>
        ImportanceMultipliers.TryGetValue(importance?.Trim() ?? string.Empty, out var value)
            ? value
            : throw new KeyNotFoundException($"Unsupported matching importance '{importance}'.");

    public static MatchingResultBand ResolveBand(decimal scorePercent)
    {
        var bounded = Math.Clamp(scorePercent, 0m, 100m);
        return ResultBands.First(band => bounded >= band.LowerInclusive);
    }

    public static IReadOnlyCollection<string> SupportedCategories => CategoryWeights.Keys.ToArray();

    public static IReadOnlyCollection<string> SupportedHandlerCodes =>
        ScoringHandlersByCode.Keys.OrderBy(code => code, StringComparer.Ordinal).ToArray();

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, MatchingHandlerResolution>> BuildHandlers()
    {
        var handlers = new Dictionary<string, IReadOnlyDictionary<string, MatchingHandlerResolution>>(StringComparer.Ordinal)
        {
            ["tech_skill"] = Category("tech_skill",
                Row("H_TECH_01", "NO_EVIDENCE", 0m, "NOT_EVIDENCED"),
                Row("H_TECH_02", "INDIRECT_MATCH", 0.25m, "PARTIALLY_MATCHED"),
                Row("H_TECH_03", "MENTION_ONLY", 0.5m, "PARTIALLY_MATCHED"),
                Row("H_TECH_04", "APPLIED_MATCH", 0.75m, "MATCHED"),
                Row("H_TECH_05", "FULL_MATCH", 1m, "MATCHED")),

            ["experience"] = Category("experience",
                Row("H_EXP_D01", "NO_EVIDENCE", 0m, "NOT_EVIDENCED"),
                Row("H_EXP_D02", "INDIRECT_MATCH", 0.25m, "PARTIALLY_MATCHED"),
                Row("H_EXP_D03", "MENTION_ONLY", 0.5m, "PARTIALLY_MATCHED"),
                Row("H_EXP_D04", "APPLIED_MATCH", 0.75m, "MATCHED"),
                Row("H_EXP_D05", "FULL_MATCH", 1m, "MATCHED"),
                Row("H_EXP_H01", "NO_EVIDENCE", 0m, "NOT_EVIDENCED"),
                Row("H_EXP_H02", "INDIRECT_MATCH", 0.25m, "PARTIALLY_MATCHED"),
                Row("H_EXP_H03", "MENTION_ONLY", 0.5m, "PARTIALLY_MATCHED"),
                Row("H_EXP_H04", "APPLIED_MATCH", 0.75m, "MATCHED"),
                Row("H_EXP_H05", "FULL_MATCH", 1m, "MATCHED")),

            ["education"] = Category("education",
                Row("H_EDU_01", "NO_EVIDENCE", 0m, "NOT_EVIDENCED"),
                Row("H_EDU_02", "NO_MATCH", 0m, "NOT_MET"),
                Row("H_EDU_03", "INDIRECT_MATCH", 0.25m, "PARTIALLY_MATCHED"),
                Row("H_EDU_04", "MENTION_ONLY", 0.5m, "PARTIALLY_MATCHED"),
                Row("H_EDU_05", "APPLIED_MATCH", 0.75m, "MATCHED"),
                Row("H_EDU_06", "FULL_MATCH", 1m, "MATCHED")),

            ["language"] = Category("language",
                Row("H_LANG_Q01", "NO_EVIDENCE", 0m, "NOT_EVIDENCED"),
                Row("H_LANG_Q02", "INDIRECT_MATCH", 0.25m, "PARTIALLY_MATCHED"),
                Row("H_LANG_Q03", "MENTION_ONLY", 0.5m, "PARTIALLY_MATCHED"),
                Row("H_LANG_Q04", "APPLIED_MATCH", 0.75m, "MATCHED"),
                Row("H_LANG_Q05", "FULL_MATCH", 1m, "MATCHED"),
                Row("H_LANG_F01", "NO_EVIDENCE", 0m, "NOT_EVIDENCED"),
                Row("H_LANG_F02", "INDIRECT_MATCH", 0.25m, "PARTIALLY_MATCHED"),
                Row("H_LANG_F03", "MENTION_ONLY", 0.5m, "PARTIALLY_MATCHED"),
                Row("H_LANG_F04", "APPLIED_MATCH", 0.75m, "MATCHED"),
                Row("H_LANG_F05", "FULL_MATCH", 1m, "MATCHED")),

            ["domain_knowledge"] = Category("domain_knowledge",
                Row("H_DOMAIN_01", "NO_EVIDENCE", 0m, "NOT_EVIDENCED"),
                Row("H_DOMAIN_02", "INDIRECT_MATCH", 0.25m, "PARTIALLY_MATCHED"),
                Row("H_DOMAIN_03", "MENTION_ONLY", 0.5m, "PARTIALLY_MATCHED"),
                Row("H_DOMAIN_04", "APPLIED_MATCH", 0.75m, "MATCHED"),
                Row("H_DOMAIN_05", "FULL_MATCH", 1m, "MATCHED")),

            ["soft_skill"] = Category("soft_skill",
                Row("H_SOFT_01", "NO_EVIDENCE", 0m, "NOT_EVIDENCED"),
                Row("H_SOFT_02", "INDIRECT_MATCH", 0.25m, "PARTIALLY_MATCHED"),
                Row("H_SOFT_03", "MENTION_ONLY", 0.5m, "PARTIALLY_MATCHED"),
                Row("H_SOFT_04", "APPLIED_MATCH", 0.75m, "MATCHED"),
                Row("H_SOFT_05", "FULL_MATCH", 1m, "MATCHED"))
        };
        return handlers;
    }

    private static IReadOnlyDictionary<string, MatchingHandlerResolution> BuildScoringHandlersByCode()
    {
        var handlersByCode = new Dictionary<string, MatchingHandlerResolution>(StringComparer.OrdinalIgnoreCase);
        foreach (var resolution in Handlers.Values.SelectMany(categoryHandlers => categoryHandlers.Values))
        {
            if (!handlersByCode.TryAdd(resolution.HandlerCode, resolution))
            {
                throw new InvalidOperationException(
                    $"Duplicate score-bearing matching handler code '{resolution.HandlerCode}'.");
            }
        }

        return handlersByCode;
    }

    private static IReadOnlyDictionary<string, MatchingHandlerResolution> Category(
        string category,
        params (string Code, string Level, decimal Score, string Status)[] rows) =>
        rows.ToDictionary(
            row => row.Code,
            row => new MatchingHandlerResolution(category, row.Code, row.Level, row.Score, row.Status),
            StringComparer.Ordinal);

    private static (string Code, string Level, decimal Score, string Status) Row(
        string code,
        string level,
        decimal score,
        string status) => (code, level, score, status);
}
