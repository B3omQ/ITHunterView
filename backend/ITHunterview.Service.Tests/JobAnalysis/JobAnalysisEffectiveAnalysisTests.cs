using System.Text.Json;
using FluentAssertions;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Constant.Prompts;
using ITHunterview.Service.DTOs.JobAnalysis;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Service;
using ITHunterview.Service.Service.Matching;
using ITHunterview.Service.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ITHunterview.Service.Tests.JobAnalysis;

public sealed class JobAnalysisEffectiveAnalysisTests
{
    [Fact]
    public void SkillYearsFixture_ValidatorEffectiveSerializerAndProjectorPreserveTheLinkedGroups()
    {
        var providerJson = ReadFixture("jd-analysis-v5-skill-years.json");
        var validation = new JdAnalysisResponseValidator().Validate(providerJson, new JobAnalysisInputSnapshot());

        validation.IsValid.Should().BeTrue();
        var effectiveJson = CreateService().SerializeEffectiveAnalysis(validation.Data!);
        var projection = new JdRequirementProjector().Project(effectiveJson);

        projection.SourceSchemaVersion.Should().Be(JdAnalysisEffectiveContract.SchemaVersion);
        projection.UsesLegacySemantics.Should().BeFalse();
        projection.Groups.Should().HaveCount(2);
        projection.Groups.Select(group => group.SourceRequirementId).Should().Equal("req-004", "req-004");
        projection.Groups.Select(group => group.Intent).Should().Equal("experience_duration", "qualification");
        projection.Groups[0].Operator.Should().Be("all_of");
        projection.Groups[0].Items.Should().ContainSingle()
            .Which.MinYears.Should().Be(3);
        projection.Groups[1].Operator.Should().Be("one_of");
        projection.Groups[1].Items.Select(item => item.SkillName)
            .Should().Equal("java", "node.js", "python", "go");
        projection.Groups.Select(group => group.RequirementVerbatim).Distinct()
            .Should().ContainSingle()
            .Which.Should().Be("Có ít nhất 3 năm kinh nghiệm phát triển backend bằng Java, NodeJS, Python hoặc Golang.");
    }

    [Fact]
    public void SerializeEffectiveAnalysis_UsesExactCompactEffectiveV1Shape()
    {
        var analysis = CreateAnalysis();

        var json = CreateService().SerializeEffectiveAnalysis(analysis);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        root.GetProperty("schema_version").GetString().Should().Be(JdAnalysisEffectiveContract.SchemaVersion);
        root.GetProperty("analysis_quality").GetString().Should().Be("PARTIAL");
        var coverage = root.GetProperty("analysis_coverage");
        coverage.GetProperty("input_group_count").GetInt32().Should().Be(3);
        coverage.GetProperty("accepted_group_count").GetInt32().Should().Be(3);
        coverage.GetProperty("was_truncated").GetBoolean().Should().BeTrue();
        coverage.TryGetProperty("requirement_set_complete", out _).Should().BeFalse();

        var metrics = root.GetProperty("matching_metrics");
        metrics.GetProperty("skills_normalized").EnumerateArray()
            .Select(element => element.GetString()).Should().Equal("Java", "NodeJS", "node.js");
        metrics.TryGetProperty("requirements_list", out _).Should().BeFalse();
        metrics.TryGetProperty("seniority_fit", out _).Should().BeFalse();

        var groups = metrics.GetProperty("requirement_groups");
        groups[0].GetProperty("group_id").GetString().Should().Be("grp-001");
        groups[0].GetProperty("source_requirement_id").GetString().Should().Be("req-004");
        groups[0].GetProperty("intent").GetString().Should().Be("qualification");
        groups[0].GetProperty("operator").GetString().Should().Be("one_of");
        groups[0].GetProperty("min_satisfied").GetInt32().Should().Be(1);
        groups[0].GetProperty("items")[0].GetProperty("item_id").GetString().Should().Be("grp-001:item-001");
        groups[0].GetProperty("items")[0].GetProperty("skill_name").GetString().Should().Be("Java");
    }

