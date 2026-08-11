using System.Threading.Tasks;
using FluentAssertions;
using System.Threading;
using ITHunterview.Service.Constant.Prompts;
using ITHunterview.Service.DTOs.PromptAdmin;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Service;
using ITHunterview.Service.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ITHunterview.Service.Tests.JobAnalysis;

public class JobAnalysisExtractionServiceTests
{
    [Fact]
    public async Task ExtractWithActivePrompts_UsesOneCompatibleJdPromptPair()
    {
        var aiService = new Mock<IAiService>();
        aiService.Setup(x => x.GetActiveProviderNameAsync()).ReturnsAsync("test-provider");
        aiService
            .Setup(x => x.GenerateTextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                "test-provider",
                AiGenerationOptions.StrictJsonExtraction,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("{}");

        var promptService = new Mock<IPromptManagementService>();
        promptService.Setup(x => x.GetActivePromptPairSnapshotAsync(
                JdAnalysisPromptContract.SystemPromptKey,
                JdAnalysisPromptContract.UserPromptKey,
                default))
            .ReturnsAsync(new PromptPairSnapshotDto
            {
                Contract = JdAnalysisPromptContract.ContractV3,
                System = new PromptSnapshotDto { Content = "system from database" },
                User = new PromptSnapshotDto { Content = $"user {JdAnalysisPromptContract.UserPlaceholder}" }
            });

        var validator = new Mock<IJdAnalysisResponseValidator>();
        validator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<JobAnalysisInputSnapshot>()))
            .Returns(new ValidationResult<ValidatedJobAnalysis>());

        var service = new JobAnalysisExtractionService(
            aiService.Object,
            promptService.Object,
            validator.Object,
            NullLogger<JobAnalysisExtractionService>.Instance);

        await service.ExtractWithActivePromptsAsync(new JobAnalysisInputSnapshot { Title = "Backend Engineer" });

        promptService.Verify(x => x.GetActivePromptPairSnapshotAsync(
            JdAnalysisPromptContract.SystemPromptKey,
            JdAnalysisPromptContract.UserPromptKey,
            default), Times.Once);
        promptService.Verify(x => x.GetActivePromptSnapshotAsync(It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()), Times.Never);
        aiService.Verify(x => x.GenerateTextAsync(
            It.IsAny<string>(),
            It.Is<string>(system =>
                system.Contains("system from database", StringComparison.Ordinal) &&
                system.Contains(JdAnalysisOutputSchema.BeginMarker, StringComparison.Ordinal) &&
                system.Contains("\"schema_version\": \"jd-analysis/v4\"", StringComparison.Ordinal)),
            "test-provider",
            AiGenerationOptions.StrictJsonExtraction,
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
        public async Task ExtractWithActivePrompts_ForwardsCancellationTokenToAiProvider()
    {
        using var cancellation = new CancellationTokenSource();
        var aiService = new Mock<IAiService>();
        aiService.Setup(x => x.GetActiveProviderNameAsync()).ReturnsAsync("test-provider");
        aiService
            .Setup(x => x.GenerateTextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                "test-provider",
                AiGenerationOptions.StrictJsonExtraction,
                cancellation.Token))
            .ReturnsAsync("{}");

        var promptService = new Mock<IPromptManagementService>();
        promptService.Setup(x => x.GetActivePromptPairSnapshotAsync(
                JdAnalysisPromptContract.SystemPromptKey,
                JdAnalysisPromptContract.UserPromptKey,
                cancellation.Token))
            .ReturnsAsync(new PromptPairSnapshotDto
            {
                Contract = JdAnalysisPromptContract.ContractV3,
                System = new PromptSnapshotDto { Content = "system" },
                User = new PromptSnapshotDto { Content = $"user {JdAnalysisPromptContract.UserPlaceholder}" }
            });

        var validator = new Mock<IJdAnalysisResponseValidator>();
        validator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<JobAnalysisInputSnapshot>()))
            .Returns(new ValidationResult<ValidatedJobAnalysis>());

        var service = new JobAnalysisExtractionService(
            aiService.Object,
            promptService.Object,
            validator.Object,
            NullLogger<JobAnalysisExtractionService>.Instance);

        await service.ExtractWithActivePromptsAsync(
            new JobAnalysisInputSnapshot { Title = "Backend Engineer" },
            cancellation.Token);

