using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Service.Matching;

public sealed record JdFitGroupScore(
    ProjectedJdRequirementGroup Group,
    decimal GroupScore,
    decimal CategoryWeight,
    decimal ImportanceMultiplier,
    IReadOnlyList<string> SelectedItemIds,
    IReadOnlyList<string> SatisfiedItemIds,
    int SourceOrder);

public sealed record JdFitScoreResult(
    decimal ScorePercent,
    MatchingResultBand ResultBand,
    IReadOnlyList<JdFitGroupScore> Groups);

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
        if (response.Quality != JdStageTwoOutputQuality.COMPLETE)
        {
            throw new InvalidOperationException("MATCHING_STAGE2_OUTPUT_INVALID");
        }

        var groups = projection.Groups
            .Select((group, index) => CalculateGroup(group, response.ItemAssessments, index))
            .ToArray();
        if (groups.Length == 0)
        {
            throw new InvalidOperationException("MATCHING_SCORE_DENOMINATOR_INVALID");
        }

        var denominator = groups.Sum(group => group.CategoryWeight * group.ImportanceMultiplier);
        if (denominator <= 0m)
        {
            throw new InvalidOperationException("MATCHING_SCORE_DENOMINATOR_INVALID");
        }

        var numerator = groups.Sum(group =>
            group.GroupScore * group.CategoryWeight * group.ImportanceMultiplier);
        var percent = 100m * numerator / denominator;
        return new JdFitScoreResult(percent, MatchingScorePolicy.ResolveBand(percent), groups);
    }

    private static JdFitGroupScore CalculateGroup(
        ProjectedJdRequirementGroup group,
        IReadOnlyDictionary<string, JdStageTwoItemAssessment> assessments,
        int sourceOrder)
    {
        if (group.Items.Count == 0 || group.Items.Any(item => !assessments.ContainsKey(item.ItemId)))
        {
            throw new InvalidOperationException("MATCHING_STAGE2_OUTPUT_INVALID");
        }

        var ordered = group.Items
            .Select(item => assessments[item.ItemId])
            .ToArray();
        var selected = group.Operator switch
        {
            "all_of" => CalculateAllOf(ordered),
            "one_of" => CalculateOneOf(ordered),
            "at_least_n" => CalculateAtLeastN(ordered, group.MinSatisfied),
            _ => throw new InvalidOperationException(JdRequirementProjector.InvalidEffectiveJdAnalysis)
        };
        var groupScore = selected.Average(item => item.Score);
        var categoryWeight = CalculateGroupCategoryWeight(group);
        var importance = MatchingScorePolicy.GetImportanceMultiplier(group.Importance);
        var selectedIds = selected.Select(item => item.ItemId).ToArray();
        var satisfiedIds = ordered.Where(item => item.Score > 0m).Select(item => item.ItemId).ToArray();

        return new JdFitGroupScore(
            group,
            groupScore,
            categoryWeight,
            importance,
            selectedIds,
            satisfiedIds,
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
