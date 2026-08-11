using ITHunterview.Service.Service.Matching;

namespace ITHunterview.Service.Tests.Matching;

public sealed class JdMatchingOutputRecoveryTests
{
    [Fact]
    public void Recover_TruncatedNestedEvidence_KeepsOnlyFullyClosedAssessments()
    {
        const string output = """
            ```json
            {
              "schemaVersion":"jd-stage2/v2",
              "scores": [
                {"reqId":"g1:i1","handlerCode":"H_TECH_05","evidence":[{"quotation":"Built API {v2}","section":"Experience"}]},
                {"reqId":"g1:i2","handlerCode":"H_LANG_06","evidence":[{"quotation":"English
            """;

        using var recovered = JdMatchingOutputRecovery.Recover(output);

        Assert.NotNull(recovered.Document);
        Assert.False(recovered.IsCompleteJson);
        Assert.True(recovered.WasTruncated);
        Assert.Equal("jd-stage2/v2", recovered.Document!.RootElement.GetProperty("schemaVersion").GetString());
        var scores = recovered.Document.RootElement.GetProperty("scores");
        Assert.Single(scores.EnumerateArray());
        Assert.Equal("g1:i1", scores[0].GetProperty("reqId").GetString());
        Assert.Single(scores[0].GetProperty("evidence").EnumerateArray());
        Assert.Contains("RECOVERED_COMPLETE_SCORE_OBJECTS", recovered.WarningCodes);
    }

    [Fact]
    public void Recover_TruncatedWithoutObservedSupportedSchema_ReturnsInvalid()
    {
        const string output = """
            {"scores":[{"reqId":"g1:i1","handlerCode":"H_TECH_05"}
            """;

        using var recovered = JdMatchingOutputRecovery.Recover(output);

        Assert.Null(recovered.Document);
        Assert.Contains("SCHEMA_VERSION_MISSING_OR_UNSUPPORTED", recovered.WarningCodes);
    }

    [Fact]
    public void Recover_CompleteObjectWithTrailingText_ExtractsObjectWithoutInventingData()
    {
        const string output = """
            {"schemaVersion":"jd-stage2/v2","scores":[{"reqId":"g1:i1","handlerCode":"H_TECH_05"}]}
            This sentence is not JSON.
            """;

        using var recovered = JdMatchingOutputRecovery.Recover(output);

        Assert.NotNull(recovered.Document);
        Assert.False(recovered.IsCompleteJson);
        Assert.False(recovered.WasTruncated);
        Assert.Single(recovered.Document!.RootElement.GetProperty("scores").EnumerateArray());
        Assert.Contains("EXTRACTED_COMPLETE_JSON_OBJECT", recovered.WarningCodes);
    }
}
