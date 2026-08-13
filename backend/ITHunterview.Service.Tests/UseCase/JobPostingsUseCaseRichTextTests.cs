using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Job;
using ITHunterview.Service.Utils;
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
        Assert.False(result.Data!.RequiresAnalysis);
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

    [Theory]
    [InlineData("jobCode")]
    [InlineData("title")]
    [InlineData("benefits")]
    [InlineData("incomeText")]
    [InlineData("workLocationText")]
    [InlineData("minSalary")]
    [InlineData("maxSalary")]
    [InlineData("currency")]
    [InlineData("location")]
    [InlineData("applicationDeadline")]
    [InlineData("level")]
    [InlineData("workingModel")]
    [InlineData("jobExpertise")]
    [InlineData("jobDomain")]
    public async Task UpdateJobAsync_WhenPublishedNonAnalysisFieldChanges_DoesNotMarkAnalysisStale(string field)
    {
        var recruiterId = Guid.NewGuid();
        var activeRunId = Guid.NewGuid();
        var job = CreatePublished(recruiterId, activeRunId);
        var repository = CreateRepository(job);
        var useCase = CreateUseCase(repository.Object);
        var dto = CreateUpdateDto(job.Description, job.Requirements);
        ApplyNonAnalysisChange(dto, field);

        var result = await useCase.UpdateJobAsync(job.Id, dto, recruiterId);

        Assert.True(result.Success);
        Assert.Equal(JobStatus.PUBLISHED, job.Status);
        Assert.Equal(4, job.AnalysisRevision);
        Assert.Equal(4, job.EffectiveAnalysisRevision);
        Assert.Equal("SUCCESS", job.ParseStatus);
        Assert.Equal(activeRunId, job.ActiveAnalysisRunId);
        Assert.Equal(activeRunId, job.EffectiveAnalysisRunId);
        Assert.False(result.Data!.RequiresAnalysis);
        repository.Verify(x => x.UpdateAsync(job), Times.Once);
    }

    [Theory]
    [InlineData("description")]
    [InlineData("requirements")]
    public async Task UpdateJobAsync_WhenPublishedAnalysisSourceChanges_MarksOnlyNewRevisionStale(string field)
    {
        var recruiterId = Guid.NewGuid();
        var effectiveRunId = Guid.NewGuid();
        var job = CreatePublished(recruiterId, effectiveRunId);
        var publishedAt = job.PublishedAt;
        var expiresAt = job.ExpiresAt;
        var repository = CreateRepository(job);
        var useCase = CreateUseCase(repository.Object);
        var dto = CreateUpdateDto(
            field == "description" ? "Build React and GraphQL services" : job.Description,
            field == "requirements" ? "- React\n- Node.js\n- PostgreSQL" : job.Requirements);

        var result = await useCase.UpdateJobAsync(job.Id, dto, recruiterId);

        Assert.True(result.Success);
        Assert.Equal(JobStatus.PUBLISHED, job.Status);
        Assert.Equal(5, job.AnalysisRevision);
        Assert.Equal(4, job.EffectiveAnalysisRevision);
        Assert.Equal("STALE", job.ParseStatus);
        Assert.Null(job.ActiveAnalysisRunId);
        Assert.Null(job.AnalysisInputHash);
        Assert.Equal(effectiveRunId, job.EffectiveAnalysisRunId);
        Assert.Equal("{\"skills\":[\"React\"]}", job.ParsedData);
        Assert.Equal(publishedAt, job.PublishedAt);
        Assert.Equal(expiresAt, job.ExpiresAt);
        Assert.Equal(12, job.ApplicationCount);
        Assert.Equal(34, job.ViewCount);
        Assert.True(result.Data!.RequiresAnalysis);
        repository.Verify(x => x.UpdateAsync(job), Times.Once);
    }

    [Fact]
    public async Task UpdateJobAsync_WhenPendingReview_RemainsRejected()
    {
        var recruiterId = Guid.NewGuid();
        var job = CreateDraft(recruiterId, Guid.NewGuid());
        job.Status = JobStatus.PENDING_REVIEW;
        var repository = CreateRepository(job);
        var useCase = CreateUseCase(repository.Object);

        var result = await useCase.UpdateJobAsync(
            job.Id,
            CreateUpdateDto(job.Description, job.Requirements),
            recruiterId);

        Assert.False(result.Success);
        repository.Verify(x => x.UpdateAsync(It.IsAny<JobPostings>()), Times.Never);
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
            Mock.Of<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>(),
            Mock.Of<ITHunterview.Service.Interface.UseCase.INotificationUseCase>(),
            Mock.Of<ITHunterview.Service.Interface.UseCase.ICandidateFeatureUsageUseCase>(),
            null!, // UpdateJobAsync does not access the DbContext used by Extend/Push Top transactions.
            Mock.Of<Microsoft.AspNetCore.SignalR.IHubContext<ITHunterview.Service.Hubs.NotificationHub>>(),
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

    private static JobPostings CreatePublished(Guid recruiterId, Guid effectiveRunId)
    {
        var job = CreateDraft(recruiterId, effectiveRunId);
        job.Status = JobStatus.PUBLISHED;
        job.EffectiveAnalysisRevision = job.AnalysisRevision;
        job.EffectiveAnalysisRunId = effectiveRunId;
        job.ParsedData = "{\"skills\":[\"React\"]}";
        job.PublishedAt = DateTime.UtcNow.AddDays(-10);
        job.ExpiresAt = DateTime.UtcNow.AddDays(20);
        job.ApplicationCount = 12;
        job.ViewCount = 34;
        return job;
    }

    private static void ApplyNonAnalysisChange(UpdateJobPostingDto dto, string field)
    {
        switch (field)
        {
            case "jobCode": dto.JobCode = "JB-TEST-UPDATED"; break;
            case "title": dto.Title = "Senior Backend Engineer"; break;
            case "benefits": dto.Benefits = "Health insurance and bonus"; break;
            case "incomeText": dto.IncomeText = "Up to 50M"; break;
            case "workLocationText": dto.WorkLocationText = "Ho Chi Minh City"; break;
            case "minSalary": dto.MinSalary = 20_000_000; break;
            case "maxSalary": dto.MaxSalary = 50_000_000; break;
            case "currency": dto.Currency = "USD"; break;
            case "location": dto.Location = "Ho Chi Minh City"; break;
            case "applicationDeadline": dto.ApplicationDeadline = DateTime.UtcNow.AddDays(30); break;
            case "level": dto.Level = "Senior"; break;
            case "workingModel": dto.WorkingModel = "Hybrid"; break;
            case "jobExpertise": dto.JobExpertise = "Backend"; break;
            case "jobDomain": dto.JobDomain = new List<string> { "Cloud" }; break;
            default: throw new ArgumentOutOfRangeException(nameof(field), field, null);
        }
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
