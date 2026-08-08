using System.Text.Json;
using FluentAssertions;
using ITHunterview.Service.Constant.Prompts;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Service.Matching;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ITHunterview.Service.Tests.Matching;

public sealed class JdStageTwoMatchingServiceTests
{
    [Fact]
    public async Task Execute_UsesOnePathForBlankOrArbitraryModelConfig()
    {
        foreach (var modelConfig in new[] { "", "{}", "{\"contract\":\"jd-matching/v3\"}", "{\"contract\":\"future\"}" })
        {
            var ai = new Mock<IAiService>(MockBehavior.Strict);
            ai.Setup(x => x.GetActiveProviderNameAsync()).ReturnsAsync("Gemini");
            ai.Setup(x => x.GenerateTextAsync(
                    It.IsAny<string>(),
                    null,
                    "Gemini",
                    It.IsAny<AiGenerationOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidResponse());
            var service = CreateService(ai);

            var result = await service.ExecuteAsync(Prompt(modelConfig), "{\"cv\":true}", Projection());

            result.FinalScore.Should().BeGreaterThan(0m);
            ai.Verify(x => x.GenerateTextAsync(
                It.IsAny<string>(), null, "Gemini", It.IsAny<AiGenerationOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public async Task Execute_ComposesLockedSchemaAndSubstitutesEachContextExactlyOnce()
    {
        var ai = new Mock<IAiService>(MockBehavior.Strict);
        ai.Setup(x => x.GetActiveProviderNameAsync()).ReturnsAsync("Gemini");
        string? capturedPrompt = null;
        ai.Setup(x => x.GenerateTextAsync(
                It.IsAny<string>(), null, "Gemini", It.IsAny<AiGenerationOptions>(), It.IsAny<CancellationToken>()))
            .Callback<string, string?, string?, AiGenerationOptions, CancellationToken>((prompt, _, _, _, _) => capturedPrompt = prompt)
            .ReturnsAsync(ValidResponse());

        await CreateService(ai).ExecuteAsync(Prompt("{}"), "{\"cvMarker\":true}", Projection());

        capturedPrompt.Should().NotBeNull();
        Count(capturedPrompt!, JdMatchingOutputSchema.BeginMarker).Should().Be(1);
        Count(capturedPrompt!, JdMatchingOutputSchema.EndMarker).Should().Be(1);
        Count(capturedPrompt!, "{\"cvMarker\":true}").Should().Be(1);
        Count(capturedPrompt!, "\"ReqId\": \"g1:i1\"").Should().Be(1);
        capturedPrompt.Should().NotContain("[CV_TEXT]");
        capturedPrompt.Should().NotContain("[PARSED_JD_REQUIREMENTS]");
    }

    [Fact]
    public async Task Execute_InvalidFirstResponseUsesExactlyOneControlledRetry()
    {
        var ai = new Mock<IAiService>(MockBehavior.Strict);
        ai.Setup(x => x.GetActiveProviderNameAsync()).ReturnsAsync("Gemini");
        ai.SetupSequence(x => x.GenerateTextAsync(
                It.IsAny<string>(), null, "Gemini", It.IsAny<AiGenerationOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{not-json")
            .ReturnsAsync(ValidResponse());

        var result = await CreateService(ai).ExecuteAsync(Prompt("{}"), "{\"cv\":true}", Projection());

        result.FinalScore.Should().BeGreaterThan(0m);
        ai.Verify(x => x.GenerateTextAsync(
            It.IsAny<string>(), null, "Gemini", It.Is<AiGenerationOptions>(o => o.ProfileId == "jd-matching-json/v1"), It.IsAny<CancellationToken>()), Times.Once);
        ai.Verify(x => x.GenerateTextAsync(
            It.IsAny<string>(), null, "Gemini", It.Is<AiGenerationOptions>(o => o.ProfileId == "jd-matching-json-retry/v1"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_TwoInvalidResponsesReturnBoundedFailureWithoutRawOutput()
    {
        const string sensitiveModelOutput = "{\"scores\": [\"PRIVATE-CV-DATA\"]";
        var ai = new Mock<IAiService>(MockBehavior.Strict);
        ai.Setup(x => x.GetActiveProviderNameAsync()).ReturnsAsync("Gemini");
        ai.SetupSequence(x => x.GenerateTextAsync(
                It.IsAny<string>(), null, "Gemini", It.IsAny<AiGenerationOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sensitiveModelOutput)
            .ReturnsAsync(sensitiveModelOutput);

        var action = () => CreateService(ai).ExecuteAsync(Prompt("{}"), "{\"cv\":true}", Projection());

        var exception = await action.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Message.Should().Be("MATCHING_STAGE2_OUTPUT_INVALID");
        exception.Which.ToString().Should().NotContain("PRIVATE-CV-DATA");
        ai.Verify(x => x.GenerateTextAsync(
            It.IsAny<string>(), null, "Gemini", It.IsAny<AiGenerationOptions>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Execute_CancellationAfterFirstFailurePreventsSecondProviderCall()
    {
        using var cancellation = new CancellationTokenSource();
        var ai = new Mock<IAiService>(MockBehavior.Strict);
        ai.Setup(x => x.GetActiveProviderNameAsync()).ReturnsAsync("Gemini");
        ai.Setup(x => x.GenerateTextAsync(
                It.IsAny<string>(), null, "Gemini", It.IsAny<AiGenerationOptions>(), It.IsAny<CancellationToken>()))
            .Callback(() => cancellation.Cancel())
            .ReturnsAsync("{not-json");

        var action = () => CreateService(ai).ExecuteAsync(Prompt("{}"), "{\"cv\":true}", Projection(), cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
        ai.Verify(x => x.GenerateTextAsync(
            It.IsAny<string>(), null, "Gemini", It.IsAny<AiGenerationOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static JdStageTwoMatchingService CreateService(Mock<IAiService> ai) =>
        new(ai.Object, NullLogger<JdStageTwoMatchingService>.Instance);

    private static PromptSnapshotDto Prompt(string modelConfig) => new()
    {
        VersionId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
        VersionTag = "v2.0",
        Content = "Semantic instructions.\n[CV_TEXT]\n[PARSED_JD_REQUIREMENTS]",
        ModelConfig = modelConfig
    };

    private static JdRequirementProjection Projection() => new(
        "jd-analysis/v4",
        new[]
        {
            new ProjectedJdRequirementGroup(
                "g1",
                "all_of",
                1,
                "must_have",
                new[]
                {
                    new ProjectedJdRequirementItem(
                        "g1:i1", "tech_skill", "React", "React", "React", "requirements",
                        Array.Empty<string>(), null, null, 1m)
                })
        },
        false);

    private static string ValidResponse() =>
        "{\"scores\":[{\"reqId\":\"g1:i1\",\"handlerCode\":\"H_TECH_05\",\"handlerScore\":1,\"reasoning\":\"Production React evidence\",\"confidence\":\"high\",\"flag\":null}],\"criticalGaps\":[],\"penalties\":[],\"narrative\":\"Good fit\",\"improvements\":[]}";

    private static int Count(string value, string token) =>
        value.Split(token, StringSplitOptions.None).Length - 1;
}