            aiService.Verify(x => x.GenerateTextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                "test-provider",
                AiGenerationOptions.StrictJsonExtraction,
                cancellation.Token), Times.Exactly(2));
        }

        [Fact]
        public async Task ExtractWithActivePrompts_UsesStrictJsonGenerationProfile()
        {
            var aiService = new Mock<IAiService>();
            aiService.Setup(x => x.GetActiveProviderNameAsync()).ReturnsAsync("test-provider");
            aiService.Setup(x => x.GenerateTextAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    "test-provider",
                    AiGenerationOptions.StrictJsonExtraction,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync("{}");

            var promptService = new Mock<IPromptManagementService>();
            promptService.Setup(x => x.GetActivePromptPairSnapshotAsync(
                    JdAnalysisPromptContract.SystemPromptKey,
                    JdAnalysisPromptContract.UserPromptKey,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PromptPairSnapshotDto
                {
                    Contract = JdAnalysisPromptContract.ContractV3,
                    System = new PromptSnapshotDto { Content = "system" },
                    User = new PromptSnapshotDto { Content = $"user {JdAnalysisPromptContract.UserPlaceholder}" }
                });

            var validator = new Mock<IJdAnalysisResponseValidator>();
            validator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<JobAnalysisInputSnapshot>()))
                .Returns(new ValidationResult<ValidatedJobAnalysis>());

            var service = new JobAnalysisExtractionService(
                aiService.Object,
                promptService.Object,
                validator.Object,
                NullLogger<JobAnalysisExtractionService>.Instance);

            await service.ExtractWithActivePromptsAsync(new JobAnalysisInputSnapshot());

            aiService.Verify(x => x.GenerateTextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                "test-provider",
                AiGenerationOptions.StrictJsonExtraction,
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ExtractAsync_RetriesOneTruncatedOutputAndReturnsCompleteAttempt()
        {
            var aiService = new Mock<IAiService>();
            aiService.Setup(x => x.GetActiveProviderNameAsync()).ReturnsAsync("test-provider");
            aiService.SetupSequence(x => x.GenerateTextAsync(
                    It.IsAny<string>(), It.IsAny<string>(), "test-provider",
                    AiGenerationOptions.StrictJsonExtraction, It.IsAny<CancellationToken>()))
                .ReturnsAsync("{\"schema_version\":\"jd-analysis/v4\",\"matching_metrics\":{\"requirement_groups\":[{\"operator\":\"all_of\"")
                .ReturnsAsync("{\"schema_version\":\"jd-analysis/v4\",\"matching_metrics\":{\"job_titles_normalized\":[],\"total_years_exp\":0,\"domains\":[],\"requirement_groups\":[]}}");

            var promptService = new Mock<IPromptManagementService>();
            var validator = new Mock<IJdAnalysisResponseValidator>();
            validator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<JobAnalysisInputSnapshot>()))
                .Returns(new ValidationResult<ValidatedJobAnalysis>
                {
                    IsValid = true,
                    Quality = ITHunterview.Domain.Enums.JdAnalysisQuality.COMPLETE,
                    Data = new ValidatedJobAnalysis()
                });

            var service = new JobAnalysisExtractionService(
                aiService.Object, promptService.Object, validator.Object,
                NullLogger<JobAnalysisExtractionService>.Instance);

            var result = await service.ExtractAsync(
                new JobAnalysisInputSnapshot { Requirements = "Use C#." }, "system", "user [JOB_INPUT_JSON]");

            result.ProviderRequestCount.Should().Be(2);
            result.Quality.Should().Be(ITHunterview.Domain.Enums.JdAnalysisQuality.COMPLETE);
            result.PersistableAnalysisJson.Should().NotBeNullOrWhiteSpace();
            result.UsesRawTextFallback.Should().BeFalse();
        }

        [Fact]
        public async Task ExtractAsync_WhenBothAttemptsAreInvalid_ReturnsRawTextFallbackInsteadOfThrowing()
        {
            var aiService = new Mock<IAiService>();
            aiService.Setup(x => x.GetActiveProviderNameAsync()).ReturnsAsync("test-provider");
            aiService.Setup(x => x.GenerateTextAsync(
                    It.IsAny<string>(), It.IsAny<string>(), "test-provider",
                    AiGenerationOptions.StrictJsonExtraction, It.IsAny<CancellationToken>()))
                .ReturnsAsync("not-json");

            var validator = new Mock<IJdAnalysisResponseValidator>();
            validator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<JobAnalysisInputSnapshot>()))
                .Returns(new ValidationResult<ValidatedJobAnalysis>
                {
                    IsValid = false,
                    Quality = ITHunterview.Domain.Enums.JdAnalysisQuality.INVALID,
                    FailureCode = "INVALID_JSON_FORMAT"
                });

            var service = new JobAnalysisExtractionService(
                aiService.Object, new Mock<IPromptManagementService>().Object, validator.Object,
                NullLogger<JobAnalysisExtractionService>.Instance);

            var result = await service.ExtractAsync(
                new JobAnalysisInputSnapshot { Title = "Backend", Requirements = "Use C#." },
                "system", "user [JOB_INPUT_JSON]");

            result.ProviderRequestCount.Should().Be(2);
            result.Quality.Should().Be(ITHunterview.Domain.Enums.JdAnalysisQuality.INVALID);
            result.UsesRawTextFallback.Should().BeTrue();
            result.RawTextFallback.Should().Contain("Backend");
        }

        [Fact]
        public async Task ExtractAsync_WhenProviderFailsOnce_RetriesBeforeReturningFallback()
        {
            var aiService = new Mock<IAiService>();
            aiService.Setup(x => x.GetActiveProviderNameAsync()).ReturnsAsync("test-provider");
            aiService.SetupSequence(x => x.GenerateTextAsync(
                    It.IsAny<string>(), It.IsAny<string>(), "test-provider",
                    AiGenerationOptions.StrictJsonExtraction, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("temporary provider failure"))
                .ReturnsAsync("not-json");

            var validator = new Mock<IJdAnalysisResponseValidator>();
            validator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<JobAnalysisInputSnapshot>()))
                .Returns(new ValidationResult<ValidatedJobAnalysis>
                {
                    IsValid = false,
                    Quality = ITHunterview.Domain.Enums.JdAnalysisQuality.INVALID,
                    FailureCode = "INVALID_JSON_FORMAT"
                });

            var service = new JobAnalysisExtractionService(
                aiService.Object, new Mock<IPromptManagementService>().Object, validator.Object,
                NullLogger<JobAnalysisExtractionService>.Instance);

            var result = await service.ExtractAsync(
                new JobAnalysisInputSnapshot { Requirements = "Use C#." },
                "system", "user [JOB_INPUT_JSON]");

            result.ProviderRequestCount.Should().Be(2);
            result.Quality.Should().Be(ITHunterview.Domain.Enums.JdAnalysisQuality.INVALID);
            result.UsesRawTextFallback.Should().BeTrue();
            aiService.Verify(x => x.GenerateTextAsync(
                It.IsAny<string>(), It.IsAny<string>(), "test-provider",
                AiGenerationOptions.StrictJsonExtraction, It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ExtractAsync_WithTruncatedV4AndRealValidator_ReturnsUsablePartialAnalysis()
        {
            var aiService = new Mock<IAiService>();
            aiService.Setup(x => x.GetActiveProviderNameAsync()).ReturnsAsync("test-provider");
            aiService.Setup(x => x.GenerateTextAsync(
                    It.IsAny<string>(), It.IsAny<string>(), "test-provider",
                    AiGenerationOptions.StrictJsonExtraction, It.IsAny<CancellationToken>()))
                .ReturnsAsync("""
                    {"schema_version":"jd-analysis/v4","matching_metrics":{"job_titles_normalized":[],"total_years_exp":0,"domains":[],"requirement_groups":[{"operator":"all_of","importance":"must_have","source_section":"requirements","requirement_verbatim":"Use C#.","items":[{"category":"tech_skill","skill_name":"c#","raw_mention":"C#"}]},{"operator":"all_of","importance":"must_have","source_section":"requirements","requirement_verbatim":"Use PostgreSQL.","items":[{"category":"tech_skill","skill_name":"postgresql"
                    """);

            var service = new JobAnalysisExtractionService(
                aiService.Object,
                new Mock<IPromptManagementService>().Object,
                new JdAnalysisResponseValidator(),
                NullLogger<JobAnalysisExtractionService>.Instance);

            var result = await service.ExtractAsync(
                new JobAnalysisInputSnapshot { Requirements = "Use C#." },
                "system",
                "user [JOB_INPUT_JSON]");

            result.Quality.Should().Be(ITHunterview.Domain.Enums.JdAnalysisQuality.PARTIAL);
            result.Validation.IsUsable.Should().BeTrue();
            result.Coverage.AcceptedGroupCount.Should().Be(1);
            result.Coverage.DiscardedGroupCount.Should().Be(1);
            result.Diagnostics.Should().Contain(diagnostic => diagnostic.Code == "OUTPUT_TRUNCATED");
        }
}
