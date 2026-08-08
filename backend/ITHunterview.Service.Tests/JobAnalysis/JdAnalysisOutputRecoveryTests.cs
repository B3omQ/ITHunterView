using System.Text.Json;
using FluentAssertions;
using ITHunterview.Service.Utils;

namespace ITHunterview.Service.Tests.JobAnalysis;

public sealed class JdAnalysisOutputRecoveryTests
{
    [Fact]
    public void Recover_TruncatedV4Payload_KeepsOnlyCompleteRequirementGroups()
    {
        const string truncated = """
        {"schema_version":"jd-analysis/v4","matching_metrics":{"job_titles_normalized":["Backend Engineer"],"total_years_exp":3,"domains":["e-commerce"],"requirement_groups":[{"operator":"all_of","importance":"must_have","source_section":"requirements","requirement_verbatim":"Use C#.","items":[{"category":"tech_skill","skill_name":"c#","raw_mention":"C#"}]},{"operator":"all_of","importance":"must_have","source_section":"requirements","requirement_verbatim":"Use PostgreSQL.","items":[{"category":"tech_skill","skill_name":"postgresql","raw_mention":"PostgreSQL"}
        """;

        var result = JdAnalysisOutputRecovery.Recover(truncated);

        result.WasTruncated.Should().BeTrue();
        result.Json.Should().NotBeNullOrWhiteSpace();
        result.AcceptedGroupCount.Should().Be(1);
        result.InputGroupCount.Should().Be(2);
        result.DiscardedGroupCount.Should().Be(1);
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Code == "OUTPUT_TRUNCATED");
        using var document = JsonDocument.Parse(result.Json!);
        document.RootElement.GetProperty("matching_metrics")
            .GetProperty("requirement_groups").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public void Recover_WhenNoCompleteGroupExists_ReturnsNoPersistableJson()
    {
        var result = JdAnalysisOutputRecovery.Recover(
            "{\"schema_version\":\"jd-analysis/v4\",\"matching_metrics\":{\"requirement_groups\":[{\"operator\":\"all_of\"");

        result.WasTruncated.Should().BeTrue();
        result.Json.Should().BeNull();
        result.AcceptedGroupCount.Should().Be(0);
    }

    [Fact]
    public void Recover_ValidPayload_DoesNotRewriteIt()
    {
        const string valid = "{\"schema_version\":\"jd-analysis/v4\",\"matching_metrics\":{\"job_titles_normalized\":[],\"total_years_exp\":0,\"domains\":[],\"requirement_groups\":[]}}";

        var result = JdAnalysisOutputRecovery.Recover(valid);

        result.WasTruncated.Should().BeFalse();
        result.Json.Should().Be(valid);
        result.Diagnostics.Should().BeEmpty();
    }
}
