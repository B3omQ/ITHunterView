using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Service.Matching;

public sealed record JdCriticalGap(
    string Code,
    string Scope,
    string GroupId,
    string? ItemId,
    string Operator,
    int RequiredCount,
    int SatisfiedCount,
    IReadOnlyList<string> AffectedItemIds);

public sealed record JdCriticalGapEvaluation(
    IReadOnlyList<JdCriticalGap> CriticalGaps,
    IReadOnlyList<string> WarningFlags);

public sealed class JdCriticalGapEvaluator
{
    public JdCriticalGapEvaluation Evaluate(
        JdRequirementProjection projection,
        IReadOnlyDictionary<string, JdStageTwoItemAssessment> assessments)
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(assessments);

        var gaps = new List<JdCriticalGap>();
        foreach (var group in projection.Groups.Where(group => group.Importance == "must_have"))
        {
            var items = group.Items.Select(item => assessments[item.ItemId]).ToArray();
            switch (group.Operator)
            {
                case "all_of":
                    gaps.AddRange(items.Where(item => item.Score == 0m).Select(item => new JdCriticalGap(
                        "CRITICAL_GAP", "item", group.GroupId, item.ItemId, group.Operator,
                        1, 0, new[] { item.ItemId })));
                    break;
                case "one_of" when items.All(item => item.Score == 0m):
                    gaps.Add(new JdCriticalGap(
                        "CRITICAL_GAP", "group", group.GroupId, null, group.Operator,
                        1, 0, items.Select(item => item.ItemId).ToArray()));
                    break;
                case "at_least_n":
                    var satisfied = items.Count(item => item.Score > 0m);
                    if (satisfied < group.MinSatisfied)
                    {
                        gaps.Add(new JdCriticalGap(
                            "CRITICAL_GAP", "group", group.GroupId, null, group.Operator,
                            group.MinSatisfied, satisfied,
                            items.Where(item => item.Score == 0m).Select(item => item.ItemId).ToArray()));
                    }
                    break;
            }
        }

        var warningFlags = new List<string>();
        if (gaps.Count >= 2)
        {
            warningFlags.Add("MULTIPLE_CRITICAL_GAPS");
        }

        var mustHaveTechnicalItems = projection.Groups
            .Where(group => group.Importance == "must_have")
            .SelectMany(group => group.Items)
            .Where(item => item.Category == "tech_skill")
            .ToArray();
        if (mustHaveTechnicalItems.Length > 0 &&
            mustHaveTechnicalItems.All(item => assessments[item.ItemId].Score == 0m))
        {
            warningFlags.Add("CORE_TECH_MISMATCH");
        }

        return new JdCriticalGapEvaluation(gaps, warningFlags);
    }
}
