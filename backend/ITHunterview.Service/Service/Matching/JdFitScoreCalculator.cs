using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Service.Matching;

public sealed record JdFitGroupScore(
    ProjectedJdRequirementGroup Group,
    decimal? GroupScore,
    decimal CategoryWeight,
    decimal ImportanceMultiplier,
    IReadOnlyList<string> SelectedItemIds,
    IReadOnlyList<string> SatisfiedItemIds,
    IReadOnlyList<string> ResolvedItemIds,
    IReadOnlyList<string> MissingItemIds,
    bool IsComplete,
    bool ContributesToAggregate,
    int SourceOrder);

public sealed record JdFitScoreResult(
    decimal? ScorePercent,
    MatchingResultBand? ResultBand,
    IReadOnlyList<JdFitGroupScore> Groups,
    MatchingCompletionDisposition CompletionDisposition)
{
    public bool ScoreAvailable => CompletionDisposition == MatchingCompletionDisposition.ScoredBillable;
}

/// <summary>
/// Pure workbook-driven group calculator. Every source requirement contributes
/// one denominator unit regardless of the number of alternatives it contains.
/// </summary>
public sealed class JdFitScoreCalculator
{
    public JdFitScoreResult Calculate(
        JdRequirementProjection projection,
        JdStageTwoValidatedResponse response)
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(response);
        var groups = projection.Groups
            .Select((group, index) => CalculateGroup(group, response.ItemAssessments, index))
            .ToArray();
        if (groups.Length == 0 || response.Quality != JdStageTwoOutputQuality.COMPLETE ||
            groups.Any(group => !group.IsComplete))
        {
            return new JdFitScoreResult(
                null,
                null,
                groups,
                MatchingCompletionDisposition.UnscoredRefundable);
        }

        var contributingGroups = groups.Where(group => group.ContributesToAggregate).ToArray();
        var denominator = contributingGroups.Sum(group => group.CategoryWeight * group.ImportanceMultiplier);
        if (denominator <= 0m)
        {
            return new JdFitScoreResult(
                null,
                null,
                groups,
                MatchingCompletionDisposition.UnscoredRefundable);
        }

        var numerator = contributingGroups.Sum(group =>
            group.GroupScore!.Value * group.CategoryWeight * group.ImportanceMultiplier);
        var percent = 100m * numerator / denominator;
        return new JdFitScoreResult(
            percent,
            MatchingScorePolicy.ResolveBand(percent),
            groups,
            MatchingCompletionDisposition.ScoredBillable);
    }

    private static JdFitGroupScore CalculateGroup(
        ProjectedJdRequirementGroup group,
        IReadOnlyDictionary<string, JdStageTwoItemAssessment> assessments,
        int sourceOrder)
    {
        var resolved = group.Items
            .Where(item => assessments.ContainsKey(item.ItemId))
            .Select(item => assessments[item.ItemId])
            .ToArray();
        var missingIds = group.Items
            .Where(item => !assessments.ContainsKey(item.ItemId))
            .Select(item => item.ItemId)
            .ToArray();
        var isComplete = group.Items.Count > 0 && missingIds.Length == 0;
        var selected = isComplete
            ? group.Operator switch
            {
                "all_of" => CalculateAllOf(resolved),
                "one_of" => CalculateOneOf(resolved),
                "at_least_n" => CalculateAtLeastN(resolved, group.MinSatisfied),
                _ => throw new InvalidOperationException(JdRequirementProjector.InvalidEffectiveJdAnalysis)
            }
            : Array.Empty<JdStageTwoItemAssessment>();
        decimal? groupScore = isComplete && selected.Count > 0
            ? selected.Average(item => item.Score)
            : null;
        var categoryWeight = CalculateGroupCategoryWeight(group);
        var importance = MatchingScorePolicy.GetImportanceMultiplier(group.Importance);
        var selectedIds = selected.Select(item => item.ItemId).ToArray();
        var satisfiedIds = resolved.Where(item => item.Score > 0m).Select(item => item.ItemId).ToArray();

        return new JdFitGroupScore(
            group,
            groupScore,
            categoryWeight,
            importance,
            selectedIds,
            satisfiedIds,
            resolved.Select(item => item.ItemId).ToArray(),
            missingIds,
            isComplete,
            isComplete && groupScore.HasValue,
            sourceOrder);
    }

    private static IReadOnlyList<JdStageTwoItemAssessment> CalculateAllOf(
        IReadOnlyList<JdStageTwoItemAssessment> items) => items;

    private static IReadOnlyList<JdStageTwoItemAssessment> CalculateOneOf(
        IReadOnlyList<JdStageTwoItemAssessment> items) =>
        new[]
        {
            items.OrderByDescending(item => item.Score)
                .ThenBy(item => item.ItemId, StringComparer.Ordinal)
                .First()
        };

    private static IReadOnlyList<JdStageTwoItemAssessment> CalculateAtLeastN(
        IReadOnlyList<JdStageTwoItemAssessment> items,
        int minimum) =>
        items.OrderByDescending(item => item.Score)
            .ThenBy(item => item.ItemId, StringComparer.Ordinal)
            .Take(minimum)
            .ToArray();

    private static decimal CalculateGroupCategoryWeight(ProjectedJdRequirementGroup group) =>
        group.Items.Select(item => item.Category)
            .Distinct(StringComparer.Ordinal)
            .Select(MatchingScorePolicy.GetCategoryWeight)
            .Average();
}