    [Fact]
    public void SerializeEffectiveAnalysis_DoesNotPersistProviderOrLegacyDuplicateFields()
    {
        var json = CreateService().SerializeEffectiveAnalysis(CreateAnalysis());

        json.Should().NotContain("confidence")
            .And.NotContain("detail_verbatim")
            .And.NotContain("evidences")
            .And.NotContain("requirements_list")
            .And.NotContain("seniority_fit")
            .And.NotContain("jd-analysis/v5");
        using var document = JsonDocument.Parse(json);
        var item = document.RootElement.GetProperty("matching_metrics").GetProperty("requirement_groups")[0].GetProperty("items")[0];
        item.TryGetProperty("source_section", out _).Should().BeFalse();
        item.TryGetProperty("evidence", out _).Should().BeFalse();
        item.TryGetProperty("min_years", out _).Should().BeFalse();
    }

    [Fact]
    public void SerializeEffectiveAnalysis_IsDeterministicAndPreservesSourceOrderWithoutAliasMerging()
    {
        var analysis = CreateAnalysis();
        var service = CreateService();

        var first = service.SerializeEffectiveAnalysis(analysis);
        var second = service.SerializeEffectiveAnalysis(analysis);

        second.Should().Be(first);
        using var document = JsonDocument.Parse(first);
        var groups = document.RootElement.GetProperty("matching_metrics").GetProperty("requirement_groups");
        groups[0].GetProperty("requirement_verbatim").GetString().Should().Be("Java or NodeJS or node.js");
        groups[1].GetProperty("requirement_verbatim").GetString().Should().Be("Financial services experience");
        groups[2].GetProperty("requirement_verbatim").GetString().Should().Be("At least 3 years");
        groups[2].GetProperty("items")[0].GetProperty("min_years").GetInt32().Should().Be(3);
    }

    private static ValidatedJobAnalysis CreateAnalysis() => new()
    {
        SchemaVersion = "jd-analysis/v5",
        Quality = JdAnalysisQuality.PARTIAL,
        Coverage = new JdAnalysisCoverage(3, 3, 0, 6, 6, 0, false),
        Diagnostics = new List<JdAnalysisDiagnostic> { new("OUTPUT_TRUNCATED", "$") },
        JobTitlesNormalized = new List<string> { "Backend Engineer", "backend engineer", "Platform Engineer" },
        Domains = new List<string> { "FinTech", "fintech" },
        TotalYearsExp = 3,
        RequirementGroups = new List<ValidatedRequirementGroup>
        {
            new()
            {
                GroupId = "semantic-hash-that-must-not-leak",
                SourceRequirementId = "req-004",
                Intent = "qualification",
                Operator = "one_of",
                MinSatisfied = 1,
                Importance = "must_have",
                SourceSection = "requirements",
                RequirementVerbatim = "Java or NodeJS or node.js",
                Items = new List<ValidatedRequirementItem>
                {
                    new() { Category = "tech_skill", SkillName = "Java", RawMention = "Java" },
                    new() { Category = "tech_skill", SkillName = "java", RawMention = "java" },
                    new() { Category = "tech_skill", SkillName = "NodeJS", RawMention = "NodeJS" },
                    new() { Category = "tech_skill", SkillName = "node.js", RawMention = "node.js" }
                }
            },
            new()
            {
                SourceRequirementId = "req-005",
                Intent = "qualification",
                Operator = "all_of",
                MinSatisfied = 1,
                Importance = "must_have",
                SourceSection = "requirements",
                RequirementVerbatim = "Financial services experience",
                Items = new List<ValidatedRequirementItem>
                {
                    new() { Category = "domain_knowledge", SkillName = "financial services", RawMention = "Financial services" }
                }
            },
            new()
            {
                SourceRequirementId = "req-006",
                Intent = "experience_duration",
                Operator = "all_of",
                MinSatisfied = 1,
                Importance = "must_have",
                SourceSection = "requirements",
                RequirementVerbatim = "At least 3 years",
                Items = new List<ValidatedRequirementItem>
                {
                    new() { Category = "experience", SkillName = "backend experience", RawMention = "3 years", MinYears = 3 }
                }
            }
        }
    };

    private static JobAnalysisExtractionService CreateService() => new(
        Mock.Of<IAiService>(),
        Mock.Of<IPromptManagementService>(),
        Mock.Of<IJdAnalysisResponseValidator>(),
        NullLogger<JobAnalysisExtractionService>.Instance);

    private static string ReadFixture(string name) => File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "JobAnalysis", "Fixtures", name));
}
