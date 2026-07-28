using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Job;
using ITHunterview.Service.Helpers;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.UseCase;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ITHunterview.Service.Tests.UseCase;

public class JobPostingsUseCaseRichTextTests
{
    [Fact]
    public async Task UpdateJobAsync_WhenOnlyMarkdownFormattingChanges_DoesNotMarkAnalysisStale()
    {
        var recruiterId = Guid.NewGuid();
        var activeRunId = Guid.NewGuid();
        var job = CreateDraft(recruiterId, activeRunId);
        var repository = CreateRepository(job);
        var useCase = CreateUseCase(repository.Object);

        var result = await useCase.UpdateJobAsync(job.Id, CreateUpdateDto(
            description: "**Build** React services",
            requirements: "1. React\n2. _Node.js_"), recruiterId);

        Assert.True(result.Success);
        Assert.Equal("**Build** React services", job.Description);
        Assert.Equal("1. React\n2. _Node.js_", job.Requirements);
        Assert.Equal(4, job.AnalysisRevision);
        Assert.Equal("SUCCESS", job.ParseStatus);
        Assert.Equal(activeRunId, job.ActiveAnalysisRunId);
        Assert.Equal("analysis-input-hash", job.AnalysisInputHash);
        repository.Verify(x => x.UpdateAsync(job), Times.Once);
    }

    [Fact]
    public async Task UpdateJobAsync_WhenRichTextVisibleContentChanges_MarksAnalysisStale()
    {
        var recruiterId = Guid.NewGuid();
        var job = CreateDraft(recruiterId, Guid.NewGuid());
        var repository = CreateRepository(job);
        var useCase = CreateUseCase(repository.Object);

        var result = await useCase.UpdateJobAsync(job.Id, CreateUpdateDto(
            description: "Build React and GraphQL services",
            requirements: "- React\n- Node.js"), recruiterId);

        Assert.True(result.Success);
        Assert.Equal(5, job.AnalysisRevision);
        Assert.Equal("STALE", job.ParseStatus);
        Assert.Null(job.ActiveAnalysisRunId);
        Assert.Null(job.AnalysisInputHash);
        Assert.Null(job.ParseError);
        repository.Verify(x => x.UpdateAsync(job), Times.Once);
    }

    [Fact]
    public async Task UpdateJobAsync_WhenHtmlIsSubmitted_RejectsBeforePersistence()
    {
        var recruiterId = Guid.NewGuid();
        var job = CreateDraft(recruiterId, Guid.NewGuid());
        var originalDescription = job.Description;
        var repository = CreateRepository(job);
        var useCase = CreateUseCase(repository.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => useCase.UpdateJobAsync(
            job.Id,
            CreateUpdateDto(description: "<script>alert('xss')</script>", requirements: "- React\n- Node.js"),
            recruiterId));

        Assert.Contains("raw HTML", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(originalDescription, job.Description);
        repository.Verify(x => x.UpdateAsync(It.IsAny<JobPostings>()), Times.Never);
    }

    private static JobPostingsUseCase CreateUseCase(IJobPostingRepository repository)
    {
        return new JobPostingsUseCase(
            repository,
            Mock.Of<ICompanyRepository>(),
            new JobAnalysisInputBuilder(),
            NullLogger<JobPostingsUseCase>.Instance);
    }

    private static Mock<IJobPostingRepository> CreateRepository(JobPostings job)
    {
        var repository = new Mock<IJobPostingRepository>();
        repository.Setup(x => x.GetByIdAsync(job.Id)).ReturnsAsync(job);
        repository.Setup(x => x.GetSkillsByJobIdAsync(job.Id)).ReturnsAsync(new List<JobSkillRequirementDto>());
        repository.Setup(x => x.UpdateAsync(job)).Returns(Task.CompletedTask);
        return repository;
    }

    private static JobPostings CreateDraft(Guid recruiterId, Guid activeRunId)
    {
        return new JobPostings
        {
            Id = Guid.NewGuid(),
            RecruiterId = recruiterId,
            Status = JobStatus.DRAFT,
            JobCode = "JB-TEST-001",
            Title = "Backend Engineer",
            Description = "Build React services",
            Requirements = "- React\n- Node.js",
            Benefits = "Health insurance",
            IncomeText = "Negotiable",
            WorkLocationText = "Ha Noi",
            Location = "Ha Noi",
            Currency = "VND",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            AnalysisRevision = 4,
            ParseStatus = "SUCCESS",
            ActiveAnalysisRunId = activeRunId,
            AnalysisInputHash = "analysis-input-hash",
            ParseError = "old error"
        };
    }

    private static UpdateJobPostingDto CreateUpdateDto(string description, string requirements)
    {
        return new UpdateJobPostingDto
        {
            JobCode = "JB-TEST-001",
            Title = "Backend Engineer",
            Description = description,
            Requirements = requirements,
            Benefits = "Health insurance",
            IncomeText = "Negotiable",
            WorkLocationText = "Ha Noi",
            Location = "Ha Noi",
            Currency = "VND"
        };
    }
}
