using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.JobSearch;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.UseCase;
using Moq;
using Xunit;

namespace ITHunterview.Service.Tests.UseCase
{
    public class PublicJobUseCaseTests
    {
        private readonly Mock<IJobSearchRepository> _jobSearchRepositoryMock;
        private readonly PublicJobUseCase _sut;

        public PublicJobUseCaseTests()
        {
            _jobSearchRepositoryMock = new Mock<IJobSearchRepository>();
            _sut = new PublicJobUseCase(_jobSearchRepositoryMock.Object);
        }

        [Fact]
        public async Task SearchJobsAsync_ForcesPublishedStatus_ReturnsPaginatedData()
        {
            // Arrange
            var query = new JobSearchQueryDto { Keyword = "DevOps" };
            var expectedResponse = new PaginatedDataResponse<JobCardDto>
            {
                Data = new List<JobCardDto> { new JobCardDto { Id = Guid.NewGuid(), Title = "DevOps Engineer" } },
                Meta = new PaginationMeta { TotalItems = 1, TotalPages = 1 }
            };

            _jobSearchRepositoryMock.Setup(r => r.SearchJobsAsync(It.Is<JobSearchQueryDto>(q => q.Status == JobStatus.PUBLISHED)))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _sut.SearchJobsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Meta.TotalItems.Should().Be(1);
            result.Data.First().Title.Should().Be("DevOps Engineer");
        }

        [Fact]
        public async Task GetJobDetailAsync_WhenJobNotFound_ReturnsErrorResponse()
        {
            // Arrange
            var jobId = Guid.NewGuid();
            _jobSearchRepositoryMock.Setup(r => r.GetJobDetailAsync(jobId))
                .ReturnsAsync((JobDetailViewDto?)null);

            // Act
            var result = await _sut.GetJobDetailAsync(jobId);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Job not found or not published.");
        }

        [Fact]
        public async Task GetJobDetailAsync_WhenJobExists_ReturnsSuccessResponse()
        {
            // Arrange
            var jobId = Guid.NewGuid();
            var jobDetail = new JobDetailViewDto { Id = jobId, Title = "Tech Lead" };

            _jobSearchRepositoryMock.Setup(r => r.GetJobDetailAsync(jobId))
                .ReturnsAsync(jobDetail);

            // Act
            var result = await _sut.GetJobDetailAsync(jobId);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Title.Should().Be("Tech Lead");
        }

        [Fact]
        public async Task GetFeaturedTopJobsAsync_ReturnsTopJobsList()
        {
            // Arrange
            var jobs = new List<JobCardDto>
            {
                new JobCardDto { Id = Guid.NewGuid(), Title = "Top Job 1" },
                new JobCardDto { Id = Guid.NewGuid(), Title = "Top Job 2" }
            };

            _jobSearchRepositoryMock.Setup(r => r.GetFeaturedTopJobsAsync(6))
                .ReturnsAsync(jobs);

            // Act
            var result = await _sut.GetFeaturedTopJobsAsync(6);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().HaveCount(2);
        }
    }
}
