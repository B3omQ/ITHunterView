using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Entities.Cv;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.FeatureUsage;
using ITHunterview.Service.DTOs.Job;
using ITHunterview.Service.Hubs;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.UseCase;
using ITHunterview.Service.Utils;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ITHunterview.Service.Tests.UseCase;

public sealed class JobPostingsUseCasePushTopTests
{
    private sealed class PushTopTestContext : ITHunterviewContext
    {
        public PushTopTestContext(DbContextOptions<ITHunterviewContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var allowed = new HashSet<Type> { typeof(JobPostings), typeof(Cvs) };

            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                         .Where(t => !allowed.Contains(t.ClrType))
                         .Select(t => t.ClrType)
                         .Distinct()
                         .ToList())
            {
                modelBuilder.Ignore(entityType);
            }

            modelBuilder.Entity<Cvs>(entity =>
            {
                entity.HasKey(cv => cv.Id);
                entity.Ignore(cv => cv.User);
                entity.Ignore(cv => cv.TitleEmbedding);
                entity.Ignore(cv => cv.SkillsEmbedding);
                entity.Ignore(cv => cv.ExperienceEmbedding);
                entity.Ignore(cv => cv.DomainEmbedding);
            });

            modelBuilder.Entity<JobPostings>(entity =>
            {
                entity.HasKey(job => job.Id);
                entity.Ignore(job => job.ActiveAnalysisRun);
                entity.Ignore(job => job.EffectiveAnalysisRun);
                entity.Ignore(job => job.TitleEmbedding);
                entity.Ignore(job => job.SkillsEmbedding);
                entity.Ignore(job => job.ExperienceEmbedding);
                entity.Ignore(job => job.DomainEmbedding);
            });
        }
    }

    private static readonly Guid OwnerRecruiterUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid OtherRecruiterUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TargetJobId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static FeatureConsumptionExpectation QuotaExpectation() => new(
        FeatureConsumptionPaymentMethod.SUBSCRIPTION_QUOTA,
        null);

    private readonly PushTopTestContext _context;
    private readonly Mock<IJobPostingRepository> _jobPostingRepositoryMock;
    private readonly Mock<ICandidateFeatureUsageUseCase> _featureUsageUseCaseMock;
    private readonly JobPostingsUseCase _sut;

    public JobPostingsUseCasePushTopTests()
    {
        var dbOptions = new DbContextOptionsBuilder<ITHunterviewContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new PushTopTestContext(dbOptions);
        _jobPostingRepositoryMock = new Mock<IJobPostingRepository>(MockBehavior.Strict);
        _featureUsageUseCaseMock = new Mock<ICandidateFeatureUsageUseCase>(MockBehavior.Strict);

        _sut = new JobPostingsUseCase(
            _jobPostingRepositoryMock.Object,
            Mock.Of<ICompanyRepository>(),
            Mock.Of<IJobAnalysisInputBuilder>(),
            Mock.Of<IServiceScopeFactory>(),
            Mock.Of<INotificationUseCase>(),
            _featureUsageUseCaseMock.Object,
            _context,
            Mock.Of<IHubContext<NotificationHub>>(),
            Mock.Of<ILogger<JobPostingsUseCase>>()
        );
    }

    private static JobPostings CreateDefaultPublishedJob(Guid? ownerId = null, bool isBanned = false, JobStatus status = JobStatus.PUBLISHED, DateTime? pushedTopUntil = null)
    {
        return new JobPostings
        {
            Id = TargetJobId,
            RecruiterId = ownerId ?? OwnerRecruiterUserId,
            CompanyId = Guid.NewGuid(),
            Title = "Senior Fullstack Engineer",
            Description = "Great role",
            Location = "Ho Chi Minh",
            Currency = "VND",
            Status = status,
            IsBanned = isBanned,
            PublishedAt = DateTime.UtcNow.AddDays(-1),
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            PushedTopUntil = pushedTopUntil,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
    }

    [Fact]
    public async Task AUTH05_PushTopJobAsync_WhenJobMissing_ThrowsGenericNotFoundWithoutConsumptionOrUpdate()
    {
        // Mutation caught: returning success or leaking foreign job details
        _jobPostingRepositoryMock
            .Setup(r => r.GetByIdAsync(TargetJobId))
            .ReturnsAsync((JobPostings?)null);

        var action = () => _sut.PushTopJobAsync(TargetJobId, OwnerRecruiterUserId, QuotaExpectation());

        var ex = await action.Should().ThrowAsync<KeyNotFoundException>();
        ex.WithMessage("Job posting not found.");
        _featureUsageUseCaseMock.Verify(f => f.TryConsumePushTopAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<FeatureConsumptionExpectation>()), Times.Never);
        _jobPostingRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<JobPostings>()), Times.Never);
    }

    [Fact]
    public async Task AUTH05_PushTopJobAsync_WhenOwnedByAnotherRecruiter_ThrowsSameGenericNotFoundWithoutConsumptionOrUpdate()
    {
        // Mutation caught: leaking job existence with a differentiated 'not your job' message
        var foreignJob = CreateDefaultPublishedJob(ownerId: OtherRecruiterUserId);
        var originalPushedTopUntil = foreignJob.PushedTopUntil;

        _jobPostingRepositoryMock
            .Setup(r => r.GetByIdAsync(TargetJobId))
            .ReturnsAsync(foreignJob);

        var action = () => _sut.PushTopJobAsync(TargetJobId, OwnerRecruiterUserId, QuotaExpectation());

        var ex = await action.Should().ThrowAsync<KeyNotFoundException>();
        ex.WithMessage("Job posting not found.");
        foreignJob.PushedTopUntil.Should().Be(originalPushedTopUntil);
        _featureUsageUseCaseMock.Verify(f => f.TryConsumePushTopAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<FeatureConsumptionExpectation>()), Times.Never);
        _jobPostingRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<JobPostings>()), Times.Never);
    }

    [Fact]
    public async Task AUTH06_PushTopJobAsync_WhenOwnedJobIsBanned_ThrowsConflictBeforeConsumptionOrUpdate()
    {
        // Mutation caught: allowing banned jobs to consume coins or extend push top window
        var bannedJob = CreateDefaultPublishedJob(isBanned: true);
        var originalPushedTopUntil = bannedJob.PushedTopUntil;

        _jobPostingRepositoryMock
            .Setup(r => r.GetByIdAsync(TargetJobId))
            .ReturnsAsync(bannedJob);

        var action = () => _sut.PushTopJobAsync(TargetJobId, OwnerRecruiterUserId, QuotaExpectation());

        await action.Should().ThrowAsync<InvalidOperationException>();
        bannedJob.PushedTopUntil.Should().Be(originalPushedTopUntil);
        _featureUsageUseCaseMock.Verify(f => f.TryConsumePushTopAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<FeatureConsumptionExpectation>()), Times.Never);
        _jobPostingRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<JobPostings>()), Times.Never);
    }

    [Theory]
    [InlineData(JobStatus.DRAFT)]
    [InlineData(JobStatus.PENDING_REVIEW)]
    [InlineData(JobStatus.CLOSED)]
    [InlineData(JobStatus.EXPIRED)]
    public async Task AUTH07_PushTopJobAsync_WhenStatusNotPublished_ThrowsConflict(JobStatus nonPublishedStatus)
    {
        // Mutation caught: allowing non-published jobs to be pushed top
        var unpublishedJob = CreateDefaultPublishedJob(status: nonPublishedStatus);
        var originalPushedTopUntil = unpublishedJob.PushedTopUntil;

        _jobPostingRepositoryMock
            .Setup(r => r.GetByIdAsync(TargetJobId))
            .ReturnsAsync(unpublishedJob);

        var action = () => _sut.PushTopJobAsync(TargetJobId, OwnerRecruiterUserId, QuotaExpectation());

        await action.Should().ThrowAsync<InvalidOperationException>();
        unpublishedJob.PushedTopUntil.Should().Be(originalPushedTopUntil);
        _featureUsageUseCaseMock.Verify(f => f.TryConsumePushTopAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<FeatureConsumptionExpectation>()), Times.Never);
        _jobPostingRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<JobPostings>()), Times.Never);
    }

    [Fact]
    public async Task PUSH01_PushTopJobAsync_WhenInactiveOrNoWindow_BecomesNowPlus24h()
    {
        // Characterization: inactive window resets to now + 24 hours
        var job = CreateDefaultPublishedJob(pushedTopUntil: null);

        _jobPostingRepositoryMock.Setup(r => r.GetByIdAsync(TargetJobId)).ReturnsAsync(job);
        _jobPostingRepositoryMock.Setup(r => r.UpdateAsync(job)).Returns(Task.CompletedTask);
        _jobPostingRepositoryMock.Setup(r => r.GetSkillsByJobIdAsync(TargetJobId)).ReturnsAsync(new List<JobSkillRequirementDto>());
        _featureUsageUseCaseMock
            .Setup(f => f.TryConsumePushTopAsync(OwnerRecruiterUserId, TargetJobId.ToString(), It.IsAny<FeatureConsumptionExpectation>()))
            .ReturnsAsync(new FeatureConsumptionResult { ChargedCoins = 0 });

        var before = DateTime.UtcNow;
        var result = await _sut.PushTopJobAsync(TargetJobId, OwnerRecruiterUserId, QuotaExpectation());
        var after = DateTime.UtcNow;

        result.Success.Should().BeTrue();
        job.PushedTopUntil.Should().NotBeNull();
        job.PushedTopUntil!.Value.Should().BeOnOrAfter(before.AddHours(24)).And.BeOnOrBefore(after.AddHours(24));
    }

    [Fact]
    public async Task PUSH02_PushTopJobAsync_WhenActiveWindow_ExtendsFromExistingEnd()
    {
        // Characterization: active window extends 24h from previous expiration
        var futureTime = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var expectedNewTime = futureTime.AddHours(24);
        var job = CreateDefaultPublishedJob(pushedTopUntil: futureTime);

        _jobPostingRepositoryMock.Setup(r => r.GetByIdAsync(TargetJobId)).ReturnsAsync(job);
        _jobPostingRepositoryMock.Setup(r => r.UpdateAsync(job)).Returns(Task.CompletedTask);
        _jobPostingRepositoryMock.Setup(r => r.GetSkillsByJobIdAsync(TargetJobId)).ReturnsAsync(new List<JobSkillRequirementDto>());
        _featureUsageUseCaseMock
            .Setup(f => f.TryConsumePushTopAsync(OwnerRecruiterUserId, TargetJobId.ToString(), It.IsAny<FeatureConsumptionExpectation>()))
            .ReturnsAsync(new FeatureConsumptionResult { ChargedCoins = 7200 });

        var result = await _sut.PushTopJobAsync(TargetJobId, OwnerRecruiterUserId, QuotaExpectation());

        result.Success.Should().BeTrue();
        job.PushedTopUntil.Should().Be(expectedNewTime);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7200)]
    public async Task PUSH03_PushTopJobAsync_WhenSuccessful_ConsumesAndUpdatesExactlyOnce(int chargedCoins)
    {
        // Mutation caught: double-charging coins or updating job posting multiple times
        var job = CreateDefaultPublishedJob(pushedTopUntil: null);

        _jobPostingRepositoryMock.Setup(r => r.GetByIdAsync(TargetJobId)).ReturnsAsync(job);
        _jobPostingRepositoryMock.Setup(r => r.UpdateAsync(job)).Returns(Task.CompletedTask);
        _jobPostingRepositoryMock.Setup(r => r.GetSkillsByJobIdAsync(TargetJobId)).ReturnsAsync(new List<JobSkillRequirementDto>());
        var expectation = QuotaExpectation();
        _featureUsageUseCaseMock
            .Setup(f => f.TryConsumePushTopAsync(OwnerRecruiterUserId, TargetJobId.ToString(), expectation))
            .ReturnsAsync(new FeatureConsumptionResult { ChargedCoins = chargedCoins });

        var result = await _sut.PushTopJobAsync(TargetJobId, OwnerRecruiterUserId, expectation);

        result.Success.Should().BeTrue();
        _featureUsageUseCaseMock.Verify(f => f.TryConsumePushTopAsync(OwnerRecruiterUserId, TargetJobId.ToString(), expectation), Times.Once);
        _jobPostingRepositoryMock.Verify(r => r.UpdateAsync(job), Times.Once);
    }
}
