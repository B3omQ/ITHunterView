using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Service.Matching;

namespace ITHunterview.Service.Tests.Matching;

public sealed class CvAnalysisResponseValidatorTests
{
    private readonly CvAnalysisResponseValidator _sut = new();

    [Fact]
    public void ValidV2Document_ReturnsCompleteAndPreservesAiMetrics()
    {
        var result = _sut.ValidateAndCanonicalize(CreateValidDocument());

        result.Quality.Should().Be(CvAnalysisQuality.COMPLETE);
        result.IsUsable.Should().BeTrue();
        result.Diagnostics.Should().BeEmpty();
        using var canonical = JsonDocument.Parse(result.CanonicalJson);
        canonical.RootElement.GetProperty("matching_metrics").GetProperty("total_years_exp").GetInt32().Should().Be(7);
        canonical.RootElement.GetProperty("matching_evidence").GetProperty("experience_summary")
            .GetProperty("total_professional_months").GetInt32().Should().Be(11);
        canonical.RootElement.GetProperty("matching_metrics").GetProperty("skills_normalized")[0]
            .GetString().Should().Be("ReactJS");
    }

    [Fact]
    public void CurrentPeriodWithEndDate_ReturnsCompleteAndPreservesPeriod()
    {
        var root = ParseValid();
        var period = root["matching_evidence"]!["experience_summary"]!["periods"]![0]!.AsObject();
        period["is_current"] = true;
        period["end_year"] = 2026;
        period["end_month"] = 7;

        var result = _sut.ValidateAndCanonicalize(root.ToJsonString());

        result.Quality.Should().Be(CvAnalysisQuality.COMPLETE);
        using var canonical = JsonDocument.Parse(result.CanonicalJson);
        var actual = canonical.RootElement.GetProperty("matching_evidence").GetProperty("experience_summary")
            .GetProperty("periods")[0];
        actual.GetProperty("is_current").GetBoolean().Should().BeTrue();
        actual.GetProperty("end_year").GetInt32().Should().Be(2026);
    }

    [Fact]
    public void UnknownProperty_IsIgnoredWithoutLoweringQuality()
    {
        var root = ParseValid();
        root["provider_note"] = "must never enter canonical output";

        var result = _sut.ValidateAndCanonicalize(root.ToJsonString());

        result.Quality.Should().Be(CvAnalysisQuality.COMPLETE);
        JsonNode.Parse(result.CanonicalJson)!["provider_note"].Should().BeNull();
    }

    [Fact]
    public void UngroundedEvidence_RemainsCompleteWhenAllJsonTypesAreValid()
    {
        var root = ParseValid();
        root["matching_evidence"]!["requirement_signals"]![0]!["evidence"]![0] = "AI supplied evidence";

        var result = _sut.ValidateAndCanonicalize(root.ToJsonString());

        result.Quality.Should().Be(CvAnalysisQuality.COMPLETE);
        result.IsUsable.Should().BeTrue();
    }

    [Fact]
    public void MetricWithoutEvidence_RemainsComplete()
    {
        var root = ParseValid();
        root["matching_metrics"]!["skills_normalized"] = new JsonArray("Java");

        var result = _sut.ValidateAndCanonicalize(root.ToJsonString());

        result.Quality.Should().Be(CvAnalysisQuality.COMPLETE);
    }

    [Fact]
    public void OneMalformedExperienceEntry_DiscardsOnlyThatEntryAndReturnsPartial()
    {
        var root = ParseValid();
        root["verbatim_sections"]!["professional_experience_and_projects"]!.AsArray()
            .Add(JsonValue.Create("malformed"));

        var result = _sut.ValidateAndCanonicalize(root.ToJsonString());

        result.Quality.Should().Be(CvAnalysisQuality.PARTIAL);
        result.IsUsable.Should().BeTrue();
        result.Coverage!.InputExperienceEntryCount.Should().Be(2);
        result.Coverage.AcceptedExperienceEntryCount.Should().Be(1);
        result.Diagnostics.Should().ContainSingle(x => x.Code == "EXPERIENCE_ENTRY_INVALID");
    }

    [Fact]
    public void WrongTypeForOneMetric_DefaultsOnlyThatMetricAndReturnsPartial()
    {
        var root = ParseValid();
        root["matching_metrics"]!["total_years_exp"] = "seven";

        var result = _sut.ValidateAndCanonicalize(root.ToJsonString());

        result.Quality.Should().Be(CvAnalysisQuality.PARTIAL);
        result.Coverage!.ExperienceMetricAvailable.Should().BeFalse();
        result.Coverage.SkillMetricsAvailable.Should().BeTrue();
        using var canonical = JsonDocument.Parse(result.CanonicalJson);
        canonical.RootElement.GetProperty("matching_metrics").GetProperty("total_years_exp").GetInt32().Should().Be(0);
    }

    [Fact]
    public void MissingMatchingEvidence_WithUsableMetrics_ReturnsPartial()
    {
        var root = ParseValid();
        root.Remove("matching_evidence");

        var result = _sut.ValidateAndCanonicalize(root.ToJsonString());

        result.Quality.Should().Be(CvAnalysisQuality.PARTIAL);
        result.IsUsable.Should().BeTrue();
        result.Diagnostics.Should().Contain(x => x.Code == "MATCHING_EVIDENCE_INVALID");
    }

