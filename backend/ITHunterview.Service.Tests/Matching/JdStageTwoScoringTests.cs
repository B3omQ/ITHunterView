using FluentAssertions;
using System.Text.Json;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Service.Matching;

namespace ITHunterview.Service.Tests.Matching;

public sealed class JdStageTwoScoringTests
{
    [Fact]
    public void Calculate_CrossFamilyHandler_KeepsProjectionCategoryWeight()
    {
        var projection = Projection(Group(
            "tech", "all_of", 1, "must_have", Item("tech:item", "tech_skill")));
        using var json = JsonDocument.Parse("""
            {"schemaVersion":"jd-stage2/v2","scores":[
              {"reqId":"tech:item","handlerCode":"H_EXP_D04"}
            ]}
            """);
        var response = new JdMatchingResponseAdapter().Adapt(json, projection);

        var result = new JdFitScoreCalculator().Calculate(projection, response);

        response.ItemAssessments["tech:item"].Category.Should().Be("tech_skill");
        response.ItemAssessments["tech:item"].Score.Should().Be(0.75m);
        result.Groups.Single().CategoryWeight.Should().Be(1m);
        result.ScorePercent.Should().Be(75m);
    }

    [Fact]
    public void Calculate_AllOf_AveragesEveryItemAndSelectsAll()
    {
        var projection = Projection(Group(
            "all", "all_of", 3, "must_have",
            Item("a", "tech_skill"), Item("b", "tech_skill"), Item("c", "tech_skill")));

        var result = new JdFitScoreCalculator().Calculate(
            projection,
            Response(projection, ("a", 1m), ("b", 0.5m), ("c", 0m)));

        result.ScorePercent.Should().Be(50m);
        result.Groups.Single().GroupScore.Should().Be(0.5m);
        result.Groups.Single().SelectedItemIds.Should().Equal("a", "b", "c");
    }

    [Fact]
    public void Calculate_OneOf_SelectsMaximumAndOneGroupDenominatorUnit()
    {
        var projection = Projection(Group(
            "one", "one_of", 1, "must_have",
            Item("a", "tech_skill"), Item("b", "tech_skill"), Item("c", "tech_skill")));

        var result = new JdFitScoreCalculator().Calculate(
            projection,
            Response(projection, ("a", 0m), ("b", 0.75m), ("c", 0.5m)));

        result.ScorePercent.Should().Be(75m);
        result.Groups.Single().SelectedItemIds.Should().Equal("b");
    }

    [Fact]
    public void Calculate_OneOfTie_IsStableWhenProjectionOrderReverses()
    {
        var first = Projection(Group(
            "one", "one_of", 1, "must_have",
            Item("b", "tech_skill"), Item("a", "tech_skill")));
        var reversed = first with
        {
            Groups = new[] { first.Groups[0] with { Items = first.Groups[0].Items.Reverse().ToArray() } }
        };

        var firstResult = new JdFitScoreCalculator().Calculate(first, Response(first, ("a", 0.75m), ("b", 0.75m)));
        var reversedResult = new JdFitScoreCalculator().Calculate(reversed, Response(reversed, ("b", 0.75m), ("a", 0.75m)));

        firstResult.Groups.Single().SelectedItemIds.Should().Equal("a");
        reversedResult.Groups.Single().SelectedItemIds.Should().Equal("a");
        firstResult.ScorePercent.Should().Be(reversedResult.ScorePercent);
    }

    [Fact]
    public void Calculate_AtLeastN_SelectsDeterministicTopNAndAveragesThem()
    {
        var projection = Projection(Group(
            "n", "at_least_n", 2, "must_have",
            Item("a", "tech_skill"), Item("b", "tech_skill"),
            Item("c", "tech_skill"), Item("d", "tech_skill")));

        var result = new JdFitScoreCalculator().Calculate(
            projection,
            Response(projection, ("a", 1m), ("b", 0.75m), ("c", 0.5m), ("d", 0m)));

        result.ScorePercent.Should().Be(87.5m);
        result.Groups.Single().SelectedItemIds.Should().Equal("a", "b");
    }

    [Fact]
    public void Calculate_MixedCategoryGroup_UsesMeanDistinctCategoryWeight()
    {
        var projection = Projection(Group(
            "mixed", "one_of", 1, "must_have",
            Item("tech", "tech_skill"), Item("lang", "language")));

        var result = new JdFitScoreCalculator().Calculate(
            projection,
            Response(projection, ("tech", 0.5m), ("lang", 1m)));

        result.ScorePercent.Should().Be(100m);
        result.Groups.Single().CategoryWeight.Should().Be(0.8m);
        result.Groups.Single().SelectedItemIds.Should().Equal("lang");
    }

