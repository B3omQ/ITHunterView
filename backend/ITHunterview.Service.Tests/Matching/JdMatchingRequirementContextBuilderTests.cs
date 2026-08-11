using System.Text.Json;
using FluentAssertions;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Service.Matching;

namespace ITHunterview.Service.Tests.Matching;

public sealed class JdMatchingRequirementContextBuilderTests
{
    [Fact]
    public void Build_MixedCategoryGroup_EmitsOneEntryPerItemWithoutFlattening()
    {
        var projection = Projection(
            new ProjectedJdRequirementGroup(
                "grp-001",
                "all_of",
                2,
                "must_have",
                new[]
                {
                    Item("grp-001:tech", "tech_skill", "React", 3, null, "React experience"),
                    Item("grp-001:exp", "experience", "software development", null, 5, "At least 5 years")
                },
                "requirements",
                "React and at least 5 years of software development experience"));

        var result = new JdMatchingRequirementContextBuilder().Build(projection);
        using var document = JsonDocument.Parse(result.Json);
        var entries = document.RootElement.EnumerateArray().ToArray();

        result.GroupCount.Should().Be(1);
        result.RequirementCount.Should().Be(2);
        entries.Should().HaveCount(2);
        entries[0].GetProperty("ReqId").GetString().Should().Be("grp-001:tech");
        entries[1].GetProperty("ReqId").GetString().Should().Be("grp-001:exp");
        entries[0].GetProperty("Category").GetString().Should().Be("tech_skill");
        entries[1].GetProperty("Category").GetString().Should().Be("experience");
        entries[0].GetProperty("RequirementVerbatim").GetString()
            .Should().Be("React and at least 5 years of software development experience");
        entries[1].GetProperty("SourceSection").GetString().Should().Be("requirements");
    }

