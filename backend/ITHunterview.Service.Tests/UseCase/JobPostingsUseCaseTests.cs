using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ITHunterview.Service.Tests.UseCase
{
    public class JobPostingsUseCaseTests : IDisposable
    {
        private sealed class TestContext : ITHunterviewContext
        {
            public TestContext(DbContextOptions<ITHunterviewContext> options) : base(options) { }

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

        private readonly Mock<IJobPostingRepository> _jobPostingRepositoryMock;
        private readonly Mock<ICompanyRepository> _companyRepositoryMock;
        private readonly Mock<IJobAnalysisInputBuilder> _inputBuilderMock;
        private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
        private readonly Mock<INotificationUseCase> _notificationUseCaseMock;
        private readonly Mock<ICandidateFeatureUsageUseCase> _featureUsageUseCaseMock;
        private readonly Mock<IHubContext<NotificationHub>> _hubContextMock;
        private readonly Mock<ILogger<JobPostingsUseCase>> _loggerMock;
        private readonly TestContext _context;
        private readonly JobPostingsUseCase _sut;

        public JobPostingsUseCaseTests()
        {
            _jobPostingRepositoryMock = new Mock<IJobPostingRepository>();
            _companyRepositoryMock = new Mock<ICompanyRepository>();
            _inputBuilderMock = new Mock<IJobAnalysisInputBuilder>();
            _scopeFactoryMock = new Mock<IServiceScopeFactory>();
            _notificationUseCaseMock = new Mock<INotificationUseCase>();
            _featureUsageUseCaseMock = new Mock<ICandidateFeatureUsageUseCase>();
            _hubContextMock = new Mock<IHubContext<NotificationHub>>();
            _loggerMock = new Mock<ILogger<JobPostingsUseCase>>();

            var options = new DbContextOptionsBuilder<ITHunterviewContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new TestContext(options);

            _sut = new JobPostingsUseCase(
                _jobPostingRepositoryMock.Object,
                _companyRepositoryMock.Object,
                _inputBuilderMock.Object,
                _scopeFactoryMock.Object,
                _notificationUseCaseMock.Object,
                _featureUsageUseCaseMock.Object,
                _context,
                _hubContextMock.Object,
                _loggerMock.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task GetJobsAsync_ReturnsPagedJobs()
        {
            // Arrange
            var jobId = Guid.NewGuid();
            var expectedJobs = new List<JobPostings>
            {
                new JobPostings { Id = jobId, Title = "Senior .NET Developer", Status = JobStatus.PUBLISHED }
            };

            _jobPostingRepositoryMock.Setup(r => r.GetPagedAsync("net", JobStatus.PUBLISHED, 1, 7, null))
                .ReturnsAsync((expectedJobs, 1));
            _jobPostingRepositoryMock.Setup(r => r.GetSkillsForJobsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new Dictionary<Guid, List<string>> { [jobId] = new List<string> { ".NET" } });

            // Act
            var result = await _sut.GetJobsAsync("net", JobStatus.PUBLISHED, 1, 7);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items[0].Title.Should().Be("Senior .NET Developer");
        }

        [Fact]
        public async Task CreateJobAsync_WhenCompanyNotFound_ReturnsError()
        {
            // Arrange
            var recruiterId = Guid.NewGuid();
            var dto = new CreateJobPostingDto { Title = "Test Job" };
            _jobPostingRepositoryMock.Setup(c => c.GetRecruiterCompanyIdAsync(recruiterId))
                .ReturnsAsync((Guid?)null);

            // Act
            var result = await _sut.CreateJobAsync(dto, recruiterId);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Recruiter company not found");
        }

        [Fact]
        public async Task CloseJobAsync_WhenRecruiterIsOwner_UpdatesStatusToClosed()
        {
            // Arrange
            var recruiterId = Guid.NewGuid();
            var jobId = Guid.NewGuid();
            var job = new JobPostings
            {
                Id = jobId,
                RecruiterId = recruiterId,
                Title = "QA Lead",
                Status = JobStatus.PUBLISHED
            };
            _jobPostingRepositoryMock.Setup(r => r.GetByIdAsync(jobId))
                .ReturnsAsync(job);

            // Act
            var result = await _sut.CloseJobAsync(jobId, recruiterId);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().BeTrue();
            job.Status.Should().Be(JobStatus.CLOSED);
            _jobPostingRepositoryMock.Verify(r => r.UpdateAsync(job), Times.Once);
        }
    }
}
