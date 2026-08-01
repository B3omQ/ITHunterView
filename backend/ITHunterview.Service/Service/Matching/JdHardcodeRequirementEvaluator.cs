using System;
using System.Collections.Generic;
using System.Linq;
using ITHunterview.Service.DTOs.Cv.Matching;

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
    decimal Coverage,
    bool EvaluatedBySkillComponent = true,
    string? NotEvaluatedReason = null,
    IReadOnlyList<string>? MatchedItemIds = null);

public sealed record JdHardcodeRequirementEvaluation(
    decimal SkillScore,
    IReadOnlyList<JdRequirementGroupOutcome> Outcomes);

public sealed class JdHardcodeRequirementEvaluator
{
    public JdHardcodeRequirementEvaluation Evaluate(
        JdRequirementProjection projection,
        IReadOnlyCollection<string> cvSkills)
    {
        var cv = cvSkills.Select(Normalize).Where(value => value.Length > 0).ToHashSet(StringComparer.Ordinal);
        var outcomes = projection.Groups.Select(group => EvaluateGroup(group, cv)).ToList();
        var evaluatedOutcomes = outcomes
            .Select((outcome, index) => new { Outcome = outcome, Group = projection.Groups[index] })
            .Where(value => value.Outcome.EvaluatedBySkillComponent)
            .ToList();

        if (evaluatedOutcomes.Count == 0)
        {
            return new JdHardcodeRequirementEvaluation(0.5m, outcomes);
        }

        decimal numerator = 0m;
        decimal denominator = 0m;
        foreach (var item in evaluatedOutcomes)
        {
            var weight = item.Group.Importance.Equals("must_have", StringComparison.OrdinalIgnoreCase) ? 1m : .25m;
            numerator += item.Outcome.Coverage * weight;
            denominator += weight;
        }

        return new JdHardcodeRequirementEvaluation(numerator / denominator, outcomes);
    }

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

    private static JdRequirementGroupOutcome EvaluateGroup(ProjectedJdRequirementGroup group, HashSet<string> cvSkills)
    {
        if (group.Items.Any(item => !item.Category.Equals("tech_skill", StringComparison.Ordinal)))
        {
            return new JdRequirementGroupOutcome(
                group.GroupId,
                group.Operator,
                0,
                group.Items.Count,
                false,
                0m,
                false,
                "non_technical_group",
                Array.Empty<string>());
        }

        var items = group.Items
            .Select(item => new { item.ItemId, Skill = Normalize(item.SkillName) })
            .Where(item => item.Skill.Length > 0)
            .ToList();
        var matchedItems = items.Where(item => cvSkills.Contains(item.Skill)).ToList();
        var required = group.Operator.Equals("all_of", StringComparison.OrdinalIgnoreCase) ? items.Count : Math.Max(1, group.MinSatisfied);
        var coverage = group.Operator.Equals("one_of", StringComparison.OrdinalIgnoreCase)
            ? matchedItems.Count > 0 ? 1m : 0m
            : required == 0 ? 0m : Math.Min(1m, (decimal)matchedItems.Count / required);

        return new JdRequirementGroupOutcome(
            group.GroupId,
            group.Operator,
            matchedItems.Count,
            required,
            coverage == 1m,
            coverage,
            true,
            null,
            matchedItems.Select(item => item.ItemId).ToList());
    }

    private static string Normalize(string? value) => string.Join(" ", (value ?? string.Empty)
        .Trim().ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
