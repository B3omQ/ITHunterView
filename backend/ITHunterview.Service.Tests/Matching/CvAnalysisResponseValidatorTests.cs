using System;
using System.Text.Json;
using FluentAssertions;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Service.Matching;
using Xunit;

namespace ITHunterview.Service.Tests.Matching;

public class CvAnalysisResponseValidatorTests
{
    private const string RawCvText = """
        Jane Doe
        Backend Developer
        Acme - Backend Developer - Jan 2020 - Jan 2023
        Built APIs in C#.
        Skills: C#
        """;

    private static readonly CvAnalysisInputSnapshot Input = new(
        RawCvText,
        "pasted_text",
        null,
        new DateOnly(2026, 8, 1));

    private readonly CvAnalysisResponseValidator _sut = new();

    [Fact]
    public void ValidateAndCanonicalize_ValidV2Document_RecalculatesExperienceAndKeepsMetricCompatibility()
    {
        var result = _sut.ValidateAndCanonicalize(CreateValidDocument(), Input);

        result.IsValid.Should().BeTrue(result.FailureCode);
        using var json = JsonDocument.Parse(result.CanonicalJson);
        var metrics = json.RootElement.GetProperty("matching_metrics");
        metrics.GetProperty("total_years_exp").GetInt32().Should().Be(3);
        json.RootElement
            .GetProperty("matching_evidence")
            .GetProperty("experience_summary")
            .GetProperty("total_professional_months")
            .GetInt32()
            .Should().Be(36);
        metrics.GetProperty("skills_normalized")[0].GetString().Should().Be("c#");
    }

    [Fact]
    public void ValidateAndCanonicalize_MetricArrayContainsObject_RejectsTheResponse()
    {
        var document = CreateValidDocument().Replace("\"skills_normalized\":[\"C#\"]", "\"skills_normalized\":[{\"name\":\"C#\"}]");

        var result = _sut.ValidateAndCanonicalize(document, Input);

        result.IsValid.Should().BeFalse();
        result.FailureCode.Should().Be("CV_ANALYSIS_SCHEMA_INVALID");
    }

    [Fact]
    public void ValidateAndCanonicalize_EvidenceNotFoundInRawCv_RejectsTheResponse()
    {
        var document = CreateValidDocument().Replace("Built APIs in C#.", "Invented production result");

        var result = _sut.ValidateAndCanonicalize(document, Input);

        result.IsValid.Should().BeFalse();
        result.FailureCode.Should().Be("CV_ANALYSIS_EVIDENCE_NOT_GROUNDED");
    }

    [Fact]
    public void ValidateAndCanonicalize_EvidenceWithCollapsedPdfLineBreaks_AcceptsTheResponse()
    {
        const string wrappedRawText = """
            Jane Doe
            Backend Developer
            Acme - Backend Developer - Jan 2020 - Jan 2023
            Built APIs in
            C#.
            Skills: C#
            """;
        var wrappedInput = Input with { RawText = wrappedRawText };

        var result = _sut.ValidateAndCanonicalize(CreateValidDocument(), wrappedInput);

        result.IsValid.Should().BeTrue(result.FailureCode);
    }

    [Fact]
    public void ValidateAndCanonicalize_InvalidEntryType_RejectsTheResponse()
    {
        var document = CreateValidDocument().Replace("\"entry_type\":\"professional_experience\"", "\"entry_type\":\"made_up_entry_type\"");

        var result = _sut.ValidateAndCanonicalize(document, Input);

        result.IsValid.Should().BeFalse();
        result.FailureCode.Should().Be("CV_ANALYSIS_SCHEMA_INVALID");
    }

    private static string CreateValidDocument()
    {
        return JsonSerializer.Serialize(new
        {
            schema_version = "cv-analysis/v2",
            verbatim_sections = new
            {
                personal_info = new { name = "Jane Doe", title = "Backend Developer", summary = "" },
                education = Array.Empty<object>(),
                languages = Array.Empty<object>(),
                skills_section = new[] { "C#" },
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
                skills_normalized = new[] { "C#" },
                total_years_exp = 0,
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
                    total_professional_months = 0,
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
}
