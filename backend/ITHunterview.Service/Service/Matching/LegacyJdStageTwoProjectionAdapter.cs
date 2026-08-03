using System;
using System.Collections.Generic;
using System.Linq;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Service.Matching;

public sealed record LegacyStageTwoRequirement(
    string ReqId,
    string NormalizedText,
    string Category,
    string Importance,
    string DetailVerbatim,
    decimal CategoryWeight,
    string Operator,
    int MinSatisfied,
    string Evidence);

/// <summary>
/// Projects validated JD analysis into the active legacy Stage 2 contract.
/// A legacy score has exactly one handler/category, so mixed-category groups
/// cannot be represented without changing their meaning and must fail closed.
/// </summary>
public static class LegacyJdStageTwoProjectionAdapter
{
    public static IReadOnlyList<LegacyStageTwoRequirement> Adapt(JdRequirementProjection projection)
    {
        ArgumentNullException.ThrowIfNull(projection);

        var requirements = new List<LegacyStageTwoRequirement>(projection.Groups.Count);
        foreach (var group in projection.Groups)
        {
            if (group.Items.Count == 0)
                throw new InvalidOperationException("INVALID_EFFECTIVE_JD_ANALYSIS");

            var categories = group.Items
                .Select(item => item.Category)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (categories.Length != 1)
                throw new InvalidOperationException("MATCHING_LEGACY_CONTRACT_UNREPRESENTABLE");

            var category = categories[0];
            requirements.Add(new LegacyStageTwoRequirement(
                group.GroupId,
                JoinDistinct(group.Items.Select(item => item.SkillName), " | "),
                category,
                group.Importance,
                JoinDistinct(group.Items.Select(item => item.DetailVerbatim), "; "),
                JdRequirementCategoryWeights.Get(category),
                group.Operator,
                group.MinSatisfied,
                JoinDistinct(group.Items.SelectMany(item => item.Evidences), "; ")));
        }

        return requirements;
    }

    private static string JoinDistinct(IEnumerable<string?> values, string separator)
        => string.Join(
            separator,
            values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim())
                .Distinct(StringComparer.Ordinal));
}
