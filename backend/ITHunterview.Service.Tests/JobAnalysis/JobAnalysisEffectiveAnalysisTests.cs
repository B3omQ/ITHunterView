using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Reflection;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.JobAnalysis;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Service;
using ITHunterview.Service.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ITHunterview.Service.Tests.JobAnalysis;

public class JobAnalysisEffectiveAnalysisTests
{
    [Fact]
    public void SerializeEffectiveAnalysis_WhenDictionarySkillsAreFiltered_PreservesEveryDetailedRequirement()
    {
        var service = new JobAnalysisExtractionService(
            Mock.Of<IAiService>(),
            Mock.Of<IPromptManagementService>(),
            Mock.Of<IJdAnalysisResponseValidator>(),
            NullLogger<JobAnalysisExtractionService>.Instance);

        var analysis = new ValidatedJobAnalysis
        {
            JobTitlesNormalized = new List<string> { "business analyst" },
            SkillsNormalized = new List<ValidatedSkillMention>
            {
                new() { Name = "jira", Category = "tech_skill", Importance = "must_have" },
                new() { Name = "kubernetes", Category = "tech_skill", Importance = "nice_to_have" }
            },
            RequirementsList = new List<ValidatedRequirementItem>
            {
                new() { SkillName = "Jira", Category = "tech_skill", Importance = "must_have" },
                new() { SkillName = "Kubernetes", Category = "tech_skill", Importance = "nice_to_have" },
                new() { SkillName = "3 years experience", Category = "experience", Importance = "must_have" }
            }
        };

        string json = service.SerializeEffectiveAnalysis(analysis, new HashSet<string> { "jira" });
        using var document = JsonDocument.Parse(json);
        var metrics = document.RootElement.GetProperty("matching_metrics");

        var skills = metrics.GetProperty("skills_normalized").EnumerateArray().ToList();
        var requirements = metrics.GetProperty("requirements_list").EnumerateArray().ToList();

        Assert.Single(skills);
        Assert.Equal("jira", skills[0].GetProperty("name").GetString());
        Assert.Equal(3, requirements.Count);
        Assert.Contains(requirements, item => item.GetProperty("skill_name").GetString() == "Kubernetes");
        Assert.Contains(requirements, item => item.GetProperty("skill_name").GetString() == "3 years experience");
    }

    [Fact]
    public void SerializeEffectiveAnalysis_WithV3Groups_PreservesGroupLogicAndEvidence()
    {
        var service = new JobAnalysisExtractionService(
            Mock.Of<IAiService>(),
            Mock.Of<IPromptManagementService>(),
            Mock.Of<IJdAnalysisResponseValidator>(),
            NullLogger<JobAnalysisExtractionService>.Instance);
        var analysis = new ValidatedJobAnalysis
        {
            SchemaVersion = "jd-analysis/v3",
            RequirementGroups = new List<ValidatedRequirementGroup>
            {
                new()
                {
                    GroupId = "grp-001", Operator = "one_of", MinSatisfied = 1, Importance = "must_have",
                    Items = new List<ValidatedRequirementItem>
                    {
                        new() { SkillName = "react", Category = "tech_skill", Evidences = new List<string> { "React, Angular, or Vue" } }
                    }
                }
            }
        };

        using var document = JsonDocument.Parse(service.SerializeEffectiveAnalysis(analysis));
        var group = document.RootElement.GetProperty("matching_metrics").GetProperty("requirement_groups")[0];

        Assert.Equal("one_of", group.GetProperty("operator").GetString());
        Assert.Equal(1, group.GetProperty("min_satisfied").GetInt32());
        Assert.Equal("React, Angular, or Vue", group.GetProperty("items")[0].GetProperty("evidences")[0].GetString());
    }

    [Fact]
    public void RepositoryDecisionProjection_PreservesQualityMetadata()
    {
        const string source = """
            {
              "schema_version":"jd-analysis/v3",
              "analysis_quality":"PARTIAL",
              "analysis_coverage":{"input_group_count":2,"accepted_group_count":1,"discarded_group_count":1,"input_item_count":2,"accepted_item_count":1,"discarded_item_count":1,"requirement_set_complete":false},
              "analysis_diagnostics":[{"code":"OUTPUT_TRUNCATED","json_path":"$"}],
              "matching_metrics":{"job_titles_normalized":[],"skills_normalized":[],"total_years_exp":0,"domains":[],"requirements_list":[],"requirement_groups":[]}
            }
            """;
        var method = typeof(JobAnalysisRepository).GetMethod(
            "RebuildEffectiveAnalysisWithAcceptedStandardSkills",
            BindingFlags.NonPublic | BindingFlags.Static);

        var result = method!.Invoke(null, new object?[] { source, Array.Empty<JobSkillDecisions>() }) as string;

        using var document = JsonDocument.Parse(result!);
        document.RootElement.GetProperty("analysis_quality").GetString().Should().Be("PARTIAL");
        document.RootElement.GetProperty("analysis_coverage").GetProperty("accepted_group_count").GetInt32().Should().Be(1);
        document.RootElement.GetProperty("analysis_diagnostics")[0].GetProperty("code").GetString().Should().Be("OUTPUT_TRUNCATED");
    }
}
