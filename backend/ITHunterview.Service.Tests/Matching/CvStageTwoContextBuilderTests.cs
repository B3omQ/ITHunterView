using System.Text.Json;
using ITHunterview.Service.Service.Matching;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.Tests.Matching;

public class CvStageTwoContextBuilderTests
{
    [Fact]
    public void Build_LargeCanonicalCv_ReturnsBoundedValidJsonWithoutSubstringing()
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
        Assert.True(result.Json.Length < source.Length);
        Assert.Equal("Engineer", output.RootElement.GetProperty("candidate").GetProperty("title").GetString());
        Assert.Equal("C#", output.RootElement.GetProperty("matching_metrics").GetProperty("skills_normalized")[0].GetString());
        Assert.Equal(CvAnalysisQuality.PARTIAL, result.Quality);
        Assert.Equal("PARTIAL", output.RootElement.GetProperty("cv_analysis").GetProperty("quality").GetString());
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
        Assert.False(output.RootElement.GetProperty("cv_analysis").GetProperty("experience_metric_available").GetBoolean());
        Assert.Contains("INTEGER_INVALID", output.RootElement.GetProperty("cv_analysis").GetProperty("warning_codes")
            .EnumerateArray().Select(value => value.GetString()));
    }
}
