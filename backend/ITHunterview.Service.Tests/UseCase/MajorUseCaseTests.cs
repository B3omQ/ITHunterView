using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.MasterData;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.UseCase;
using Moq;
using Xunit;

namespace ITHunterview.Service.Tests.UseCase
{
    public class MajorUseCaseTests
    {
        private readonly Mock<IMajorRepository> _majorRepositoryMock;
        private readonly MajorUseCase _sut;

        public MajorUseCaseTests()
        {
            _majorRepositoryMock = new Mock<IMajorRepository>();
            _sut = new MajorUseCase(_majorRepositoryMock.Object);
        }

        [Fact]
        public async Task GetPagedMajorsAsync_ReturnsPagedList()
        {
            // Arrange
            var majors = new List<Majors>
            {
                new Majors { Id = 1, Name = "Computer Science", Code = "CS" }
            };
            _majorRepositoryMock.Setup(r => r.GetPagedMajorsAsync(1, 10, null))
                .ReturnsAsync((majors, 1));

            // Act
            var result = await _sut.GetPagedMajorsAsync(1, 10, null);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items[0].Name.Should().Be("Computer Science");
        }

        [Fact]
        public async Task CreateMajorAsync_WhenCodeExists_ReturnsErrorResponse()
        {
            // Arrange
            var dto = new CreateMajorDto { Name = "Software Engineering", Code = "SE" };
            _majorRepositoryMock.Setup(r => r.ExistsByCodeAsync("SE", null))
                .ReturnsAsync(true);

            // Act
            var result = await _sut.CreateMajorAsync(dto, Guid.NewGuid());

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Major code already exists.");
        }

        [Fact]
        public async Task CreateMajorAsync_WhenParentExceedsMaxDepth_ReturnsErrorResponse()
        {
            // Arrange
            var dto = new CreateMajorDto { Name = "Deep SubMajor", Code = "DSM", ParentId = 3 };
            var parent = new Majors { Id = 3, ParentId = 2 };
            var grandParent = new Majors { Id = 2, ParentId = 1 };

            var allMajors = new List<Majors>
            {
                new Majors { Id = 1, ParentId = null },
                new Majors { Id = 2, ParentId = 1 },
                new Majors { Id = 3, ParentId = 2 }
            };

            _majorRepositoryMock.Setup(r => r.ExistsByCodeAsync("DSM", null))
                .ReturnsAsync(false);
            _majorRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => allMajors.FirstOrDefault(m => m.Id == id));
            _majorRepositoryMock.Setup(r => r.GetAllActiveMajorsAsync())
                .ReturnsAsync(allMajors);

            // Act
            var result = await _sut.CreateMajorAsync(dto, Guid.NewGuid());

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Exceeds maximum hierarchy depth");
        }

        [Fact]
        public async Task CreateMajorAsync_WhenValid_ReturnsCreatedMajor()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dto = new CreateMajorDto { Name = "Information Technology", Code = "IT" };

            _majorRepositoryMock.Setup(r => r.ExistsByCodeAsync("IT", null))
                .ReturnsAsync(false);

            // Act
            var result = await _sut.CreateMajorAsync(dto, userId);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Code.Should().Be("IT");
            _majorRepositoryMock.Verify(r => r.AddAsync(It.Is<Majors>(m => m.Code == "IT")), Times.Once);
        }

        [Fact]
        public async Task DeleteMajorAsync_WhenMajorNotFound_ReturnsErrorResponse()
        {
            // Arrange
            _majorRepositoryMock.Setup(r => r.GetByIdAsync(99))
                .ReturnsAsync((Majors?)null);

            // Act
            var result = await _sut.DeleteMajorAsync(99, Guid.NewGuid());

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Major does not exist.");
        }

        [Fact]
        public async Task DeleteMajorAsync_WhenHasChildren_ReturnsErrorResponse()
        {
            // Arrange
            var major = new Majors { Id = 1, Name = "Root Major" };
            _majorRepositoryMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(major);
            _majorRepositoryMock.Setup(r => r.HasChildrenAsync(1))
                .ReturnsAsync(true);

            // Act
            var result = await _sut.DeleteMajorAsync(1, Guid.NewGuid());

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Cannot delete this major because it contains sub-majors (children).");
        }
    }
}
