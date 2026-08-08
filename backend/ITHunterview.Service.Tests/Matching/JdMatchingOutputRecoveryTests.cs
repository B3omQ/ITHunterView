using ITHunterview.Service.Service.Matching;

namespace ITHunterview.Service.Tests.Matching;

public sealed class JdMatchingOutputRecoveryTests
{
    [Fact]
    public void Recover_TruncatedScoresArray_KeepsOnlyFullyClosedScoreObjects()
    {
        const string output = """
            ```json
            {
              "scores": [
                {"reqId":"g1:i1","handlerCode":"H_TECH_05","handlerScore":1},
                {"reqId":"g1:i2","handlerCode":"H_LANG_06","handlerScore":
            """;

        using var recovered = JdMatchingOutputRecovery.Recover(output);

        Assert.NotNull(recovered.Document);
        Assert.False(recovered.IsCompleteJson);
        Assert.True(recovered.WasTruncated);
        var scores = recovered.Document!.RootElement.GetProperty("scores");
        Assert.Equal(1, scores.GetArrayLength());
        Assert.Equal("g1:i1", scores[0].GetProperty("reqId").GetString());
        Assert.Contains("RECOVERED_COMPLETE_SCORE_OBJECTS", recovered.WarningCodes);
    }

    [Fact]
    public void Recover_TruncatedBeforeFirstCompleteScore_ReturnsInvalidRecovery()
    {
        const string output = """
            {"scores":[{"reqId":"g1:i1","handlerCode":"H_TECH_05"
            """;

        using var recovered = JdMatchingOutputRecovery.Recover(output);

        Assert.Null(recovered.Document);
        Assert.False(recovered.IsCompleteJson);
        Assert.True(recovered.WasTruncated);
        Assert.Contains("JSON_PARSE_FAILED", recovered.WarningCodes);
    }

    [Fact]
    public void Recover_CompleteObjectWithTrailingText_ExtractsObjectWithoutInventingData()
    {
        const string output = """
            {"scores":[{"reqId":"g1:i1","handlerCode":"H_TECH_05","handlerScore":0.7}]}
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
