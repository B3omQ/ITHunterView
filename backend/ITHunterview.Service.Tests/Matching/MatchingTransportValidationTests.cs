using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Service.Matching;

namespace ITHunterview.Service.Tests.Matching;

public sealed class MatchingTransportValidationTests
{
    [Fact]
    public async Task BoundedReader_RejectsContentLengthAboveLimit()
    {
        using var content = new StringContent("small", Encoding.UTF8, "application/json");
        content.Headers.ContentLength = 10;

        var action = () => BoundedHttpContentReader.ReadAsStringAsync(content, 5);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("AI_RESPONSE_TOO_LARGE");
    }

    [Fact]
    public async Task BoundedReader_RejectsChunkedBodyWhenItExceedsLimit()
    {
        using var content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("123456789")));

        var action = () => BoundedHttpContentReader.ReadAsStringAsync(content, 5);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("AI_RESPONSE_TOO_LARGE");
    }

    [Fact]
    public async Task BoundedReader_ReturnsValidUtf8WithinLimit()
    {
        using var content = new StringContent("{\"ok\":true}", Encoding.UTF8, "application/json");

        var result = await BoundedHttpContentReader.ReadAsStringAsync(content, 100);

        result.Should().Be("{\"ok\":true}");
    }

    [Fact]
    public void LegacyValidator_RejectsMissingDuplicateAndOutOfRangeScores()
    {
        var requirements = new[] { "r1", "r2" };
        var missing = Parse("{\"scores\":[{\"reqId\":\"r1\",\"handlerCode\":\"H_TECH_05\",\"handlerScore\":1}]}");
        var duplicate = Parse("{\"scores\":[{\"reqId\":\"r1\",\"handlerCode\":\"H_TECH_05\",\"handlerScore\":1},{\"reqId\":\"r1\",\"handlerCode\":\"H_TECH_01\",\"handlerScore\":0},{\"reqId\":\"r2\",\"handlerCode\":\"H_LANG_06\",\"handlerScore\":1}]}");
        var outOfRange = Parse("{\"scores\":[{\"reqId\":\"r1\",\"handlerCode\":\"H_TECH_05\",\"handlerScore\":2},{\"reqId\":\"r2\",\"handlerCode\":\"H_LANG_06\",\"handlerScore\":1}]}");

        Action missingAction = () => LegacyJdStageTwoResponseValidator.Validate(missing, requirements);
        Action duplicateAction = () => LegacyJdStageTwoResponseValidator.Validate(duplicate, requirements);
        Action rangeAction = () => LegacyJdStageTwoResponseValidator.Validate(outOfRange, requirements);

        missingAction.Should().Throw<InvalidOperationException>().WithMessage("INVALID_STAGE_TWO_RESPONSE");
        duplicateAction.Should().Throw<InvalidOperationException>().WithMessage("INVALID_STAGE_TWO_RESPONSE");
        rangeAction.Should().Throw<InvalidOperationException>().WithMessage("INVALID_STAGE_TWO_RESPONSE");
    }

    [Fact]
    public void LegacyValidator_AcceptsExactlyOneBoundedScorePerRequirement()
    {
        var requirements = new[] { "r1", "r2" };
        using var response = Parse("{\"scores\":[{\"reqId\":\"r1\",\"handlerCode\":\"H_TECH_04\",\"handlerScore\":0.5},{\"reqId\":\"r2\",\"handlerCode\":\"H_LANG_01\",\"handlerScore\":0,\"flag\":\"CRITICAL_GAP\"}]}");

        var action = () => LegacyJdStageTwoResponseValidator.Validate(response, requirements);

        action.Should().NotThrow();
    }

    [Fact]
    public void LegacyValidator_RejectsUnknownHandlerCode()
    {
        var requirements = new[] { "r1" };
        using var response = Parse("{\"scores\":[{\"reqId\":\"r1\",\"handlerCode\":\"INJECTED\",\"handlerScore\":1}]}");

        var action = () => LegacyJdStageTwoResponseValidator.Validate(response, requirements);

        action.Should().Throw<InvalidOperationException>().WithMessage("INVALID_STAGE_TWO_RESPONSE");
    }

    private static JsonDocument Parse(string json) => JsonDocument.Parse(json);
}
