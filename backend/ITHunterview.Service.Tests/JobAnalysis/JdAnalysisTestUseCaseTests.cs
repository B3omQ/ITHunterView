using System.Text.Json;
using FluentAssertions;
using ITHunterview.Service.Constant.Prompts;
using ITHunterview.Service.DTOs.PromptAdmin;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Service;
using ITHunterview.Service.UseCase;
using ITHunterview.Service.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ITHunterview.Service.Tests.JobAnalysis;

public sealed class JdAnalysisTestUseCaseTests
{
    [Fact]
    public async Task AnalyzeAsync_PastedJd_ReturnsCanonicalEffectiveAnalysis()
    {
        var ai = new Mock<IAiService>();
        ai.Setup(service => service.GetActiveProviderNameAsync()).ReturnsAsync("test-provider");
        ai.Setup(service => service.GenerateTextAsync(
                It.Is<string>(prompt => prompt.Contains("Java", StringComparison.Ordinal)),
                It.IsAny<string>(),
                "test-provider",
                AiGenerationOptions.JdAnalysisJsonExtraction,
                It.IsAny<CancellationToken>(),
                "JD_EXTRACTION"))
            .ReturnsAsync(CompleteSingleGroupV5);

        var prompts = new Mock<IPromptManagementService>();
        prompts.Setup(service => service.GetActivePromptPairSnapshotAsync(
                JdAnalysisPromptContract.SystemPromptKey,
                JdAnalysisPromptContract.UserPromptKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PromptPairSnapshotDto
            {
                Contract = "jd-analysis-prompt/test",
                System = new PromptSnapshotDto { Content = "Extract grounded requirements." },
                User = new PromptSnapshotDto { Content = $"Analyze {JdAnalysisPromptContract.UserPlaceholder}" }
            });

        var extraction = new JobAnalysisExtractionService(
            ai.Object,
            prompts.Object,
            new JdAnalysisResponseValidator(),
            NullLogger<JobAnalysisExtractionService>.Instance);
        var useCase = new JdAnalysisTestUseCase(new JobAnalysisInputBuilder(), extraction);

        JsonElement result = await useCase.AnalyzeAsync("Requirements:\nUse Java.");

        result.GetProperty("schema_version").GetString().Should().Be("jd-analysis-effective/v1");
        result.GetProperty("analysis_quality").GetString().Should().Be("COMPLETE");
        result.GetProperty("matching_metrics")
            .GetProperty("skills_normalized")[0]
            .GetString().Should().Be("Java");
    }

    [Fact]
    public async Task AnalyzeAsync_WhitespaceJd_RejectsBeforeCallingProvider()
    {
        var extraction = new Mock<IJobAnalysisExtractionService>(MockBehavior.Strict);
        var useCase = new JdAnalysisTestUseCase(new JobAnalysisInputBuilder(), extraction.Object);

        var action = () => useCase.AnalyzeAsync("   \r\n\t");

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("JD text is required.*");
    }

    [Fact]
    public async Task AnalyzeAsync_InvalidModelOutput_ReturnsTypedUnprocessableError()
    {
        var ai = new Mock<IAiService>();
        ai.Setup(service => service.GetActiveProviderNameAsync()).ReturnsAsync("test-provider");
        ai.Setup(service => service.GenerateTextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                "test-provider",
                It.IsAny<AiGenerationOptions>(),
                It.IsAny<CancellationToken>(),
                "JD_EXTRACTION"))
            .ReturnsAsync("not-json");

        var prompts = new Mock<IPromptManagementService>();
        prompts.Setup(service => service.GetActivePromptPairSnapshotAsync(
                JdAnalysisPromptContract.SystemPromptKey,
                JdAnalysisPromptContract.UserPromptKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PromptPairSnapshotDto
            {
                Contract = "jd-analysis-prompt/test",
                System = new PromptSnapshotDto { Content = "Extract grounded requirements." },
                User = new PromptSnapshotDto { Content = $"Analyze {JdAnalysisPromptContract.UserPlaceholder}" }
            });

        var extraction = new JobAnalysisExtractionService(
            ai.Object,
            prompts.Object,
            new JdAnalysisResponseValidator(),
            NullLogger<JobAnalysisExtractionService>.Instance);
        var useCase = new JdAnalysisTestUseCase(new JobAnalysisInputBuilder(), extraction);

        var action = () => useCase.AnalyzeAsync("Requirements:\nUse Java.");

        var exception = await action.Should().ThrowAsync<JobAnalysisException>();
        exception.Which.HttpStatus.Should().Be(422);
        exception.Which.Code.Should().Be("JD_ANALYSIS_INVALID");
    }

    private const string CompleteSingleGroupV5 =
        "{\"schema_version\":\"jd-analysis/v5\",\"matching_metrics\":{\"job_titles_normalized\":[],\"total_years_exp\":0,\"domains\":[],\"requirement_groups\":[{\"source_requirement_id\":\"req-001\",\"intent\":\"qualification\",\"operator\":\"all_of\",\"importance\":\"must_have\",\"source_section\":\"requirements\",\"requirement_verbatim\":\"Use Java.\",\"items\":[{\"category\":\"tech_skill\",\"skill_name\":\"Java\",\"raw_mention\":\"Java\"}]}]}}";
}
