using System.Net;
using FluentAssertions;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Constant.Prompts;
using ITHunterview.Service.DTOs.PromptAdmin;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Service;
using ITHunterview.Service.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ITHunterview.Service.Tests.JobAnalysis;

public sealed class JobAnalysisExtractionServiceTests
{
    [Fact]
    public async Task ExtractWithActivePrompts_UsesCompatiblePairAndApplicationOwnedV5Schema()
    {
        var ai = CreateAi(CompleteSingleGroupV5);
        var prompts = new Mock<IPromptManagementService>();
        prompts.Setup(service => service.GetActivePromptPairSnapshotAsync(
                JdAnalysisPromptContract.SystemPromptKey,
                JdAnalysisPromptContract.UserPromptKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PromptPairSnapshotDto
            {
                Contract = "jd-analysis-prompt/custom-compatible",
                System = new PromptSnapshotDto { Content = "semantic system from database" },
                User = new PromptSnapshotDto { Content = $"user {JdAnalysisPromptContract.UserPlaceholder}" }
            });
        var service = CreateService(ai, prompts.Object);

        var result = await service.ExtractWithActivePromptsAsync(new JobAnalysisInputSnapshot { Title = "Backend" });

        result.Quality.Should().Be(JdAnalysisQuality.COMPLETE);
        prompts.Verify(candidate => candidate.GetActivePromptPairSnapshotAsync(
            JdAnalysisPromptContract.SystemPromptKey,
            JdAnalysisPromptContract.UserPromptKey,
            It.IsAny<CancellationToken>()), Times.Once);
        prompts.Verify(candidate => candidate.GetActivePromptSnapshotAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        ai.Verify(candidate => candidate.GenerateTextAsync(
            It.IsAny<string>(),
            It.Is<string>(system =>
                system.Contains("semantic system from database", StringComparison.Ordinal) &&
                system.Contains(JdAnalysisOutputSchema.BeginMarker, StringComparison.Ordinal) &&
                system.Contains("\"schema_version\": \"jd-analysis/v5\"", StringComparison.Ordinal)),
            "test-provider",
            AiGenerationOptions.JdAnalysisJsonExtraction,
            It.IsAny<CancellationToken>(),
            "JD_EXTRACTION"), Times.Once);
    }

    [Fact]
    public async Task ExtractAsync_FirstCompleteResult_DoesNotCallProviderAgain()
    {
        var ai = CreateAi(CompleteSingleGroupV5);
        var service = CreateService(ai);

        var result = await service.ExtractAsync(new JobAnalysisInputSnapshot(), "system", "user [JOB_INPUT_JSON]");

        result.Quality.Should().Be(JdAnalysisQuality.COMPLETE);
        result.ProviderRequestCount.Should().Be(1);
        VerifyProfileCalls(ai, first: 1, retry: 0);
    }

    [Fact]
    public async Task ExtractAsync_TruncatedFirstResult_RetriesOnceWithLargerProfileAndReturnsComplete()
    {
        var ai = CreateAiSequence(TruncatedAfterOneGroup, CompleteSingleGroupV5);
        var service = CreateService(ai);

        var result = await service.ExtractAsync(
            new JobAnalysisInputSnapshot { Requirements = "Use Java." },
            "system",
            "user [JOB_INPUT_JSON]");

        result.Quality.Should().Be(JdAnalysisQuality.COMPLETE);
        result.ProviderRequestCount.Should().Be(2);
        VerifyProfileCalls(ai, first: 1, retry: 1);
    }

    [Fact]
    public async Task ExtractAsync_WhenBothAttemptsAreInvalid_ReturnsRawTextFallback()
    {
        var ai = CreateAiSequence("not-json", "still-not-json");
        var service = CreateService(ai);

        var result = await service.ExtractAsync(
            new JobAnalysisInputSnapshot { Title = "Backend", Requirements = "Use Java." },
            "system",
            "user [JOB_INPUT_JSON]");

        result.Quality.Should().Be(JdAnalysisQuality.INVALID);
        result.UsesRawTextFallback.Should().BeTrue();
        result.RawTextFallback.Should().Contain("Backend").And.Contain("Use Java.");
        result.ProviderRequestCount.Should().Be(2);
    }

    [Fact]
    public async Task ExtractAsync_FirstEmptyGroupSet_RetriesOnceAndReturnsSecondComplete()
    {
        var ai = CreateAiSequence(EmptyV5, CompleteSingleGroupV5);
        var result = await CreateService(ai).ExtractAsync(
            new JobAnalysisInputSnapshot { Title = "Backend", Requirements = "Use Java." },
            "system",
            "user [JOB_INPUT_JSON]");

        result.Quality.Should().Be(JdAnalysisQuality.COMPLETE);
        result.ProviderRequestCount.Should().Be(2);
        result.UsesRawTextFallback.Should().BeFalse();
        VerifyProfileCalls(ai, first: 1, retry: 1);
    }

    [Fact]
    public async Task ExtractAsync_BothAttemptsHaveEmptyGroupSet_ReturnsInvalidRawTextFallback()
    {
        var ai = CreateAiSequence(EmptyV5, EmptyV5);
        var result = await CreateService(ai).ExtractAsync(
            new JobAnalysisInputSnapshot { Title = "Backend", Requirements = "Use Java." },
            "system",
            "user [JOB_INPUT_JSON]");

        result.Quality.Should().Be(JdAnalysisQuality.INVALID);
        result.Validation.FailureCode.Should().Be("NO_USABLE_REQUIREMENT_GROUPS");
        result.UsesRawTextFallback.Should().BeTrue();
        result.RawTextFallback.Should().Contain("Backend").And.Contain("Use Java.");
        result.ProviderRequestCount.Should().Be(2);
        VerifyProfileCalls(ai, first: 1, retry: 1);
    }

    [Fact]
    public async Task ExtractAsync_SecondPartialCannotReplaceBetterFirstPartial()
    {
        var ai = CreateAiSequence(TruncatedAfterTwoGroups, TruncatedAfterOneGroup);
        var service = CreateService(ai);

        var result = await service.ExtractAsync(new JobAnalysisInputSnapshot(), "system", "user [JOB_INPUT_JSON]");

        result.Quality.Should().Be(JdAnalysisQuality.PARTIAL);
        result.Coverage.AcceptedGroupCount.Should().Be(2);
        result.ProviderRequestCount.Should().Be(2);
    }

    [Fact]
    public async Task ExtractAsync_TransientHttpFailure_RetriesOnce()
    {
        var ai = new Mock<IAiService>();
        ai.Setup(service => service.GetActiveProviderNameAsync()).ReturnsAsync("test-provider");
        ai.SetupSequence(service => service.GenerateTextAsync(
                It.IsAny<string>(), It.IsAny<string>(), "test-provider",
                It.IsAny<AiGenerationOptions>(), It.IsAny<CancellationToken>(), "JD_EXTRACTION"))
            .ThrowsAsync(new HttpRequestException("temporary", null, HttpStatusCode.ServiceUnavailable))
            .ReturnsAsync(CompleteSingleGroupV5);
        var service = CreateService(ai);

        var result = await service.ExtractAsync(new JobAnalysisInputSnapshot(), "system", "user [JOB_INPUT_JSON]");

        result.Quality.Should().Be(JdAnalysisQuality.COMPLETE);
        result.ProviderRequestCount.Should().Be(2);
        VerifyProfileCalls(ai, first: 1, retry: 1);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task ExtractAsync_AuthenticationFailure_DoesNotRetry(HttpStatusCode statusCode)
    {
        var ai = new Mock<IAiService>();
        ai.Setup(service => service.GetActiveProviderNameAsync()).ReturnsAsync("test-provider");
        ai.Setup(service => service.GenerateTextAsync(
                It.IsAny<string>(), It.IsAny<string>(), "test-provider",
                It.IsAny<AiGenerationOptions>(), It.IsAny<CancellationToken>(), "JD_EXTRACTION"))
            .ThrowsAsync(new HttpRequestException("auth", null, statusCode));
        var service = CreateService(ai);

        var result = await service.ExtractAsync(new JobAnalysisInputSnapshot(), "system", "user [JOB_INPUT_JSON]");

        result.Quality.Should().Be(JdAnalysisQuality.INVALID);
        result.ProviderRequestCount.Should().Be(1);
        ai.Verify(service => service.GenerateTextAsync(
            It.IsAny<string>(), It.IsAny<string>(), "test-provider",
            It.IsAny<AiGenerationOptions>(), It.IsAny<CancellationToken>(), "JD_EXTRACTION"), Times.Once);
    }

    [Fact]
    public async Task ExtractAsync_InvalidProviderConfiguration_DoesNotRetry()
    {
        var ai = new Mock<IAiService>();
        ai.Setup(service => service.GetActiveProviderNameAsync()).ReturnsAsync("test-provider");
        ai.Setup(service => service.GenerateTextAsync(
                It.IsAny<string>(), It.IsAny<string>(), "test-provider",
                It.IsAny<AiGenerationOptions>(), It.IsAny<CancellationToken>(), "JD_EXTRACTION"))
            .ThrowsAsync(new InvalidOperationException("AI_PROVIDER_NOT_CONFIGURED"));
        var service = CreateService(ai);

        var result = await service.ExtractAsync(new JobAnalysisInputSnapshot(), "system", "user [JOB_INPUT_JSON]");

        result.Quality.Should().Be(JdAnalysisQuality.INVALID);
        result.ProviderRequestCount.Should().Be(1);
        ai.Verify(service => service.GenerateTextAsync(
            It.IsAny<string>(), It.IsAny<string>(), "test-provider",
            It.IsAny<AiGenerationOptions>(), It.IsAny<CancellationToken>(), "JD_EXTRACTION"), Times.Once);
    }

    [Fact]
    public async Task ExtractAsync_CancellationIsPropagatedAndNotRetried()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var ai = new Mock<IAiService>();
        ai.Setup(service => service.GetActiveProviderNameAsync()).ReturnsAsync("test-provider");
        ai.Setup(service => service.GenerateTextAsync(
                It.IsAny<string>(), It.IsAny<string>(), "test-provider",
                It.IsAny<AiGenerationOptions>(), cancellation.Token, "JD_EXTRACTION"))
            .ThrowsAsync(new OperationCanceledException(cancellation.Token));
        var service = CreateService(ai);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ExtractAsync(
            new JobAnalysisInputSnapshot(), "system", "user [JOB_INPUT_JSON]", cancellation.Token));

        ai.Verify(service => service.GenerateTextAsync(
            It.IsAny<string>(), It.IsAny<string>(), "test-provider",
            It.IsAny<AiGenerationOptions>(), cancellation.Token, "JD_EXTRACTION"), Times.Once);
    }

    private static JobAnalysisExtractionService CreateService(
        Mock<IAiService> ai,
        IPromptManagementService? prompts = null) =>
        new(
            ai.Object,
            prompts ?? new Mock<IPromptManagementService>().Object,
            new JdAnalysisResponseValidator(),
            NullLogger<JobAnalysisExtractionService>.Instance);

    private static Mock<IAiService> CreateAi(string response)
    {
        var ai = new Mock<IAiService>();
        ai.Setup(service => service.GetActiveProviderNameAsync()).ReturnsAsync("test-provider");
        ai.Setup(service => service.GenerateTextAsync(
                It.IsAny<string>(), It.IsAny<string>(), "test-provider",
                It.IsAny<AiGenerationOptions>(), It.IsAny<CancellationToken>(), "JD_EXTRACTION"))
            .ReturnsAsync(response);
        return ai;
    }

    private static Mock<IAiService> CreateAiSequence(params string[] responses)
    {
        var ai = new Mock<IAiService>();
        ai.Setup(service => service.GetActiveProviderNameAsync()).ReturnsAsync("test-provider");
        var sequence = ai.SetupSequence(service => service.GenerateTextAsync(
            It.IsAny<string>(), It.IsAny<string>(), "test-provider",
            It.IsAny<AiGenerationOptions>(), It.IsAny<CancellationToken>(), "JD_EXTRACTION"));
        foreach (var response in responses)
        {
            sequence.ReturnsAsync(response);
        }
        return ai;
    }

    private static void VerifyProfileCalls(Mock<IAiService> ai, int first, int retry)
    {
        ai.Verify(service => service.GenerateTextAsync(
            It.IsAny<string>(), It.IsAny<string>(), "test-provider",
            AiGenerationOptions.JdAnalysisJsonExtraction, It.IsAny<CancellationToken>(), "JD_EXTRACTION"), Times.Exactly(first));
        ai.Verify(service => service.GenerateTextAsync(
            It.IsAny<string>(), It.IsAny<string>(), "test-provider",
            AiGenerationOptions.JdAnalysisJsonRetry, It.IsAny<CancellationToken>(), "JD_EXTRACTION"), Times.Exactly(retry));
    }

    private const string EmptyV5 =
        "{\"schema_version\":\"jd-analysis/v5\",\"matching_metrics\":{\"job_titles_normalized\":[],\"total_years_exp\":0,\"domains\":[],\"requirement_groups\":[]}}";

    private const string GroupOne =
        "{\"source_requirement_id\":\"req-001\",\"intent\":\"qualification\",\"operator\":\"all_of\",\"importance\":\"must_have\",\"source_section\":\"requirements\",\"requirement_verbatim\":\"Use Java.\",\"items\":[{\"category\":\"tech_skill\",\"skill_name\":\"Java\",\"raw_mention\":\"Java\"}]}";

    private const string GroupTwo =
        "{\"source_requirement_id\":\"req-002\",\"intent\":\"qualification\",\"operator\":\"all_of\",\"importance\":\"must_have\",\"source_section\":\"requirements\",\"requirement_verbatim\":\"Use SQL.\",\"items\":[{\"category\":\"tech_skill\",\"skill_name\":\"SQL\",\"raw_mention\":\"SQL\"}]}";

    private const string Prefix =
        "{\"schema_version\":\"jd-analysis/v5\",\"matching_metrics\":{\"job_titles_normalized\":[],\"total_years_exp\":0,\"domains\":[],\"requirement_groups\":[";

    private const string CompleteSingleGroupV5 = Prefix + GroupOne + "]}}";

    private static readonly string TruncatedAfterOneGroup = Prefix + GroupOne + ",";
    private static readonly string TruncatedAfterTwoGroups = Prefix + GroupOne + "," + GroupTwo + ",";
}
