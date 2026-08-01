using System.Text.Json;
using ITHunterview.Service.Service.Matching;

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
    }

    [Fact]
    public void Build_InvalidCvJson_FailsClosed()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new CvStageTwoContextBuilder().Build("{not-json"));

        Assert.Equal(CvStageTwoContextBuilder.InvalidCvMatchingContext, exception.Message);
    }
}
