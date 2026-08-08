using System;
using System.Collections.Generic;
using System.Linq;

namespace ITHunterview.Service.Service.Matching;

/// <summary>
/// The handler code is an output enum, not a free-form explanation. Keeping the
/// allowlist in backend code prevents a model from inventing a score semantics.
/// </summary>
public static class MatchingHandlerCodePolicy
{
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> Codes =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            ["tech_skill"] = CodesFor("H_TECH", 1, 5),
            ["experience"] = CodesFor("H_EXP", 1, 6),
            ["seniority_fit"] = CodesFor("H_SENIOR", 1, 6),
            ["domain_knowledge"] = CodesFor("H_DOMAIN", 1, 4),
            ["language"] = CodesFor("H_LANG", 1, 6),
            ["education"] = CodesFor("H_EDU", 1, 6),
            ["soft_skill"] = CodesFor("H_SOFT", 1, 4)
        };

    private static readonly IReadOnlySet<string> AllCodes =
        Codes.Values.SelectMany(codes => codes).ToHashSet(StringComparer.Ordinal);

    public static bool IsValid(string? category, string? handlerCode)
        => category is not null
            && handlerCode is not null
            && Codes.TryGetValue(category, out var categoryCodes)
            && categoryCodes.Contains(handlerCode.Trim());

    public static bool IsKnown(string? handlerCode)
        => !string.IsNullOrWhiteSpace(handlerCode) && AllCodes.Contains(handlerCode.Trim());

    private static IReadOnlySet<string> CodesFor(string prefix, int first, int last)
        => Enumerable.Range(first, last - first + 1)
            .Select(number => $"{prefix}_{number:00}")
            .ToHashSet(StringComparer.Ordinal);
}
