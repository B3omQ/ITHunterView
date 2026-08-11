namespace ITHunterview.Service.Service.Matching;

/// <summary>
/// The handler code is an output enum, not a free-form explanation. Keeping the
/// allowlist in backend code prevents a model from inventing a score semantics.
/// </summary>
public static class MatchingHandlerCodePolicy
{
    private static readonly HashSet<string> NonScoringCodes = new(
        ["H_EXP_00", "H_EDU_00", "H_LANG_00"],
        StringComparer.OrdinalIgnoreCase);

    public static bool IsValid(string? category, string? handlerCode)
        => category is not null && handlerCode is not null
            && MatchingScorePolicy.TryResolveHandler(category, handlerCode, out _);

    public static bool IsKnown(string? handlerCode)
        => MatchingScorePolicy.TryResolveHandlerCode(handlerCode, out _)
            || IsNonScoringCode(handlerCode);

    public static bool IsNonScoringCode(string? handlerCode)
        => !string.IsNullOrWhiteSpace(handlerCode)
            && NonScoringCodes.Contains(handlerCode.Trim());
}
