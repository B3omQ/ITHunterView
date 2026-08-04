using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.JobSearch;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.UseCase;
using Moq;
using Xunit;

namespace ITHunterview.Service.Tests.UseCase
{
    public class CandidateJobUseCaseTests
    {
        private readonly Mock<IJobSearchRepository> _jobSearchRepositoryMock;
        private readonly Mock<IUserSavedJobRepository> _userSavedJobRepositoryMock;
        private readonly Mock<IJobPostingRepository> _jobPostingsRepositoryMock;
        private readonly CandidateJobUseCase _sut;

        public CandidateJobUseCaseTests()
        {
            _jobSearchRepositoryMock = new Mock<IJobSearchRepository>();
            _userSavedJobRepositoryMock = new Mock<IUserSavedJobRepository>();
            _jobPostingsRepositoryMock = new Mock<IJobPostingRepository>();

            _sut = new CandidateJobUseCase(
                _jobSearchRepositoryMock.Object,
                _userSavedJobRepositoryMock.Object,
                _jobPostingsRepositoryMock.Object);
        }

        [Fact]
        public async Task SearchJobsAsync_ForcesPublishedStatusAndReturnsResults()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = new JobSearchQueryDto { Keyword = "Developer" };
            var expectedResponse = new PaginatedDataResponse<JobCardDto>
            {
                Data = new List<JobCardDto> { new JobCardDto { Id = Guid.NewGuid(), Title = "Developer" } },
                Meta = new PaginationMeta { TotalItems = 1, TotalPages = 1 }
            };

            _jobSearchRepositoryMock.Setup(r => r.SearchJobsAsync(It.Is<JobSearchQueryDto>(q => q.Status == JobStatus.PUBLISHED), userId))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _sut.SearchJobsAsync(query, userId);

            // Assert
            result.Should().NotBeNull();
            result.Meta.TotalItems.Should().Be(1);
            result.Data.First().Title.Should().Be("Developer");
        }

        [Fact]
        public async Task SaveJobAsync_WhenJobDoesNotExist_ThrowsArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var jobId = Guid.NewGuid();

            _jobPostingsRepositoryMock.Setup(r => r.GetByIdAsync(jobId))
                .ReturnsAsync((JobPostings?)null);

            // Act
            Func<Task> act = async () => await _sut.SaveJobAsync(userId, jobId);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Job does not exist.");
        }

        [Fact]
        public async Task SaveJobAsync_WhenAlreadySaved_ThrowsInvalidOperationException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var jobId = Guid.NewGuid();
            var job = new JobPostings { Id = jobId, Title = "FrontEnd Dev" };

            _jobPostingsRepositoryMock.Setup(r => r.GetByIdAsync(jobId))
                .ReturnsAsync(job);
            _userSavedJobRepositoryMock.Setup(r => r.ExistsAsync(userId, jobId))
                .ReturnsAsync(true);

            // Act
            Func<Task> act = async () => await _sut.SaveJobAsync(userId, jobId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Job is already saved.");
        }

        [Fact]
        public async Task SaveJobAsync_WhenValid_SavesJob()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var jobId = Guid.NewGuid();
            var job = new JobPostings { Id = jobId, Title = "FrontEnd Dev" };

            _jobPostingsRepositoryMock.Setup(r => r.GetByIdAsync(jobId))
                .ReturnsAsync(job);
            _userSavedJobRepositoryMock.Setup(r => r.ExistsAsync(userId, jobId))
                .ReturnsAsync(false);

            // Act
            await _sut.SaveJobAsync(userId, jobId);

            // Assert
            _userSavedJobRepositoryMock.Verify(r => r.AddAsync(It.Is<UserSavedJobs>(s =>
                s.UserId == userId && s.JobId == jobId)), Times.Once);
        }

        [Fact]
        public async Task UnsaveJobAsync_RemovesSavedJob()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var jobId = Guid.NewGuid();
            _userSavedJobRepositoryMock.Setup(r => r.ExistsAsync(userId, jobId))
                .ReturnsAsync(true);

            // Act
            await _sut.UnsaveJobAsync(userId, jobId);

            // Assert
            _userSavedJobRepositoryMock.Verify(r => r.DeleteAsync(userId, jobId), Times.Once);
        }
    }
}