    [Fact]
    public void Calculate_ExtraAlternatives_DoNotIncreaseSourceGroupWeight()
    {
        var oneItem = Projection(Group("one", "one_of", 1, "must_have", Item("a", "tech_skill")));
        var threeItems = Projection(Group(
            "one", "one_of", 1, "must_have",
            Item("a", "tech_skill"), Item("b", "tech_skill"), Item("c", "tech_skill")));

        var first = new JdFitScoreCalculator().Calculate(oneItem, Response(oneItem, ("a", 0.75m)));
        var second = new JdFitScoreCalculator().Calculate(
            threeItems,
            Response(threeItems, ("a", 0.75m), ("b", 0m), ("c", 0m)));

        first.ScorePercent.Should().Be(75m);
        second.ScorePercent.Should().Be(75m);
        first.Groups.Single().CategoryWeight.Should().Be(second.Groups.Single().CategoryWeight);
    }

    [Fact]
    public void Calculate_NoPoolBonus_MissingImportanceClassDoesNotGrantFreePoints()
    {
        var projection = Projection(
            Group("must", "all_of", 1, "must_have", Item("m", "tech_skill")),
            Group("nice", "all_of", 1, "nice_to_have", Item("n", "tech_skill")));

        var result = new JdFitScoreCalculator().Calculate(
            projection,
            Response(projection, ("m", 0m), ("n", 1m)));

        result.ScorePercent.Should().BeApproximately(33.333333333333333333333333333m, 0.000000000000000000000000001m);
    }

    [Theory]
    [InlineData("VERY_SUITABLE", 85)]
    [InlineData("QUITE_SUITABLE", 84.9)]
    [InlineData("QUITE_SUITABLE", 70)]
    [InlineData("PARTIAL_FIT", 69.9)]
    [InlineData("PARTIAL_FIT", 55)]
    [InlineData("LIMITED_FIT", 54.9)]
    [InlineData("LIMITED_FIT", 40)]
    [InlineData("LOW_FIT", 39.9)]
    [InlineData("LOW_FIT", 0)]
    public void ResolveResultBand_UsesExactWorkbookBoundaries(string code, double value)
    {
        MatchingScorePolicy.ResolveBand((decimal)value).ResultCode.Should().Be(code);
    }

    private static JdStageTwoValidatedResponse Response(
        JdRequirementProjection projection,
        params (string ItemId, decimal Score)[] values)
    {
        var categories = projection.Groups.SelectMany(group => group.Items)
            .ToDictionary(item => item.ItemId, item => item.Category, StringComparer.Ordinal);
        var assessments = values.ToDictionary(
            value => value.ItemId,
            value => new JdStageTwoItemAssessment(
                value.ItemId,
                categories[value.ItemId],
                Handler(categories[value.ItemId], value.Score),
                value.Score,
                "Reasoning",
                Array.Empty<JdMatchingEvidence>(),
                Array.Empty<string>()),
            StringComparer.Ordinal);
        return new JdStageTwoValidatedResponse(
            assessments,
            "Narrative",
            JdStageTwoOutputQuality.COMPLETE,
            new JdStageTwoOutputCoverage(assessments.Count, assessments.Count, assessments.Count, 0, Array.Empty<string>(), false),
            Array.Empty<string>());
    }

    private static string Handler(string category, decimal score) => (category, score) switch
    {
        ("tech_skill", 0m) => "H_TECH_01",
        ("tech_skill", 0.5m) => "H_TECH_03",
        ("tech_skill", 0.75m) => "H_TECH_04",
        ("tech_skill", 1m) => "H_TECH_05",
        ("language", 1m) => "H_LANG_F05",
        _ => throw new InvalidOperationException()
    };

    private static JdRequirementProjection Projection(params ProjectedJdRequirementGroup[] groups) =>
        new("jd-analysis/v4", groups, false);

    private static ProjectedJdRequirementGroup Group(
        string id,
        string operation,
        int minSatisfied,
        string importance,
        params ProjectedJdRequirementItem[] items) =>
        new(id, operation, minSatisfied, importance, items, "requirements", $"Requirement {id}", id, id);

    private static ProjectedJdRequirementItem Item(string id, string category) =>
        new(id, category, id, id, id, "requirements", Array.Empty<string>(), null, null,
            JdRequirementCategoryWeights.Get(category));
}
