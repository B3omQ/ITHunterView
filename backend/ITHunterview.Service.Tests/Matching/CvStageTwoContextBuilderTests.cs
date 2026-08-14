using System.Text.Json;
using ITHunterview.Service.Service.Matching;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.Tests.Matching;

public class CvStageTwoContextBuilderTests
{
    [Fact]
    public void Build_LargeCanonicalCv_DoesNotClipProviderContent()
    {
        var longValue = new string('x', 10_000);
        var source = $$"""
            {
              "schema_version":"cv-analysis/v2",
              "verbatim_sections":{
                "personal_info":{"name":"Candidate","title":"Engineer","summary":"{{longValue}}"},
                "education":[],"languages":[],"skills_section":["C#"],
                "professional_experience_and_projects":[],"certifications_and_awards":[],"other_information":""
              },
              "matching_metrics":{"job_titles_normalized":["Engineer"],"skills_normalized":["C#"],"total_years_exp":3,"domains":["fintech"]},
              "matching_evidence":{"requirement_signals":[],"experience_summary":{"total_professional_months":36,"calculation_basis":"dates","periods":[]},"seniority_signals":[]}
            }
            """;

        var result = new CvStageTwoContextBuilder().Build(source);
        using var output = JsonDocument.Parse(result.Json);

        Assert.Equal("matching-context/v1", output.RootElement.GetProperty("schema_version").GetString());
        Assert.Contains(longValue, result.Json, StringComparison.Ordinal);
        Assert.Equal("Engineer", output.RootElement.GetProperty("candidate")
            .GetProperty("personal_info").GetProperty("title").GetString());
        Assert.Equal("C#", output.RootElement.GetProperty("matching_metrics").GetProperty("skills_normalized")[0].GetString());
        Assert.Equal(CvAnalysisQuality.COMPLETE, result.Quality);
        Assert.Equal("COMPLETE", output.RootElement.GetProperty("cv_analysis").GetProperty("quality").GetString());
    }

    [Fact]
    public void Build_InvalidCvJson_FailsClosed()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new CvStageTwoContextBuilder().Build("{not-json"));

        Assert.Equal(CvStageTwoContextBuilder.InvalidCvMatchingContext, exception.Message);
    }

    [Fact]
    public void Build_CanonicalPartial_PreservesResultAndWarningMetadata()
    {
        var validator = new CvAnalysisResponseValidator();
        var malformedMetric = CvAnalysisResponseValidatorTests.CreateValidDocument()
            .Replace("\"total_years_exp\":7", "\"total_years_exp\":\"unknown\"");
        var canonical = validator.ValidateAndCanonicalize(malformedMetric);

        var result = new CvStageTwoContextBuilder().Build(canonical.CanonicalJson);

        Assert.Equal(CvAnalysisQuality.PARTIAL, result.Quality);
        Assert.Contains(result.Diagnostics, value => value.Code == "INTEGER_INVALID");
        using var output = JsonDocument.Parse(result.Json);
        Assert.False(output.RootElement.GetProperty("cv_analysis").GetProperty("coverage")
            .GetProperty("experience_metric_available").GetBoolean());
        Assert.Contains("INTEGER_INVALID", output.RootElement.GetProperty("cv_analysis").GetProperty("warning_codes")
            .EnumerateArray().Select(value => value.GetString()));
    }

    [Fact]
    public void Build_CurrentShapeCv_PreservesAllExperiencesCertificationAndOtherInformation()
    {
        var canonical = new CvAnalysisResponseValidator()
            .ValidateAndCanonicalize(CvAnalysisResponseValidatorTests.CreateCurrentShapeDocument().ToJsonString());

        var result = new CvStageTwoContextBuilder().Build(canonical.CanonicalJson);

        Assert.Equal(CvAnalysisQuality.COMPLETE, result.Quality);
        using var output = JsonDocument.Parse(result.Json);
        var candidate = output.RootElement.GetProperty("candidate");
        Assert.Equal(18, candidate.GetProperty("professional_experience_and_projects").GetArrayLength());
        Assert.Equal("Cloud Certificate", candidate.GetProperty("certifications_and_awards")[0].GetString());
        Assert.Equal("Available for remote work.", candidate.GetProperty("other_information").GetString());
        Assert.Equal(17, candidate.GetProperty("skills_section").GetArrayLength());
        Assert.Equal(20, output.RootElement.GetProperty("matching_metrics")
            .GetProperty("skills_normalized").GetArrayLength());
        Assert.Equal(8, output.RootElement.GetProperty("matching_evidence")
            .GetProperty("requirement_signals").GetArrayLength());
        Assert.Equal(3, output.RootElement.GetProperty("matching_evidence")
            .GetProperty("experience_summary").GetProperty("periods").GetArrayLength());
    }

    [Fact]
    public void Build_DoesNotClipLongVerbatimStringsOrDeduplicateArrays()
    {
        var root = CvAnalysisResponseValidatorTests.CreateCurrentShapeDocument();
        var longEvidence = new string('e', 2_000);
        root["matching_evidence"]!["requirement_signals"]![0]!["evidence"] =
            new System.Text.Json.Nodes.JsonArray(longEvidence, longEvidence);
        root["verbatim_sections"]!["skills_section"] =
            new System.Text.Json.Nodes.JsonArray("C#", "C#");
        var canonical = new CvAnalysisResponseValidator().ValidateAndCanonicalize(root.ToJsonString());

        var result = new CvStageTwoContextBuilder().Build(canonical.CanonicalJson);

        using var output = JsonDocument.Parse(result.Json);
        Assert.Equal(2, output.RootElement.GetProperty("candidate").GetProperty("skills_section").GetArrayLength());
        var evidence = output.RootElement.GetProperty("matching_evidence")
            .GetProperty("requirement_signals")[0].GetProperty("evidence");
        Assert.Equal(2, evidence.GetArrayLength());
        Assert.Equal(longEvidence, evidence[0].GetString());
    }

    [Fact]
    public void Build_RemovesOnlyCandidateNameFromVerbatimSections()
    {
        var root = CvAnalysisResponseValidatorTests.CreateCurrentShapeDocument();
        root["verbatim_sections"]!["personal_info"]!["provider_extension"] = "kept";
        root["verbatim_sections"]!["provider_section"] = new System.Text.Json.Nodes.JsonObject
        {
            ["provider_value"] = 42
        };
        var canonical = new CvAnalysisResponseValidator().ValidateAndCanonicalize(root.ToJsonString());

        var result = new CvStageTwoContextBuilder().Build(canonical.CanonicalJson);

        using var output = JsonDocument.Parse(result.Json);
        var candidate = output.RootElement.GetProperty("candidate");
        var personal = candidate.GetProperty("personal_info");
        Assert.False(personal.TryGetProperty("name", out _));
        Assert.Equal("kept", personal.GetProperty("provider_extension").GetString());
        Assert.Equal(42, candidate.GetProperty("provider_section").GetProperty("provider_value").GetInt32());
    }
}
