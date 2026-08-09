using System.Text.Json;
using FluentAssertions;
using ITHunterview.Service.Utils;

namespace ITHunterview.Service.Tests.JobAnalysis;

public sealed class JdAnalysisOutputRecoveryTests
{
    private const string FirstGroup = """
        {"source_requirement_id":"req-001","intent":"qualification","operator":"one_of","importance":"must_have","source_section":"requirements","requirement_verbatim":"Use Java or Kotlin.","items":[{"category":"tech_skill","skill_name":"Java","raw_mention":"Java"},{"category":"tech_skill","skill_name":"Kotlin","raw_mention":"Kotlin"}]}
        """;

    [Fact]
    public void Recover_TruncatedFixture_PreservesOnlyTheCompleteFirstGroup()
    {
        var result = JdAnalysisOutputRecovery.Recover(
            ReadFixture("jd-analysis-v5-truncated.json.txt"));

        result.WasTruncated.Should().BeTrue();
        result.AcceptedGroupCount.Should().Be(1);
        result.InputGroupCount.Should().Be(2);
        result.DiscardedGroupCount.Should().Be(1);
        using var document = JsonDocument.Parse(result.Json!);
        var group = document.RootElement.GetProperty("matching_metrics").GetProperty("requirement_groups")[0];
        group.GetProperty("source_requirement_id").GetString().Should().Be("req-001");
        group.GetProperty("requirement_verbatim").GetString().Should().Be("Thành thạo Java.");
    }

    [Fact]
    public void Recover_ValidV5Payload_ReturnsItByteForByteAsComplete()
    {
        const string valid = "{\"schema_version\":\"jd-analysis/v5\",\"matching_metrics\":{\"job_titles_normalized\":[],\"total_years_exp\":0,\"domains\":[],\"requirement_groups\":[]}}";

        var result = JdAnalysisOutputRecovery.Recover(valid);

        result.WasTruncated.Should().BeFalse();
        result.Json.Should().Be(valid);
        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Recover_TruncatedInsideSecondItem_KeepsOnlyFullyClosedGroupObjects()
    {
        var truncated = V5Prefix + FirstGroup + """
            ,{"source_requirement_id":"req-002","intent":"qualification","operator":"all_of","importance":"must_have","source_section":"requirements","requirement_verbatim":"Use PostgreSQL.","items":[{"category":"tech_skill","skill_name":"PostgreSQL","raw_mention":"Postgre
            """;

        var result = JdAnalysisOutputRecovery.Recover(truncated);

        result.WasTruncated.Should().BeTrue();
        result.AcceptedGroupCount.Should().Be(1);
        result.InputGroupCount.Should().Be(2);
        result.DiscardedGroupCount.Should().Be(1);
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Code == "OUTPUT_TRUNCATED");
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Code == "RECOVERED_COMPLETE_GROUPS");
        using var document = JsonDocument.Parse(result.Json!);
        var recovered = document.RootElement.GetProperty("matching_metrics").GetProperty("requirement_groups");
        document.RootElement.GetProperty("schema_version").GetString().Should().Be("jd-analysis/v5");
        recovered.GetArrayLength().Should().Be(1);
        var group = recovered[0];
        group.GetProperty("source_requirement_id").GetString().Should().Be("req-001");
        group.GetProperty("intent").GetString().Should().Be("qualification");
        group.GetProperty("operator").GetString().Should().Be("one_of");
        group.GetProperty("requirement_verbatim").GetString().Should().Be("Use Java or Kotlin.");
        group.GetProperty("items")[0].GetProperty("skill_name").GetString().Should().Be("Java");
        group.GetProperty("items")[1].GetProperty("skill_name").GetString().Should().Be("Kotlin");
    }

    [Fact]
    public void Recover_TruncatedAfterCompleteGroup_PreservesCompletedGroup()
    {
        var truncated = V5Prefix + FirstGroup + ",";

        var result = JdAnalysisOutputRecovery.Recover(truncated);

        result.Json.Should().NotBeNullOrWhiteSpace();
        result.AcceptedGroupCount.Should().Be(1);
        result.InputGroupCount.Should().Be(1);
    }

    [Fact]
    public void Recover_TruncatedBeforeAnyCompleteGroup_ReturnsNoPersistableJson()
    {
        var truncated = V5Prefix + "{\"source_requirement_id\":\"req-001\",\"items\":[{\"category\":\"tech_skill\"";

        var result = JdAnalysisOutputRecovery.Recover(truncated);

        result.WasTruncated.Should().BeTrue();
        result.Json.Should().BeNull();
        result.AcceptedGroupCount.Should().Be(0);
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Code == "NO_COMPLETE_GROUP_RECOVERED");
    }

    [Fact]
    public void Recover_TruncatedPayload_EnforcesFiftyGroupCap()
    {
        var groups = string.Join(',', Enumerable.Range(1, 51).Select(index =>
            FirstGroup.Replace("req-001", $"req-{index:000}", StringComparison.Ordinal)));
        var truncated = V5Prefix + groups + ",";

        var result = JdAnalysisOutputRecovery.Recover(truncated);

        result.AcceptedGroupCount.Should().Be(50);
        result.InputGroupCount.Should().Be(51);
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Code == "REQUIREMENT_GROUP_LIMIT_EXCEEDED");
    }

    [Fact]
    public void Recover_OversizedPayload_IsInvalidWithoutScanningForGroups()
    {
        var oversized = new string('x', 262_145);

        var result = JdAnalysisOutputRecovery.Recover(oversized);

        result.Json.Should().BeNull();
        result.Diagnostics.Should().ContainSingle(diagnostic => diagnostic.Code == "PAYLOAD_TOO_LARGE");
    }

    private const string V5Prefix = """
        {"schema_version":"jd-analysis/v5","matching_metrics":{"job_titles_normalized":["Backend Engineer"],"total_years_exp":3,"domains":["FinTech"],"requirement_groups":[
        """;

    private static string ReadFixture(string name) => File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "JobAnalysis", "Fixtures", name));
}
