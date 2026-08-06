using System.Threading.Tasks;
using ITHunterview.Service.Constant.Prompts;
using ITHunterview.Service.DTOs.PromptAdmin;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Service;
using ITHunterview.Service.Utils;
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
            .Setup(x => x.GenerateTextAsync(It.IsAny<string>(), It.IsAny<string>(), "test-provider"))
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

        var service = new JobAnalysisExtractionService(aiService.Object, promptService.Object, validator.Object);

        await service.ExtractWithActivePromptsAsync(new JobAnalysisInputSnapshot { Title = "Backend Engineer" });

        promptService.Verify(x => x.GetActivePromptPairSnapshotAsync(
            JdAnalysisPromptContract.SystemPromptKey,
            JdAnalysisPromptContract.UserPromptKey,
            default), Times.Once);
        promptService.Verify(x => x.GetActivePromptSnapshotAsync(It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()), Times.Never);
    }
}
