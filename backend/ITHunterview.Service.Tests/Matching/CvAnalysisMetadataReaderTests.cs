using ITHunterview.Domain.Enums;
using ITHunterview.Service.Utils;
using FluentAssertions;

namespace ITHunterview.Service.Tests.Matching;

public sealed class CvAnalysisMetadataReaderTests
{
    [Fact]
    public void ReadMetadata_PartialCanonicalJson_ReturnsBoundedValues()
    {
        var json = """
            {
              "analysis_quality":"PARTIAL",
              "analysis_coverage":{
                "input_experience_entry_count":2,
                "accepted_experience_entry_count":1,
                "discarded_experience_entry_count":1,
                "input_requirement_signal_count":1,
                "accepted_requirement_signal_count":1,
                "discarded_requirement_signal_count":0,
                "input_experience_period_count":0,
                "accepted_experience_period_count":0,
                "discarded_experience_period_count":0,
                "title_metrics_available":true,
                "skill_metrics_available":true,
                "experience_metric_available":false,
                "domain_metrics_available":true
              },
              "analysis_diagnostics":[
                {"code":"INTEGER_INVALID","json_path":"$.matching_metrics.total_years_exp"}
              ]
            }
            """;

        CvAnalysisMetadataReader.ReadQuality(json).Should().Be(CvAnalysisQuality.PARTIAL);
        CvAnalysisMetadataReader.ReadCoverage(json)!.DiscardedExperienceEntryCount.Should().Be(1);
        CvAnalysisMetadataReader.ReadDiagnostics(json).Should().ContainSingle(x => x.Code == "INTEGER_INVALID");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-json")]
    [InlineData("{\"analysis_quality\":\"UNKNOWN\"}")]
    public void ReadMetadata_HistoricalOrMalformed_ReturnsNullOrEmpty(string? json)
    {
        CvAnalysisMetadataReader.ReadQuality(json).Should().BeNull();
        CvAnalysisMetadataReader.ReadCoverage(json).Should().BeNull();
        CvAnalysisMetadataReader.ReadDiagnostics(json).Should().BeEmpty();
    }

    [Fact]
    public void ReadDiagnostics_CapsAtOneHundredAndDropsUnsafeEntries()
    {
        var entries = string.Join(",", Enumerable.Range(0, 150)
            .Select(index => $"{{\"code\":\"CODE_{index}\",\"json_path\":\"$.path[{index}]\"}}"));
        var json = $"{{\"analysis_diagnostics\":[{entries},{{\"code\":\"{new string('x', 101)}\",\"json_path\":\"$\"}}]}}";

        var diagnostics = CvAnalysisMetadataReader.ReadDiagnostics(json);

        diagnostics.Should().HaveCount(100);
        diagnostics.Should().OnlyContain(value => value.Code.Length <= 100 && value.JsonPath.Length <= 300);
    }
}
