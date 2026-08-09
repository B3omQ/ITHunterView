using ITHunterview.Domain.Enums;
using ITHunterview.Service.Utils;
using Xunit;

namespace ITHunterview.Service.Tests.JobAnalysis;

public sealed class JdAnalysisResponseValidatorV5Tests
{
    private readonly JdAnalysisResponseValidator _validator = new();

    [Fact]
    public void ValidateV5_CompleteFixture_CoversEveryOperatorAndCategoryWithoutSemanticRewrite()
    {
        var json = ReadFixture("jd-analysis-v5-complete.json");

        var result = _validator.Validate(json, new JobAnalysisInputSnapshot());

        Assert.True(result.IsValid);
        Assert.Equal(JdAnalysisQuality.COMPLETE, result.Quality);
        Assert.Equal(9, result.Data!.RequirementGroups.Count);
        Assert.Equal(
            new[] { "all_of", "at_least_n", "one_of" },
            result.Data.RequirementGroups.Select(group => group.Operator).Distinct().Order());
        Assert.Equal(
            new[] { "domain_knowledge", "education", "experience", "language", "soft_skill", "tech_skill" },
            result.Data.RequirementGroups.SelectMany(group => group.Items)
                .Select(item => item.Category).Distinct().Order());
        Assert.Equal(2, result.Data.RequirementGroups.Count(group => group.SourceRequirementId == "req-004"));
        Assert.Equal(
            new[] { "experience_duration", "qualification" },
            result.Data.RequirementGroups.Where(group => group.SourceRequirementId == "req-004")
                .Select(group => group.Intent));
        Assert.DoesNotContain("confidence", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("seniority_fit", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateV5_InvalidStructureFixture_IsInvalidWithoutInventingRequirements()
    {
        var result = _validator.Validate(
            ReadFixture("jd-analysis-v5-invalid-structure.json"),
            new JobAnalysisInputSnapshot());

        Assert.False(result.IsUsable);
        Assert.Equal(JdAnalysisQuality.INVALID, result.Quality);
        Assert.Equal("MISSING_REQUIREMENT_GROUPS", result.FailureCode);
    }

    [Fact]
    public void ValidateV5_WithCompletePayload_PreservesProviderOrderAndTransportMeaning()
    {
        const string json = """
            {
              "schema_version":"jd-analysis/v5",
              "matching_metrics":{
                "job_titles_normalized":["Backend Engineer"],
                "total_years_exp":3,
                "domains":["FinTech"],
                "requirement_groups":[
                  {
                    "source_requirement_id":"req-004",
                    "intent":"experience_duration",
                    "operator":"all_of",
                    "importance":"must_have",
                    "source_section":"requirements",
                    "requirement_verbatim":"At least 3 years building backend systems.",
                    "items":[
                      {"category":"experience","skill_name":"Backend development experience","raw_mention":"3 years","min_years":3},
                      {"category":"tech_skill","skill_name":"Java","raw_mention":"backend systems"}
                    ]
                  },
                  {
                    "source_requirement_id":"req-005",
                    "intent":"qualification",
                    "operator":"one_of",
                    "importance":"nice_to_have",
                    "source_section":"description",
                    "requirement_verbatim":"React or Vue is useful.",
                    "items":[
                      {"category":"tech_skill","skill_name":"React","raw_mention":"React"},
                      {"category":"tech_skill","skill_name":"Vue.js","raw_mention":"Vue"}
                    ]
                  }
                ]
              }
            }
            """;

        var result = _validator.Validate(json, new JobAnalysisInputSnapshot());

        Assert.True(result.IsValid);
        Assert.Equal(JdAnalysisQuality.COMPLETE, result.Quality);
        Assert.Equal("jd-analysis/v5", result.Data!.SchemaVersion);
        Assert.Equal(new[] { "Backend Engineer" }, result.Data.JobTitlesNormalized);
        Assert.Equal(new[] { "FinTech" }, result.Data.Domains);
        Assert.Equal(new[] { "req-004", "req-005" }, result.Data.RequirementGroups.Select(group => group.SourceRequirementId));
        Assert.Equal("experience_duration", result.Data.RequirementGroups[0].Intent);
        Assert.Equal("all_of", result.Data.RequirementGroups[0].Operator);
        Assert.Equal("At least 3 years building backend systems.", result.Data.RequirementGroups[0].RequirementVerbatim);
        Assert.Equal(new[] { "Backend development experience", "Java" }, result.Data.RequirementGroups[0].Items.Select(item => item.SkillName));
        Assert.Equal(3, result.Data.RequirementGroups[0].Items[0].MinYears);
        Assert.Equal("one_of", result.Data.RequirementGroups[1].Operator);
        Assert.Equal(1, result.Data.RequirementGroups[1].MinSatisfied);
    }

    [Fact]
    public void ValidateV5_WithMissingTechnicalMarkers_RetainsGroupAsPartialUsingNeutralValues()
    {
        const string json = """
            {"schema_version":"jd-analysis/v5","matching_metrics":{"job_titles_normalized":[],"total_years_exp":0,"domains":[],"requirement_groups":[{
              "operator":"all_of","importance":"must_have","requirement_verbatim":"An unusual but usable requirement.",
              "items":[{"category":"soft_skill","skill_name":"Context-shaped collaboration","raw_mention":"collaboration"}]
            }]}}
            """;

        var result = _validator.Validate(json, new JobAnalysisInputSnapshot());

        Assert.False(result.IsValid);
        Assert.True(result.IsUsable);
        Assert.Equal(JdAnalysisQuality.PARTIAL, result.Quality);
        var group = Assert.Single(result.Data!.RequirementGroups);
        Assert.Equal("req-recovered-001", group.SourceRequirementId);
        Assert.Equal("unspecified", group.Intent);
        Assert.Equal("unknown", group.SourceSection);
        Assert.Equal("Context-shaped collaboration", Assert.Single(group.Items).SkillName);
    }

    [Fact]
    public void ValidateV5_DiscardsOnlyInvalidGroupAndItem_WhenAnotherItemRemains()
    {
        const string json = """
            {"schema_version":"jd-analysis/v5","matching_metrics":{"job_titles_normalized":[],"total_years_exp":0,"domains":[],"requirement_groups":[
              {"source_requirement_id":"req-001","intent":"qualification","operator":"some_of","importance":"must_have","source_section":"requirements","requirement_verbatim":"Broken operator","items":[{"category":"tech_skill","skill_name":"Java","raw_mention":"Java"}]},
              {"source_requirement_id":"req-002","intent":"qualification","operator":"all_of","importance":"must_have","source_section":"requirements","requirement_verbatim":"Java and an invalid item","items":[{"category":"tech_skill","skill_name":"Java","raw_mention":"Java"},{"category":"made_up","skill_name":"Invalid","raw_mention":"Invalid"}]}
            ]}}
            """;

        var result = _validator.Validate(json, new JobAnalysisInputSnapshot());

        Assert.True(result.IsUsable);
        Assert.Equal(JdAnalysisQuality.PARTIAL, result.Quality);
        var group = Assert.Single(result.Data!.RequirementGroups);
        Assert.Equal("req-002", group.SourceRequirementId);
        Assert.Equal("Java", Assert.Single(group.Items).SkillName);
        Assert.Equal(1, group.MinSatisfied);
        Assert.Equal(2, result.Data.Coverage.InputGroupCount);
        Assert.Equal(1, result.Data.Coverage.AcceptedGroupCount);
        Assert.Equal(3, result.Data.Coverage.InputItemCount);
        Assert.Equal(1, result.Data.Coverage.AcceptedItemCount);
    }

    [Fact]
    public void ValidateV5_InvalidAtLeastNCardinality_DiscardsOnlyThatGroup()
    {
        const string json = """
            {"schema_version":"jd-analysis/v5","matching_metrics":{"job_titles_normalized":[],"total_years_exp":0,"domains":[],"requirement_groups":[
              {"source_requirement_id":"req-001","intent":"qualification","operator":"at_least_n","min_satisfied":3,"importance":"must_have","source_section":"requirements","requirement_verbatim":"Choose two","items":[{"category":"tech_skill","skill_name":"A","raw_mention":"A"},{"category":"tech_skill","skill_name":"B","raw_mention":"B"}]},
              {"source_requirement_id":"req-002","intent":"qualification","operator":"all_of","importance":"must_have","source_section":"requirements","requirement_verbatim":"Keep Java","items":[{"category":"tech_skill","skill_name":"Java","raw_mention":"Java"}]}
            ]}}
            """;

        var result = _validator.Validate(json, new JobAnalysisInputSnapshot());

        Assert.True(result.IsUsable);
        Assert.Equal(JdAnalysisQuality.PARTIAL, result.Quality);
        Assert.Equal("req-002", Assert.Single(result.Data!.RequirementGroups).SourceRequirementId);
    }

    [Fact]
    public void ValidateV5_DoesNotMergeAliasesOrSortGroups()
    {
        const string json = """
            {"schema_version":"jd-analysis/v5","matching_metrics":{"job_titles_normalized":[],"total_years_exp":0,"domains":[],"requirement_groups":[
              {"source_requirement_id":"req-002","intent":"qualification","operator":"all_of","importance":"must_have","source_section":"requirements","requirement_verbatim":"NodeJS","items":[{"category":"tech_skill","skill_name":"NodeJS","raw_mention":"NodeJS"}]},
              {"source_requirement_id":"req-001","intent":"qualification","operator":"all_of","importance":"must_have","source_section":"requirements","requirement_verbatim":"node.js","items":[{"category":"tech_skill","skill_name":"node.js","raw_mention":"node.js"}]}
            ]}}
            """;

        var result = _validator.Validate(json, new JobAnalysisInputSnapshot());

        Assert.True(result.IsValid);
        Assert.Equal(new[] { "NodeJS", "node.js" }, result.Data!.RequirementGroups.Select(group => group.Items.Single().SkillName));
    }

    [Fact]
    public void ValidateV5_ExactTransportDuplicateWithSameSourceId_RemovesOnlyDuplicate()
    {
        const string group = """{"source_requirement_id":"req-001","intent":"qualification","operator":"all_of","importance":"must_have","source_section":"requirements","requirement_verbatim":"Java","items":[{"category":"tech_skill","skill_name":"Java","raw_mention":"Java"}]}""";
        var json = "{\"schema_version\":\"jd-analysis/v5\",\"matching_metrics\":{\"job_titles_normalized\":[],\"total_years_exp\":0,\"domains\":[],\"requirement_groups\":[" + group + "," + group + "]}}";

        var result = _validator.Validate(json, new JobAnalysisInputSnapshot());

        Assert.True(result.IsUsable);
        Assert.Equal(JdAnalysisQuality.PARTIAL, result.Quality);
        Assert.Single(result.Data!.RequirementGroups);
        Assert.Contains(result.Data.Diagnostics, diagnostic => diagnostic.Code == "EXACT_DUPLICATE_GROUP_REMOVED");
    }

    [Fact]
    public void ValidateV5_InvalidYears_RemoveOnlyYearsAndRetainItemAsPartial()
    {
        const string json = """
            {"schema_version":"jd-analysis/v5","matching_metrics":{"job_titles_normalized":[],"total_years_exp":0,"domains":[],"requirement_groups":[{
              "source_requirement_id":"req-001","intent":"experience_duration","operator":"all_of","importance":"must_have","source_section":"requirements","requirement_verbatim":"Experience range",
              "items":[{"category":"experience","skill_name":"Backend experience","raw_mention":"experience","min_years":5,"max_years":2}]
            }]}}
            """;

        var result = _validator.Validate(json, new JobAnalysisInputSnapshot());

        Assert.True(result.IsUsable);
        Assert.Equal(JdAnalysisQuality.PARTIAL, result.Quality);
        var item = Assert.Single(Assert.Single(result.Data!.RequirementGroups).Items);
        Assert.Null(item.MinYears);
        Assert.Null(item.MaxYears);
    }

    [Fact]
    public void ValidateV5_ExplicitEmptyRequirementGroups_IsComplete()
    {
        const string json = """
            {"schema_version":"jd-analysis/v5","matching_metrics":{"job_titles_normalized":[],"total_years_exp":0,"domains":[],"requirement_groups":[]}}
            """;

        var result = _validator.Validate(json, new JobAnalysisInputSnapshot());

        Assert.True(result.IsValid);
        Assert.Equal(JdAnalysisQuality.COMPLETE, result.Quality);
        Assert.Empty(result.Data!.RequirementGroups);
    }

    [Fact]
    public void ValidateV5_WhenProviderAttemptedGroupsButNoneAreUsable_IsInvalid()
    {
        const string json = """
            {"schema_version":"jd-analysis/v5","matching_metrics":{"job_titles_normalized":[],"total_years_exp":0,"domains":[],"requirement_groups":[{"operator":"invalid","importance":"must_have","items":[]}]}}
            """;

        var result = _validator.Validate(json, new JobAnalysisInputSnapshot());

        Assert.False(result.IsUsable);
        Assert.Equal(JdAnalysisQuality.INVALID, result.Quality);
        Assert.Equal("NO_USABLE_REQUIREMENT_GROUPS", result.FailureCode);
    }

    private static string ReadFixture(string name) => File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "JobAnalysis", "Fixtures", name));
}
