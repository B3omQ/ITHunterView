using System;
using System.Collections.Generic;
using System.Linq;

namespace ITHunterview.Service.Service.Matching;

public sealed record JdRequirementGroupData(
    string GroupId,
    string Operator,
    int MinSatisfied,
    string Importance,
    IReadOnlyList<string> SkillNames);

public sealed record JdRequirementGroupOutcome(
    string GroupId,
    string Operator,
    int MatchedItems,
    int RequiredItems,
    bool Satisfied,
    decimal Coverage);

public sealed record JdHardcodeRequirementEvaluation(
    decimal SkillScore,
    IReadOnlyList<JdRequirementGroupOutcome> Outcomes);

public sealed class JdHardcodeRequirementEvaluator
{
    public JdHardcodeRequirementEvaluation Evaluate(
        IReadOnlyList<JdRequirementGroupData> groups,
        IReadOnlyCollection<string> cvSkills)
    {
        var cv = cvSkills.Select(Normalize).Where(value => value.Length > 0).ToHashSet(StringComparer.Ordinal);
        var outcomes = groups.Select(group => EvaluateGroup(group, cv)).ToList();
        if (outcomes.Count == 0) return new JdHardcodeRequirementEvaluation(0m, outcomes);

        decimal numerator = 0m;
        decimal denominator = 0m;
        for (var index = 0; index < outcomes.Count; index++)
        {
            var weight = groups[index].Importance.Equals("must_have", StringComparison.OrdinalIgnoreCase) ? 1m : .25m;
            numerator += outcomes[index].Coverage * weight;
            denominator += weight;
        }
        return new JdHardcodeRequirementEvaluation(denominator == 0m ? 0m : numerator / denominator, outcomes);
    }

    private static JdRequirementGroupOutcome EvaluateGroup(JdRequirementGroupData group, HashSet<string> cvSkills)
    {
        var skills = group.SkillNames.Select(Normalize).Where(value => value.Length > 0).Distinct(StringComparer.Ordinal).ToList();
        var matched = skills.Count(cvSkills.Contains);
        var required = group.Operator.Equals("all_of", StringComparison.OrdinalIgnoreCase) ? skills.Count : Math.Max(1, group.MinSatisfied);
        var coverage = group.Operator.Equals("one_of", StringComparison.OrdinalIgnoreCase)
            ? matched > 0 ? 1m : 0m
            : required == 0 ? 0m : Math.Min(1m, (decimal)matched / required);
        return new JdRequirementGroupOutcome(group.GroupId, group.Operator, matched, required, coverage == 1m, coverage);
    }

    private static string Normalize(string? value) => string.Join(" ", (value ?? string.Empty)
        .Trim().ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
