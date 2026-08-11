using FluentAssertions;
using System.Text.Json;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Service.Matching;

namespace ITHunterview.Service.Tests.Matching;

public sealed class JdCriticalGapEvaluatorTests
{
    [Theory]
    [InlineData("H_EXP_D01", true)]
    [InlineData("H_EXP_D04", false)]
    public void Evaluate_CrossFamilyHandler_UsesResolvedScoreWithoutChangingMustHaveCategory(
        string handlerCode,
        bool expectsGap)
    {
        var group = Group("g", "all_of", 1, "must_have", Item("tech", "tech_skill"));
        var projection = new JdRequirementProjection("jd-analysis/v4", new[] { group }, false);
        using var json = JsonDocument.Parse($$"""
            {"schemaVersion":"jd-stage2/v2","scores":[
              {"reqId":"tech","handlerCode":"{{handlerCode}}"}
            ]}
            """);
        var response = new JdMatchingResponseAdapter().Adapt(json, projection);

        var result = new JdCriticalGapEvaluator().Evaluate(projection, response.ItemAssessments);

        response.ItemAssessments["tech"].Category.Should().Be("tech_skill");
        if (expectsGap)
        {
            result.CriticalGaps.Should().ContainSingle().Which.ItemId.Should().Be("tech");
        }
        else
        {
            result.CriticalGaps.Should().BeEmpty();
        }
    }

    [Fact]
    public void Evaluate_AllOf_EmitsOneItemGapPerZeroWithoutOrSemantics()
    {
        var result = Evaluate(Group("g", "all_of", 3, "must_have",
            Item("a", "tech_skill"), Item("b", "tech_skill"), Item("c", "tech_skill")),
            ("a", 0m), ("b", 0m), ("c", 1m));

        result.CriticalGaps.Should().HaveCount(2);
        result.CriticalGaps.Should().OnlyContain(gap => gap.Scope == "item" && gap.Operator == "all_of");
        result.CriticalGaps.Select(gap => gap.ItemId).Should().Equal("a", "b");
        result.WarningFlags.Should().Contain("MULTIPLE_CRITICAL_GAPS");
    }

    [Fact]
    public void Evaluate_OneOf_WarnsOnlyWhenEveryAlternativeIsZero()
    {
        var group = Group("g", "one_of", 1, "must_have", Item("a", "tech_skill"), Item("b", "tech_skill"));

        Evaluate(group, ("a", 0m), ("b", 0.5m)).CriticalGaps.Should().BeEmpty();
        var allZero = Evaluate(group, ("a", 0m), ("b", 0m));
        allZero.CriticalGaps.Should().ContainSingle()
            .Which.Scope.Should().Be("group");
        allZero.CriticalGaps.Single().AffectedItemIds.Should().Equal("a", "b");
    }

    [Fact]
    public void Evaluate_AtLeastN_UsesPositiveSatisfiedCount()
    {
        var group = Group("g", "at_least_n", 2, "must_have",
            Item("a", "tech_skill"), Item("b", "tech_skill"), Item("c", "tech_skill"), Item("d", "tech_skill"));

        var insufficient = Evaluate(group, ("a", 1m), ("b", 0m), ("c", 0m), ("d", 0m));
        insufficient.CriticalGaps.Should().ContainSingle();
        insufficient.CriticalGaps.Single().RequiredCount.Should().Be(2);
        insufficient.CriticalGaps.Single().SatisfiedCount.Should().Be(1);
        Evaluate(group, ("a", 1m), ("b", 0.25m), ("c", 0m), ("d", 0m))
            .CriticalGaps.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_NiceToHaveNeverCreatesPrimaryGap()
    {
        Evaluate(Group("g", "all_of", 1, "nice_to_have", Item("a", "tech_skill")), ("a", 0m))
            .CriticalGaps.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_AllMustHaveTechZero_EmitsCoreMismatchEvenWithPositiveNonTechItem()
    {
        var projection = new JdRequirementProjection("jd-analysis/v4", new[]
        {
            Group("mixed", "all_of", 2, "must_have", Item("tech", "tech_skill"), Item("lang", "language"))
        }, false);
        var assessments = Assessments(("tech", "tech_skill", 0m), ("lang", "language", 1m));

        var result = new JdCriticalGapEvaluator().Evaluate(projection, assessments);

        result.WarningFlags.Should().Contain("CORE_TECH_MISMATCH");
    }

    [Fact]
    public void Evaluate_MissingAssessment_DoesNotCreateFalseGapOrCoreMismatch()
    {
        var group = Group(
            "g",
            "all_of",
            2,
            "must_have",
            Item("known", "tech_skill"),
            Item("missing", "tech_skill"));
        var projection = new JdRequirementProjection("jd-analysis/v4", new[] { group }, false);

        var result = new JdCriticalGapEvaluator().Evaluate(
            projection,
            Assessments(("known", "tech_skill", 0m)));

        result.CriticalGaps.Should().ContainSingle(gap => gap.ItemId == "known");
        result.CriticalGaps.Should().NotContain(gap => gap.ItemId == "missing");
        result.WarningFlags.Should().NotContain("CORE_TECH_MISMATCH");
    }

    private static JdCriticalGapEvaluation Evaluate(
        ProjectedJdRequirementGroup group,
        params (string ItemId, decimal Score)[] scores)
    {
        var categories = group.Items.ToDictionary(item => item.ItemId, item => item.Category);
        return new JdCriticalGapEvaluator().Evaluate(
            new JdRequirementProjection("jd-analysis/v4", new[] { group }, false),
            Assessments(scores.Select(score => (score.ItemId, categories[score.ItemId], score.Score)).ToArray()));
    }

    private static IReadOnlyDictionary<string, JdStageTwoItemAssessment> Assessments(
        params (string ItemId, string Category, decimal Score)[] values) =>
        values.ToDictionary(
            value => value.ItemId,
            value => new JdStageTwoItemAssessment(
                value.ItemId, value.Category, "handler", value.Score, string.Empty,
                Array.Empty<JdMatchingEvidence>(), Array.Empty<string>()),
            StringComparer.Ordinal);

    private static ProjectedJdRequirementGroup Group(
        string id, string operation, int minSatisfied, string importance,
        params ProjectedJdRequirementItem[] items) =>
        new(id, operation, minSatisfied, importance, items);

    private static ProjectedJdRequirementItem Item(string id, string category) =>
        new(id, category, id, id, id, "requirements", Array.Empty<string>(), null, null, 1m);
}
