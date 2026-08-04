using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.JobApplication;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.UseCase;
using Moq;
using Xunit;

namespace ITHunterview.Service.Tests.UseCase
{
    public class JobApplicationUseCaseTests
    {
        private readonly Mock<IJobApplicationRepository> _jobApplicationRepositoryMock;
        private readonly Mock<ICandidateProfileRepository> _candidateProfileRepositoryMock;
        private readonly Mock<IJobPostingRepository> _jobPostingRepositoryMock;
        private readonly Mock<INotificationUseCase> _notificationUseCaseMock;
        private readonly JobApplicationUseCase _sut;

        public JobApplicationUseCaseTests()
        {
            _jobApplicationRepositoryMock = new Mock<IJobApplicationRepository>();
            _candidateProfileRepositoryMock = new Mock<ICandidateProfileRepository>();
            _jobPostingRepositoryMock = new Mock<IJobPostingRepository>();
            _notificationUseCaseMock = new Mock<INotificationUseCase>();

            _sut = new JobApplicationUseCase(
                _jobApplicationRepositoryMock.Object,
                _candidateProfileRepositoryMock.Object,
                _jobPostingRepositoryMock.Object,
                _notificationUseCaseMock.Object);
        }

        [Fact]
        public async Task ApplyForJobAsync_WhenCandidateProfileNotFound_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new CreateJobApplicationRequestDto { JobId = Guid.NewGuid() };
            _candidateProfileRepositoryMock.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync((CandidateProfiles?)null);

            // Act
            Func<Task> act = async () => await _sut.ApplyForJobAsync(userId, request);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Only candidates can apply for jobs.");
        }

        [Fact]
        public async Task ApplyForJobAsync_WhenAlreadyApplied_ThrowsInvalidOperationException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var candidateId = Guid.NewGuid();
            var jobId = Guid.NewGuid();
            var request = new CreateJobApplicationRequestDto { JobId = jobId };

            var profile = new CandidateProfiles { Id = candidateId, UserId = userId };
            _candidateProfileRepositoryMock.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(profile);
            _jobApplicationRepositoryMock.Setup(r => r.HasCandidateAppliedAsync(candidateId, jobId))
                .ReturnsAsync(true);

            // Act
            Func<Task> act = async () => await _sut.ApplyForJobAsync(userId, request);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("You have already applied for this job.");
        }

        [Fact]
        public async Task ApplyForJobAsync_WhenValidRequest_CreatesApplicationAndNotifiesRecruiter()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var candidateId = Guid.NewGuid();
            var jobId = Guid.NewGuid();
            var recruiterId = Guid.NewGuid();
            var request = new CreateJobApplicationRequestDto
            {
                JobId = jobId,
                CoverLetter = "I am interested in this position.",
                CvId = Guid.NewGuid()
            };

            var profile = new CandidateProfiles { Id = candidateId, UserId = userId, FirstName = "John", LastName = "Candidate" };
            var job = new JobPostings { Id = jobId, Title = "Software Engineer", RecruiterId = recruiterId };

            _candidateProfileRepositoryMock.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(profile);
            _jobApplicationRepositoryMock.Setup(r => r.HasCandidateAppliedAsync(candidateId, jobId))
                .ReturnsAsync(false);
            _jobPostingRepositoryMock.Setup(r => r.GetByIdAsync(jobId))
                .ReturnsAsync(job);

            // Act
            var result = await _sut.ApplyForJobAsync(userId, request);

            // Assert
            result.Should().BeTrue();
            _jobApplicationRepositoryMock.Verify(r => r.CreateAsync(It.Is<JobApplications>(a =>
                a.CandidateId == candidateId &&
                a.JobId == jobId &&
                a.Status == ApplicationStatus.APPLIED)), Times.Once);
        }

        [Fact]
        public async Task GetApplicantsByJobIdAsync_ReturnsPagedApplicants()
        {
            // Arrange
            var jobId = Guid.NewGuid();
            var expectedList = new List<ApplicantDto>
            {
                new ApplicantDto { Id = Guid.NewGuid(), CandidateName = "Alice" }
            };
            var expectedPaged = new PagedResult<ApplicantDto>
            {
                Items = expectedList,
                TotalCount = 1,
                Page = 1,
                PageSize = 10
            };

            _jobApplicationRepositoryMock.Setup(r => r.GetApplicantsByJobIdAsync(jobId, 1, 10))
                .ReturnsAsync(expectedPaged);

            // Act
            var result = await _sut.GetApplicantsByJobIdAsync(jobId, 1, 10);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.Items[0].CandidateName.Should().Be("Alice");
        }
    }
}