    [Fact]
    public void SourceIndexOutOfRange_ReturnsPartialAndPreservesIndex()
    {
        var root = ParseValid();
        root["matching_evidence"]!["requirement_signals"]![0]!["source_index"] = 99;

        var result = _sut.ValidateAndCanonicalize(root.ToJsonString());

        result.Quality.Should().Be(CvAnalysisQuality.PARTIAL);
        using var canonical = JsonDocument.Parse(result.CanonicalJson);
        canonical.RootElement.GetProperty("matching_evidence").GetProperty("requirement_signals")[0]
            .GetProperty("source_index").GetInt32().Should().Be(99);
    }

    [Fact]
    public void UnknownEnumString_ReturnsPartialAndPreservesValue()
    {
        var root = ParseValid();
        root["matching_evidence"]!["requirement_signals"]![0]!["category"] = "new_provider_category";

        var result = _sut.ValidateAndCanonicalize(root.ToJsonString());

        result.Quality.Should().Be(CvAnalysisQuality.PARTIAL);
        result.Diagnostics.Should().Contain(x => x.Code == "SIGNAL_CATEGORY_UNKNOWN");
        JsonNode.Parse(result.CanonicalJson)!["matching_evidence"]!["requirement_signals"]![0]!["category"]!
            .GetValue<string>().Should().Be("new_provider_category");
    }

    [Theory]
    [InlineData("")]
    [InlineData("{\"schema_version\":")]
    [InlineData("[]")]
    [InlineData("{\"matching_metrics\":{}}")]
    [InlineData("{\"schema_version\":\"cv-analysis/v3\"}")]
    public void UnusableEnvelope_ReturnsInvalid(string output)
    {
        var result = _sut.ValidateAndCanonicalize(output);

        result.Quality.Should().Be(CvAnalysisQuality.INVALID);
        result.IsUsable.Should().BeFalse();
        result.CanonicalJson.Should().BeEmpty();
    }

    [Fact]
    public void ContentEmptyDocument_ReturnsInvalid()
    {
        var empty = """
            {"schema_version":"cv-analysis/v2","verbatim_sections":{},"matching_metrics":{},"matching_evidence":{}}
            """;

        var result = _sut.ValidateAndCanonicalize(empty);

        result.Quality.Should().Be(CvAnalysisQuality.INVALID);
        result.FailureCode.Should().Be("CV_ANALYSIS_CONTENT_EMPTY");
    }

    [Fact]
    public void RevalidatingCanonicalPartial_IsIdempotent()
    {
        var root = ParseValid();
        root["matching_metrics"]!["total_years_exp"] = "invalid";
        var first = _sut.ValidateAndCanonicalize(root.ToJsonString());

        var second = _sut.ValidateAndCanonicalize(first.CanonicalJson);

        second.Quality.Should().Be(CvAnalysisQuality.PARTIAL);
        second.CanonicalJson.Should().Be(first.CanonicalJson);
        second.Diagnostics.Should().Equal(first.Diagnostics);
    }

    private static JsonObject ParseValid() => JsonNode.Parse(CreateValidDocument())!.AsObject();

    internal static string CreateValidDocument() => JsonSerializer.Serialize(new
    {
        schema_version = "cv-analysis/v2",
        verbatim_sections = new
        {
            personal_info = new { name = "Jane Doe", title = "Backend Developer", summary = "Builds APIs" },
            education = Array.Empty<object>(),
            languages = Array.Empty<object>(),
            skills_section = new[] { "ReactJS", "C#" },
            professional_experience_and_projects = new[]
            {
                new
                {
                    company_or_project_name = "Acme",
                    role = "Backend Developer",
                    timeline = "Jan 2020 - Jan 2023",
                    entry_type = "professional_experience",
                    details_and_responsibilities = new[] { "Built APIs in C#." },
                    technologies_used = new[] { "C#" }
                }
            },
            certifications_and_awards = Array.Empty<string>(),
            other_information = ""
        },
        matching_metrics = new
        {
            job_titles_normalized = new[] { "Backend Developer" },
            skills_normalized = new[] { "ReactJS", "C#" },
            total_years_exp = 7,
            domains = Array.Empty<string>()
        },
        matching_evidence = new
        {
            requirement_signals = new[]
            {
                new
                {
                    name = "C#",
                    category = "tech_skill",
                    evidence_strength = "applied",
                    source_type = "professional_experience",
                    source_index = 0,
                    evidence = new[] { "Built APIs in C#." }
                }
            },
            experience_summary = new
            {
                total_professional_months = 11,
                calculation_basis = "explicit_timeline",
                periods = new[]
                {
                    new
                    {
                        source_index = 0,
                        entry_type = "professional_experience",
                        organization = "Acme",
                        role = "Backend Developer",
                        timeline_raw = "Jan 2020 - Jan 2023",
                        start_year = 2020,
                        start_month = 1,
                        end_year = 2023,
                        end_month = 1,
                        is_current = false,
                        evidence = "Acme - Backend Developer - Jan 2020 - Jan 2023"
                    }
                }
            },
            seniority_signals = Array.Empty<object>()
        }
    });
}
