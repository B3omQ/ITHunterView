using System;
using System.IO;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Service.Matching;
using Xunit;

namespace ITHunterview.Service.Tests.Matching;

public sealed class JdRequirementProjectorTests
{
    [Fact]
    public void Project_EffectiveV1_PreservesStructuralIdentityOrderAndSourceMeaning()
    {
        const string effective = """
            {
              "schema_version":"jd-analysis-effective/v1",
              "analysis_quality":"PARTIAL",
              "matching_metrics":{"requirement_groups":[
                {
                  "group_id":"grp-001",
                  "source_requirement_id":"req-004",
                  "intent":"experience_duration",
                  "operator":"one_of",
                  "min_satisfied":1,
                  "importance":"must_have",
                  "source_section":"requirements",
                  "requirement_verbatim":"Có ít nhất 3 năm backend với Java hoặc Go.",
                  "items":[
                    {"item_id":"grp-001:item-001","category":"experience","skill_name":"Java backend experience","raw_mention":"Java","min_years":3},
                    {"item_id":"grp-001:item-002","category":"experience","skill_name":"Go backend experience","raw_mention":"Go","min_years":3,"max_years":5}
                  ]
                }
              ]}
            }
            """;

        var projection = new JdRequirementProjector().Project(effective);

        Assert.Equal("jd-analysis-effective/v1", projection.SourceSchemaVersion);
        Assert.False(projection.UsesLegacySemantics);
        Assert.Equal(JdAnalysisQuality.PARTIAL, projection.Quality);
        var group = Assert.Single(projection.Groups);
        Assert.Equal("grp-001", group.GroupId);
        Assert.Equal("req-004", group.SourceRequirementId);
        Assert.Equal("experience_duration", group.Intent);
        Assert.Equal("one_of", group.Operator);
        Assert.Equal(1, group.MinSatisfied);
        Assert.Equal("Có ít nhất 3 năm backend với Java hoặc Go.", group.RequirementVerbatim);
        Assert.Equal(new[] { "grp-001:item-001", "grp-001:item-002" }, group.Items.Select(item => item.ItemId));
        Assert.Equal(new[] { "Java backend experience", "Go backend experience" }, group.Items.Select(item => item.SkillName));
        Assert.All(group.Items, item => Assert.Equal("requirements", item.SourceSection));
    }

    [Fact]
    public void Project_V4CompactGroup_RemainsReadableForHistoricalRows()
    {
        const string v4 = """
            {"schema_version":"jd-analysis/v4","matching_metrics":{"requirement_groups":[{"operator":"all_of","importance":"must_have","source_section":"requirements","requirement_verbatim":"Java and Spring Boot.","items":[{"category":"tech_skill","skill_name":"Java","raw_mention":"Java"},{"category":"tech_skill","skill_name":"Spring Boot","raw_mention":"Spring Boot"}]}]}}
            """;

        var projection = new JdRequirementProjector().Project(v4);

        Assert.False(projection.UsesLegacySemantics);
        var group = Assert.Single(projection.Groups);
        Assert.Equal("legacy-v4-001", group.GroupId);
        Assert.Equal(2, group.MinSatisfied);
        Assert.Equal(new[] { "legacy-v4-001:item-001", "legacy-v4-001:item-002" }, group.Items.Select(item => item.ItemId));
    }

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
    public void Project_InvalidGroupCardinality_ReturnsInvalidProjectionWithoutThrowing()
    {
        const string invalid = """
            {"schema_version":"jd-analysis/v3","matching_metrics":{"requirement_groups":[{"group_id":"grp-001","operator":"one_of","min_satisfied":2,"importance":"must_have","items":[{"category":"tech_skill","skill_name":"react"}]}]}}
            """;
        var projector = new JdRequirementProjector();

        var projection = projector.Project(invalid);

        Assert.Equal(JdAnalysisQuality.INVALID, projection.Quality);
        Assert.Empty(projection.Groups);
    }

    [Fact]
    public void Project_EffectiveV1_InvalidGroupDoesNotDiscardValidSiblingOrPoisonItsIds()
    {
        const string effective = """
            {
              "schema_version":"jd-analysis-effective/v1",
              "analysis_quality":"COMPLETE",
              "analysis_coverage":{"input_group_count":2,"accepted_group_count":2,"discarded_group_count":0,"input_item_count":2,"accepted_item_count":2,"discarded_item_count":0,"requirement_set_complete":true},
              "matching_metrics":{"requirement_groups":[
                {
                  "group_id":"grp-001","source_requirement_id":"req-001","intent":"qualification",
                  "operator":"one_of","min_satisfied":2,"importance":"must_have","source_section":"requirements",
                  "requirement_verbatim":"Invalid cardinality.",
                  "items":[{"item_id":"shared:item-001","category":"tech_skill","skill_name":"bad","raw_mention":"bad","min_years":null,"max_years":null}]
                },
                {
                  "group_id":"grp-002","source_requirement_id":"req-002","intent":"qualification",
                  "operator":"all_of","min_satisfied":1,"importance":"must_have","source_section":"requirements",
                  "requirement_verbatim":"Valid sibling.",
                  "items":[{"item_id":"shared:item-001","category":"tech_skill","skill_name":"Java","raw_mention":"Java","min_years":null,"max_years":null}]
                }
              ]}
            }
            """;

        var projection = new JdRequirementProjector().Project(effective);

        Assert.Equal(JdAnalysisQuality.PARTIAL, projection.Quality);
        var group = Assert.Single(projection.Groups);
        Assert.Equal("grp-002", group.GroupId);
        Assert.Equal("shared:item-001", Assert.Single(group.Items).ItemId);
        Assert.Contains(projection.Diagnostics!, diagnostic => diagnostic.Code == "PROJECTOR_GROUP_DROPPED");
        Assert.False(projection.RequirementSetComplete);
    }

    [Fact]
    public void Project_EffectiveV1_MalformedQualityMetadata_KeepsUsableGroupAsPartial()
    {
        const string effective = """
            {"schema_version":"jd-analysis-effective/v1","analysis_quality":"maybe","analysis_coverage":{"input_group_count":1,"accepted_group_count":1,"discarded_group_count":0,"input_item_count":1,"accepted_item_count":1,"discarded_item_count":0,"requirement_set_complete":true},"matching_metrics":{"requirement_groups":[{"group_id":"grp-001","source_requirement_id":"req-001","intent":"qualification","operator":"all_of","min_satisfied":1,"importance":"must_have","source_section":"requirements","requirement_verbatim":"Java.","items":[{"item_id":"grp-001:item-001","category":"tech_skill","skill_name":"Java","raw_mention":"Java","min_years":null,"max_years":null}]}]}}
            """;

        var projection = new JdRequirementProjector().Project(effective);

        Assert.Equal(JdAnalysisQuality.PARTIAL, projection.Quality);
        Assert.Single(projection.Groups);
        Assert.Contains(projection.Diagnostics!, diagnostic =>
            diagnostic.Code == "PROJECTOR_QUALITY_METADATA_INVALID");
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
