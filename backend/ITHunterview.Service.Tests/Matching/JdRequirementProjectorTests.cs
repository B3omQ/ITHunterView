using System;
using System.IO;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Service.Matching;
using Xunit;

namespace ITHunterview.Service.Tests.Matching;

public sealed class JdRequirementProjectorTests
{
    [Fact]
    public void Project_V3_KeepsCategoryAndAlternativeGroupSemantics()
    {
        var projector = new JdRequirementProjector();

        var projection = projector.Project(ReadFixture("jd-v3-category-groups.json"));

        var alternatives = Assert.Single(projection.Groups, group => group.GroupId == "grp-001");
        Assert.Equal("jd-analysis/v3", projection.SourceSchemaVersion);
        Assert.Equal("one_of", alternatives.Operator);
        Assert.Equal(1, alternatives.MinSatisfied);
        Assert.Equal(new[] { "tech_skill", "tech_skill", "tech_skill" }, alternatives.Items.Select(item => item.Category));
        Assert.All(alternatives.Items, item => Assert.StartsWith("grp-001:itm-", item.ItemId, StringComparison.Ordinal));
        Assert.Equal(3, alternatives.Items.Select(item => item.ItemId).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Project_V3_PreservesNonTechnicalAndMixedCategoriesWithConfiguredWeights()
    {
        var projector = new JdRequirementProjector();

        var projection = projector.Project(ReadFixture("jd-v3-category-groups.json"));

        Assert.Equal(0.9m, projection.Groups.Single(group => group.GroupId == "grp-002").Items.Single().CategoryWeight);
        Assert.Equal(0.6m, projection.Groups.Single(group => group.GroupId == "grp-003").Items.Single().CategoryWeight);
        Assert.Equal(0.5m, projection.Groups.Single(group => group.GroupId == "grp-004").Items.Single().CategoryWeight);
        Assert.Equal(new[] { "tech_skill", "domain_knowledge" }, projection.Groups.Single(group => group.GroupId == "grp-005").Items.Select(item => item.Category));
    }

    [Fact]
    public void Project_V2_AdaptsEachFlatRequirementToStableSingletonGroup()
    {
        var projector = new JdRequirementProjector();

        var projection = projector.Project(ReadFixture("jd-v2-flat-requirements.json"));

        Assert.True(projection.UsesLegacySemantics);
        Assert.Equal(new[] { "legacy-001", "legacy-002" }, projection.Groups.Select(group => group.GroupId));
        Assert.All(projection.Groups, group =>
        {
            Assert.Equal("all_of", group.Operator);
            Assert.Equal(1, group.MinSatisfied);
            Assert.Single(group.Items);
        });
    }

    [Fact]
    public void Project_InvalidGroupCardinality_FailsClosed()
    {
        const string invalid = """
            {"schema_version":"jd-analysis/v3","matching_metrics":{"requirement_groups":[{"group_id":"grp-001","operator":"one_of","min_satisfied":2,"importance":"must_have","items":[{"category":"tech_skill","skill_name":"react"}]}]}}
            """;
        var projector = new JdRequirementProjector();

        var exception = Assert.Throws<InvalidOperationException>((Action)(() => projector.Project(invalid)));

        Assert.Equal("INVALID_EFFECTIVE_JD_ANALYSIS", exception.Message);
    }

    [Fact]
    public void Project_ExplicitInvalidQuality_RemainsInvalid()
    {
        const string analysis = """
            {"schema_version":"jd-analysis/v3","analysis_quality":"INVALID","matching_metrics":{"requirement_groups":[{"group_id":"grp-001","operator":"all_of","min_satisfied":1,"importance":"must_have","items":[{"category":"tech_skill","skill_name":"react"}]}]}}
            """;
        var projector = new JdRequirementProjector();

        var projection = projector.Project(analysis);

        Assert.Equal(JdAnalysisQuality.INVALID, projection.Quality);
    }

    private static string ReadFixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Matching", "Fixtures", name));
}
