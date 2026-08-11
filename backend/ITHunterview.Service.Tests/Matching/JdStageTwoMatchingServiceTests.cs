using System.Text.Json;
using FluentAssertions;
using ITHunterview.Service.Constant.Prompts;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Service.Matching;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ITHunterview.Service.Tests.Matching;

public sealed class JdStageTwoMatchingServiceTests
{
    [Fact]
    public async Task Execute_UsesOnePathForBlankOrArbitraryModelConfig()
    {
        foreach (var modelConfig in new[] { "", "{}", "{\"contract\":\"jd-matching/v3\"}", "{\"contract\":\"future\"}" })
        {
            var ai = CompleteAi(ValidResponse());

            var result = await CreateService(ai).ExecuteAsync(Prompt(modelConfig), "{\"cv\":true}", Projection());

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
        capturedPrompt.Should().Contain("\"schemaVersion\": \"jd-stage2/v2\"");
        capturedPrompt.Should().NotContain("[CV_TEXT]");
        capturedPrompt.Should().NotContain("[PARSED_JD_REQUIREMENTS]");
    }

    [Fact]
    public async Task Execute_InvalidFirstResponseUsesExactlyOneControlledRetry()
    {
        var ai = SequenceAi("{not-json", ValidResponse());

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
        var ai = SequenceAi(sensitiveModelOutput, sensitiveModelOutput);

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

    [Fact]
    public async Task Execute_PartialThenMissingOnlyResponse_BuildsSecondPromptForExactMissingIdAndCompletes()
    {
        var prompts = new List<string>();
        var responses = new Queue<string>(new[]
        {
            Response(("g1:i1", "H_TECH_05")),
            Response(("g1:i2", "H_LANG_F05"))
        });
        var ai = new Mock<IAiService>(MockBehavior.Strict);
        ai.Setup(x => x.GetActiveProviderNameAsync()).ReturnsAsync("Gemini");
        ai.Setup(x => x.GenerateTextAsync(
                It.IsAny<string>(), null, "Gemini", It.IsAny<AiGenerationOptions>(), It.IsAny<CancellationToken>()))
            .Callback<string, string?, string?, AiGenerationOptions, CancellationToken>((prompt, _, _, _, _) => prompts.Add(prompt))
            .ReturnsAsync(() => responses.Dequeue());

        var result = await CreateService(ai).ExecuteAsync(Prompt("{}"), "{\"cv\":true}", TwoItemProjection());
        using var json = JsonDocument.Parse(result.JsonString);

        prompts.Should().HaveCount(2);
        prompts[0].Should().Contain("\"ReqId\": \"g1:i1\"").And.Contain("\"ReqId\": \"g1:i2\"");
        prompts[1].Should().NotContain("\"ReqId\": \"g1:i1\"");
        prompts[1].Should().Contain("\"ReqId\": \"g1:i2\"");
        prompts[1].Should().Contain("RECOVERY ATTEMPT");
        json.RootElement.GetProperty("contract").GetString().Should().Be(JdFitResultContract.Version4);
        json.RootElement.GetProperty("analysis").GetProperty("acceptedCount").GetInt32().Should().Be(2);
        json.RootElement.GetProperty("analysis").GetProperty("providerAttemptCount").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task Execute_KnownScoringCodesAcrossFamilies_CompletesWithoutRetryAndPreservesCategories()
    {
        var ai = CompleteAi(Response(
            ("g1:i1", "H_EXP_D04"),
            ("g1:i2", "h_tech_05")));

        var result = await CreateService(ai).ExecuteAsync(Prompt("{}"), "{\"cv\":true}", TwoItemProjection());
        using var json = JsonDocument.Parse(result.JsonString);
        var items = json.RootElement
            .GetProperty("jdFit")
            .GetProperty("requirementGroups")[0]
            .GetProperty("items")
            .EnumerateArray()
            .ToDictionary(item => item.GetProperty("itemId").GetString()!, StringComparer.Ordinal);

        items["g1:i1"].GetProperty("category").GetString().Should().Be("tech_skill");
        items["g1:i1"].GetProperty("handlerCode").GetString().Should().Be("H_EXP_D04");
        items["g1:i1"].GetProperty("score").GetDecimal().Should().Be(0.75m);
        items["g1:i2"].GetProperty("category").GetString().Should().Be("language");
        items["g1:i2"].GetProperty("handlerCode").GetString().Should().Be("H_TECH_05");
        items["g1:i2"].GetProperty("score").GetDecimal().Should().Be(1m);
        json.RootElement.GetProperty("analysis").GetProperty("acceptedCount").GetInt32().Should().Be(2);
        json.RootElement.GetProperty("analysis").GetProperty("providerAttemptCount").GetInt32().Should().Be(1);
        ai.Verify(x => x.GenerateTextAsync(
            It.IsAny<string>(), null, "Gemini", It.IsAny<AiGenerationOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_IncompleteAttempt_LogsNeutralOutputEvaluationWithQuality()
    {
        var ai = SequenceAi(
            Response(("g1:i1", "H_TECH_05")),
            Response(("g1:i2", "H_LANG_F05")));
        var logger = new Mock<ILogger<JdStageTwoMatchingService>>();
        var service = new JdStageTwoMatchingService(ai.Object, logger.Object);

        await service.ExecuteAsync(Prompt("{}"), "{\"cv\":true}", TwoItemProjection());

        logger.Verify(x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) =>
                state.ToString()!.StartsWith("JD Stage 2 output evaluated.", StringComparison.Ordinal) &&
                state.ToString()!.Contains("Quality=PARTIAL", StringComparison.Ordinal)),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task Execute_TwentyOneKnownCodesIncludingCrossFamilyAndNormalizedCodes_CompletesInOneCall()
    {
        var projection = TwentyOneItemProjection();
        var codes = new[]
        {
            "H_EXP_D01", "H_TECH_02", " h_lang_f03 ", "H_EDU_04", "H_DOMAIN_05",
            "H_SOFT_01", "H_EXP_H02", "h_tech_03", "H_LANG_Q04", "H_EDU_05",
            "H_DOMAIN_01", "H_SOFT_02", "H_EXP_D03", "H_TECH_04", "H_LANG_F05",
            "H_EDU_01", "H_DOMAIN_02", "H_SOFT_03", "H_EXP_H04", "H_TECH_05", "H_LANG_Q01"
        };
        var response = Response(projection.Groups[0].Items
            .Select((item, index) => (item.ItemId, codes[index]))
            .ToArray());
        var ai = CompleteAi(response);

        var result = await CreateService(ai).ExecuteAsync(Prompt("{}"), "{\"cv\":true}", projection);
        using var json = JsonDocument.Parse(result.JsonString);

        json.RootElement.GetProperty("analysis").GetProperty("acceptedCount").GetInt32().Should().Be(21);
        json.RootElement.GetProperty("analysis").GetProperty("providerAttemptCount").GetInt32().Should().Be(1);
        json.RootElement.GetProperty("jdFit").GetProperty("requirementGroups")[0]
            .GetProperty("items").GetArrayLength().Should().Be(21);
        ai.Verify(x => x.GenerateTextAsync(
            It.IsAny<string>(), null, "Gemini", It.IsAny<AiGenerationOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_SecondAttemptStillMissing_ThrowsInsteadOfCalculatingPartial()
    {
        var ai = SequenceAi(
            Response(("g1:i1", "H_TECH_05")),
            Response(("g1:i1", "H_TECH_05")));

        var action = () => CreateService(ai).ExecuteAsync(Prompt("{}"), "{\"cv\":true}", TwoItemProjection());

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("MATCHING_STAGE2_OUTPUT_INVALID");
    }

    [Fact]
    public async Task Execute_FatalSecondCallExceptionAfterPartial_IsNotSwallowed()
    {
        var ai = new Mock<IAiService>(MockBehavior.Strict);
        ai.Setup(x => x.GetActiveProviderNameAsync()).ReturnsAsync("Gemini");
        ai.SetupSequence(x => x.GenerateTextAsync(
                It.IsAny<string>(), null, "Gemini", It.IsAny<AiGenerationOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response(("g1:i1", "H_TECH_05")))
            .ThrowsAsync(new ApplicationException("configuration failure"));

        var action = () => CreateService(ai).ExecuteAsync(Prompt("{}"), "{\"cv\":true}", TwoItemProjection());

        await action.Should().ThrowAsync<ApplicationException>()
            .WithMessage("configuration failure");
    }

    private static JdStageTwoMatchingService CreateService(Mock<IAiService> ai) =>
        new(ai.Object, NullLogger<JdStageTwoMatchingService>.Instance);

    private static Mock<IAiService> CompleteAi(string response)
    {
        var ai = new Mock<IAiService>(MockBehavior.Strict);
        ai.Setup(x => x.GetActiveProviderNameAsync()).ReturnsAsync("Gemini");
        ai.Setup(x => x.GenerateTextAsync(
                It.IsAny<string>(), null, "Gemini", It.IsAny<AiGenerationOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        return ai;
    }

    private static Mock<IAiService> SequenceAi(string first, string second)
    {
        var ai = new Mock<IAiService>(MockBehavior.Strict);
        ai.Setup(x => x.GetActiveProviderNameAsync()).ReturnsAsync("Gemini");
        ai.SetupSequence(x => x.GenerateTextAsync(
                It.IsAny<string>(), null, "Gemini", It.IsAny<AiGenerationOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(first)
            .ReturnsAsync(second);
        return ai;
    }

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
                "g1", "all_of", 1, "must_have",
                new[] { Item("g1:i1", "tech_skill", "React") })
        },
        false);

    private static JdRequirementProjection TwoItemProjection() => new(
        "jd-analysis/v4",
        new[]
        {
            new ProjectedJdRequirementGroup(
                "g1", "all_of", 2, "must_have",
                new[]
                {
                    Item("g1:i1", "tech_skill", "React"),
                    Item("g1:i2", "language", "English")
                })
        },
        false);

    private static JdRequirementProjection TwentyOneItemProjection()
    {
        var categories = new[]
        {
            "tech_skill", "experience", "domain_knowledge", "language", "education", "soft_skill"
        };
        var items = Enumerable.Range(1, 21)
            .Select(index => Item($"g21:i{index:00}", categories[(index - 1) % categories.Length], $"Requirement {index}"))
            .ToArray();
        return new JdRequirementProjection(
            "jd-analysis/v4",
            new[] { new ProjectedJdRequirementGroup("g21", "all_of", 21, "must_have", items) },
            false);
    }

    private static ProjectedJdRequirementItem Item(string id, string category, string name) => new(
        id, category, name, name, name, "requirements", Array.Empty<string>(), null, null,
        JdRequirementCategoryWeights.Get(category));

    private static string ValidResponse() => Response(("g1:i1", "H_TECH_05"));

    private static string Response(params (string ItemId, string HandlerCode)[] items) =>
        JsonSerializer.Serialize(new
        {
            schemaVersion = "jd-stage2/v2",
            scores = items.Select(item => new
            {
                reqId = item.ItemId,
                handlerCode = item.HandlerCode,
                reasoning = "Detailed provider explanation"
            }),
            narrative = "Candidate summary"
        });

    private static int Count(string value, string token) =>
        value.Split(token, StringSplitOptions.None).Length - 1;
}