    [Fact]
    public void Build_PreservesIdsAndGroupOperatorMetadata()
    {
        var projection = Projection(
            new ProjectedJdRequirementGroup(
                "grp-choice",
                "one_of",
                1,
                "nice_to_have",
                new[]
                {
                    Item("grp-choice:one", "tech_skill", "Redis", null, null, "Redis"),
                    Item("grp-choice:two", "tech_skill", "RabbitMQ", null, null, "RabbitMQ")
                },
                "nice_to_have",
                "Redis or RabbitMQ"));

        var result = new JdMatchingRequirementContextBuilder().Build(projection);
        using var document = JsonDocument.Parse(result.Json);

        foreach (var entry in document.RootElement.EnumerateArray())
        {
            entry.GetProperty("Operator").GetString().Should().Be("one_of");
            entry.GetProperty("MinSatisfied").GetInt32().Should().Be(1);
            entry.GetProperty("Importance").GetString().Should().Be("nice_to_have");
            entry.GetProperty("GroupId").GetString().Should().Be("grp-choice");
        }

        document.RootElement.EnumerateArray()
            .Select(entry => entry.GetProperty("ReqId").GetString())
            .Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Build_PreservesYearsRawMentionAndEvidence()
    {
        var item = new ProjectedJdRequirementItem(
            "grp-years:item",
            "experience",
            "Java development",
            "Professional Java development",
            "3 years Java",
            "requirements",
            new[] { "Worked on Java services", "Delivered production features" },
            3,
            8,
            0.9m);
        var projection = Projection(new ProjectedJdRequirementGroup(
            "grp-years", "all_of", 1, "must_have", new[] { item }, "requirements", "3 years Java"));

        var result = new JdMatchingRequirementContextBuilder().Build(projection);
        using var document = JsonDocument.Parse(result.Json);
        var entry = document.RootElement[0];

        entry.GetProperty("MinYears").GetInt32().Should().Be(3);
        entry.GetProperty("MaxYears").GetInt32().Should().Be(8);
        entry.GetProperty("RawMention").GetString().Should().Be("3 years Java");
        entry.GetProperty("Evidence").EnumerateArray().Select(value => value.GetString())
            .Should().Equal("Worked on Java services", "Delivered production features");
    }

    [Fact]
    public void Build_EmptyProjection_FailsBeforeProviderUsage()
    {
        var projection = new JdRequirementProjection(
            "jd-analysis/v4",
            Array.Empty<ProjectedJdRequirementGroup>(),
            false);

        var action = () => new JdMatchingRequirementContextBuilder().Build(projection);

        action.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Be(JdRequirementProjector.InvalidEffectiveJdAnalysis);
    }

    [Fact]
    public void Build_ReorderingInputChangesOnlyDeterministicOrder()
    {
        var first = Projection(
            new ProjectedJdRequirementGroup(
                "grp-order", "at_least_n", 1, "must_have",
                new[]
                {
                    Item("grp-order:a", "tech_skill", "A", null, null, "A"),
                    Item("grp-order:b", "tech_skill", "B", null, null, "B")
                }));
        var reversed = Projection(
            first.Groups[0] with { Items = first.Groups[0].Items.Reverse().ToArray() });

        var firstJson = JsonDocument.Parse(new JdMatchingRequirementContextBuilder().Build(first).Json);
        var reversedJson = JsonDocument.Parse(new JdMatchingRequirementContextBuilder().Build(reversed).Json);

        reversedJson.RootElement.EnumerateArray()
            .Select(entry => entry.GetProperty("ReqId").GetString())
            .Should().Equal("grp-order:b", "grp-order:a");
        firstJson.RootElement.EnumerateArray()
            .Select(entry => entry.GetProperty("ReqId").GetString())
            .Should().Equal("grp-order:a", "grp-order:b");
        reversedJson.RootElement.EnumerateArray().Select(entry => entry.GetProperty("NormalizedText").GetString())
            .Should().Equal("B", "A");
    }

    [Fact]
    public void Build_IncludedItemIds_EmitsOnlyRequestedItemsWithOriginalGroupMetadata()
    {
        var projection = Projection(new ProjectedJdRequirementGroup(
            "grp-retry", "at_least_n", 2, "must_have",
            new[]
            {
                Item("grp-retry:a", "tech_skill", "A", null, null, "A"),
                Item("grp-retry:b", "tech_skill", "B", null, null, "B"),
                Item("grp-retry:c", "tech_skill", "C", null, null, "C")
            },
            "requirements",
            "Any two of A, B and C"));

        var result = new JdMatchingRequirementContextBuilder().Build(
            projection,
            new HashSet<string>(new[] { "grp-retry:c" }, StringComparer.Ordinal));
        using var document = JsonDocument.Parse(result.Json);

        result.GroupCount.Should().Be(1);
        result.RequirementCount.Should().Be(1);
        document.RootElement[0].GetProperty("ReqId").GetString().Should().Be("grp-retry:c");
        document.RootElement[0].GetProperty("Operator").GetString().Should().Be("at_least_n");
        document.RootElement[0].GetProperty("MinSatisfied").GetInt32().Should().Be(2);
        document.RootElement[0].GetProperty("RequirementVerbatim").GetString()
            .Should().Be("Any two of A, B and C");
    }

    [Theory]
    [InlineData("unknown", "all_of", 1)]
    [InlineData("tech_skill", "unsupported", 1)]
    [InlineData("tech_skill", "one_of", 2)]
    public void Build_InvalidCurrentProjection_FailsBeforeProviderUsage(
        string category,
        string operation,
        int minSatisfied)
    {
        var projection = Projection(new ProjectedJdRequirementGroup(
            "grp-invalid", operation, minSatisfied, "must_have",
            new[]
            {
                new ProjectedJdRequirementItem(
                    "grp-invalid:a", category, "A", "A", "A", "requirements",
                    new[] { "A" }, null, null, 1m)
            }));

        var action = () => new JdMatchingRequirementContextBuilder().Build(projection);

        action.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Be(JdRequirementProjector.InvalidEffectiveJdAnalysis);
    }

    private static JdRequirementProjection Projection(params ProjectedJdRequirementGroup[] groups) =>
        new("jd-analysis/v4", groups, false);

    private static ProjectedJdRequirementItem Item(
        string id,
        string category,
        string skill,
        int? minYears,
        int? maxYears,
        string evidence) => new(
        id,
        category,
        skill,
        skill,
        skill,
        "requirements",
        new[] { evidence },
        minYears,
        maxYears,
        JdRequirementCategoryWeights.Get(category));
}
