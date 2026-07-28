using System;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.JobAnalysis;
using ITHunterview.Service.Utils;
using ITHunterview.Service.Utils;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.UseCase;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ITHunterview.Service.Tests.JobAnalysis;

public class JobAnalysisUseCaseTests
{
    [Fact]
    public async Task FinalizeAsync_WhenReviewIsIncomplete_ThrowsTyped422Error()
    {
        var repository = new Mock<IJobAnalysisRepository>();
        repository
            .Setup(x => x.FinalizeAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), default))
            .ReturnsAsync(new FinalizeJobResult
            {
                Success = false,
                ErrorCode = "INCOMPLETE_REVIEW",
                ErrorMessage = "A skill still requires review."
            });

        var useCase = CreateUseCase(repository.Object);

        var exception = await Assert.ThrowsAsync<JobAnalysisException>(() => useCase.FinalizeAsync(
            Guid.NewGuid(), Guid.NewGuid(), new FinalizeJobRequestDto
            {
                AnalysisRunId = Guid.NewGuid(),
                ExpectedJobRevision = 1,
                ExpectedDecisionVersion = 1
            }));

        Assert.Equal("INCOMPLETE_REVIEW", exception.Code);
        Assert.Equal(422, exception.HttpStatus);
    }

    [Fact]
    public async Task FinalizeAsync_WhenSuccessful_ReturnsPublishedStatus()
    {
        var jobId = Guid.NewGuid();
        var repository = new Mock<IJobAnalysisRepository>();
        repository
            .Setup(x => x.FinalizeAsync(jobId, It.IsAny<Guid>(), It.IsAny<Guid>(), 1, 0, true, false, default))
            .ReturnsAsync(new FinalizeJobResult
            {
                Success = true,
                SkillCount = 0,
                Job = new JobPostings
                {
                    Id = jobId,
                    Status = JobStatus.PUBLISHED,
                    ParseStatus = "SUCCESS",
                    PublishedAt = DateTime.UtcNow
                }
            });

        var useCase = CreateUseCase(repository.Object);

        var result = await useCase.FinalizeAsync(jobId, Guid.NewGuid(), new FinalizeJobRequestDto
        {
            AnalysisRunId = Guid.NewGuid(),
            ExpectedJobRevision = 1,
            ExpectedDecisionVersion = 0,
            ConfirmNoStandardSkills = true
        });

        Assert.True(result.Success);
        Assert.Equal("PUBLISHED", result.Status);
        Assert.Equal("SUCCESS", result.ParseStatus);
    }

    private static JobAnalysisUseCase CreateUseCase(IJobAnalysisRepository repository)
    {
        return new JobAnalysisUseCase(
            repository,
            Mock.Of<IJobAnalysisInputBuilder>(),
            Mock.Of<IPromptManagementService>(),
            NullLogger<JobAnalysisUseCase>.Instance);
    }
}
