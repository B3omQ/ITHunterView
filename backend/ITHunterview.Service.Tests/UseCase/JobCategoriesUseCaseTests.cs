using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Job;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.UseCase;
using Moq;
using Xunit;

namespace ITHunterview.Service.Tests.UseCase
{
    public class JobCategoriesUseCaseTests
    {
        private readonly Mock<IJobCategoryRepository> _jobCategoryRepositoryMock;
        private readonly JobCategoriesUseCase _sut;

        public JobCategoriesUseCaseTests()
        {
            _jobCategoryRepositoryMock = new Mock<IJobCategoryRepository>();
            _sut = new JobCategoriesUseCase(_jobCategoryRepositoryMock.Object);
        }

        [Fact]
        public async Task GetCategoriesAsync_ReturnsMappedCategories()
        {
            // Arrange
            var categories = new List<JobCategories>
            {
                new JobCategories { Id = 1, Name = "Software Development", ParentId = null },
                new JobCategories { Id = 2, Name = "Web Development", ParentId = 1 }
            };

            _jobCategoryRepositoryMock.Setup(r => r.GetCategoriesAsync())
                .ReturnsAsync(categories);

            // Act
            var result = await _sut.GetCategoriesAsync();

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            result.Data![0].Name.Should().Be("Software Development");
            result.Data[1].ParentId.Should().Be(1);
        }
    }
}
