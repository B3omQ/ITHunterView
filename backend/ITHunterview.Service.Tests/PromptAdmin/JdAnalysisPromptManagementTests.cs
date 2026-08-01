using System;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Constant.Prompts;
using ITHunterview.Service.DTOs.PromptAdmin;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.UseCase;
using Moq;

namespace ITHunterview.Service.Tests.PromptAdmin;

public class JdAnalysisPromptManagementTests
{
    [Fact]
    public async Task CreateVersion_WhenJdUserPromptMissesPlaceholder_RejectsDraft()
    {
        var repository = new Mock<IPromptAdminRepository>();
        var promptId = Guid.NewGuid();
        repository.Setup(x => x.GetPromptWithHistoryAsync(promptId))
            .ReturnsAsync(new Prompts { Id = promptId, PromptKey = JdAnalysisPromptContract.UserPromptKey });

        var useCase = new PromptAdminUseCase(repository.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => useCase.CreatePromptVersionAsync(
            promptId,
            new CreatePromptVersionDto
            {
                VersionTag = "v3.0.0",
                Content = "Analyze this job.",
                ModelConfig = "{\"contract\":\"jd-analysis/v3\",\"role\":\"user\"}",
                MakeActive = false
            },
            Guid.NewGuid()));

        Assert.Contains(JdAnalysisPromptContract.UserPlaceholder, exception.Message);
        repository.Verify(x => x.CreatePromptVersionAsync(It.IsAny<PromptVersions>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task CreateVersion_WhenJdPromptWouldActivateIndividually_RejectsRequest()
    {
        var repository = new Mock<IPromptAdminRepository>();
        var promptId = Guid.NewGuid();
        repository.Setup(x => x.GetPromptWithHistoryAsync(promptId))
            .ReturnsAsync(new Prompts { Id = promptId, PromptKey = JdAnalysisPromptContract.UserPromptKey });

        var useCase = new PromptAdminUseCase(repository.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => useCase.CreatePromptVersionAsync(
            promptId,
            new CreatePromptVersionDto
            {
                VersionTag = "v3.0.0",
                Content = $"Analyze this job: {JdAnalysisPromptContract.UserPlaceholder}",
                ModelConfig = "{\"contract\":\"jd-analysis/v3\",\"role\":\"user\"}",
                MakeActive = true
            },
            Guid.NewGuid()));

        Assert.Contains("prompt-pair", exception.Message);
        repository.Verify(x => x.CreatePromptVersionAsync(It.IsAny<PromptVersions>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task ActivatePair_WhenJdContractsMatch_ActivatesBothVersionsAtomically()
    {
        var repository = new Mock<IPromptAdminRepository>();
        var systemPromptId = Guid.NewGuid();
        var userPromptId = Guid.NewGuid();
        var systemVersionId = Guid.NewGuid();
        var userVersionId = Guid.NewGuid();

        repository.Setup(x => x.GetPromptVersionAsync(systemVersionId))
            .ReturnsAsync(new PromptVersions
            {
                Id = systemVersionId,
                PromptId = systemPromptId,
                Prompt = new Prompts { Id = systemPromptId, PromptKey = JdAnalysisPromptContract.SystemPromptKey },
                ModelConfig = "{\"contract\":\"jd-analysis/v3\",\"role\":\"system\"}"
            });
        repository.Setup(x => x.GetPromptVersionAsync(userVersionId))
            .ReturnsAsync(new PromptVersions
            {
                Id = userVersionId,
                PromptId = userPromptId,
                Prompt = new Prompts { Id = userPromptId, PromptKey = JdAnalysisPromptContract.UserPromptKey },
                ModelConfig = "{\"contract\":\"jd-analysis/v3\",\"role\":\"user\"}"
            });

        var useCase = new PromptAdminUseCase(repository.Object);

        await useCase.ActivateJdAnalysisPromptPairAsync(systemVersionId, userVersionId, Guid.NewGuid());

        repository.Verify(x => x.ActivatePromptPairAsync(systemPromptId, systemVersionId, userPromptId, userVersionId), Times.Once);
    }

    [Fact]
    public async Task ActivatePair_WhenJdContractsDiffer_RejectsWithoutChangingActiveVersions()
    {
        var repository = new Mock<IPromptAdminRepository>();
        var systemVersionId = Guid.NewGuid();
        var userVersionId = Guid.NewGuid();

        repository.Setup(x => x.GetPromptVersionAsync(systemVersionId))
            .ReturnsAsync(new PromptVersions
            {
                Id = systemVersionId,
                PromptId = Guid.NewGuid(),
                Prompt = new Prompts { PromptKey = JdAnalysisPromptContract.SystemPromptKey },
                ModelConfig = "{\"contract\":\"jd-analysis/v3\",\"role\":\"system\"}"
            });
        repository.Setup(x => x.GetPromptVersionAsync(userVersionId))
            .ReturnsAsync(new PromptVersions
            {
                Id = userVersionId,
                PromptId = Guid.NewGuid(),
                Prompt = new Prompts { PromptKey = JdAnalysisPromptContract.UserPromptKey },
                ModelConfig = "{\"contract\":\"jd-analysis/v2\",\"role\":\"user\"}"
            });

        var useCase = new PromptAdminUseCase(repository.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ActivateJdAnalysisPromptPairAsync(
            systemVersionId, userVersionId, Guid.NewGuid()));

        repository.Verify(x => x.ActivatePromptPairAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }
}
