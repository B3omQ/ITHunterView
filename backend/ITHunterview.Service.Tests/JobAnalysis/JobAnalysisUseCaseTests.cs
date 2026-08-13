using System;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.JobAnalysis;
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

    [Fact]
    public async Task RequestAnalysisAsync_WhenJobNotFound_ThrowsKeyNotFoundException()
    {
        var repository = new Mock<IJobAnalysisRepository>();
        repository
            .Setup(x => x.GetRequestContextAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), default))
            .ReturnsAsync((JobAnalysisRequestContext?)null);

        var useCase = CreateUseCase(repository.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => useCase.RequestAnalysisAsync(
            Guid.NewGuid(), Guid.NewGuid(), new AnalyzeJobRequestDto { ExpectedRevision = 1 }));
    }

    [Fact]
    public async Task RequestAnalysisAsync_WhenPublishedAnalysisIsCurrent_ThrowsInvalidOperationException()
    {
        var jobId = Guid.NewGuid();
        var recruiterId = Guid.NewGuid();
        var repository = new Mock<IJobAnalysisRepository>();
        repository
            .Setup(x => x.GetRequestContextAsync(jobId, recruiterId, default))
            .ReturnsAsync(new JobAnalysisRequestContext
            {
                Job = new JobPostings
                {
                    Id = jobId,
                    Status = JobStatus.PUBLISHED,
                    AnalysisRevision = 1,
                    EffectiveAnalysisRevision = 1,
                    ParseStatus = "SUCCESS"
                },
                IsCompanyVerified = true
            });

        var useCase = CreateUseCase(repository.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.RequestAnalysisAsync(
            jobId, recruiterId, new AnalyzeJobRequestDto { ExpectedRevision = 1 }));

        Assert.Contains("JOB_ANALYSIS_NOT_REQUIRED", ex.Message);
    }

    [Fact]
    public async Task RequestAnalysisAsync_WhenPublishedHasUnappliedSemanticRevision_ReusesCurrentRun()
    {
        var jobId = Guid.NewGuid();
        var recruiterId = Guid.NewGuid();
        var run = new JobAnalysisRuns
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            InputRevision = 2,
            Status = JobAnalysisStatus.READY,
            IdempotencyKey = "published-edit"
        };
        var repository = new Mock<IJobAnalysisRepository>();
        repository
            .Setup(x => x.GetRequestContextAsync(jobId, recruiterId, default))
            .ReturnsAsync(new JobAnalysisRequestContext
            {
                Job = new JobPostings
                {
                    Id = jobId,
                    Status = JobStatus.PUBLISHED,
                    AnalysisRevision = 2,
                    EffectiveAnalysisRevision = 1,
                    ParseStatus = "STALE"
                },
                IsCompanyVerified = true
            });
        repository
            .Setup(x => x.FindByIdempotencyKeyAsync(jobId, "published-edit", default))
            .ReturnsAsync(run);
        repository
            .Setup(x => x.ActivateReusableRunAsync(jobId, run.Id, 2, default))
            .ReturnsAsync(true);

        var result = await CreateUseCase(repository.Object).RequestAnalysisAsync(
            jobId,
            recruiterId,
            new AnalyzeJobRequestDto { ExpectedRevision = 2, IdempotencyKey = "published-edit" });

        Assert.Equal(run.Id, result.RunId);
        Assert.True(result.IsReused);
    }

    [Fact]
    public async Task RetryAnalysisAsync_WhenPublishedCurrentRunFailed_AllowsRetry()
    {
        var jobId = Guid.NewGuid();
        var recruiterId = Guid.NewGuid();
        var failedRunId = Guid.NewGuid();
        var reusableRun = new JobAnalysisRuns
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            InputRevision = 3,
            Status = JobAnalysisStatus.PENDING,
            IdempotencyKey = "published-retry"
        };
        var context = new JobAnalysisRequestContext
        {
            Job = new JobPostings
            {
                Id = jobId,
                Status = JobStatus.PUBLISHED,
                AnalysisRevision = 3,
                EffectiveAnalysisRevision = 2,
                ParseStatus = "FAILED"
            },
            IsCompanyVerified = true
        };
        var repository = new Mock<IJobAnalysisRepository>();
        repository.Setup(x => x.GetRequestContextAsync(jobId, recruiterId, default)).ReturnsAsync(context);
        repository.Setup(x => x.GetRunAsync(failedRunId, default)).ReturnsAsync(new JobAnalysisRuns
        {
            Id = failedRunId,
            JobId = jobId,
            InputRevision = 3,
            Status = JobAnalysisStatus.FAILED
        });
        repository.Setup(x => x.FindByIdempotencyKeyAsync(jobId, "published-retry", default)).ReturnsAsync(reusableRun);
        repository.Setup(x => x.ActivateReusableRunAsync(jobId, reusableRun.Id, 3, default)).ReturnsAsync(true);

        var result = await CreateUseCase(repository.Object).RetryAnalysisAsync(
            jobId,
            failedRunId,
            recruiterId,
            new AnalyzeJobRequestDto { ExpectedRevision = 3, IdempotencyKey = "published-retry" });

        Assert.Equal(reusableRun.Id, result.RunId);
        Assert.True(result.IsQueued);
    }

    [Fact]
    public async Task RequestAnalysisAsync_WhenCompanyNotVerified_ThrowsInvalidOperationException()
    {
        var jobId = Guid.NewGuid();
        var recruiterId = Guid.NewGuid();
        var repository = new Mock<IJobAnalysisRepository>();
        repository
            .Setup(x => x.GetRequestContextAsync(jobId, recruiterId, default))
            .ReturnsAsync(new JobAnalysisRequestContext
            {
                Job = new JobPostings { Id = jobId, Status = JobStatus.DRAFT },
                IsCompanyVerified = false
            });

        var useCase = CreateUseCase(repository.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.RequestAnalysisAsync(
            jobId, recruiterId, new AnalyzeJobRequestDto { ExpectedRevision = 1 }));

        Assert.Contains("UNVERIFIED_COMPANY", ex.Message);
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
