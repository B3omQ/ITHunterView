using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.MasterData;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.UseCase;
using Moq;
using Xunit;

namespace ITHunterview.Service.Tests.UseCase
{
    public class SkillUseCaseTests
    {
        private readonly Mock<ISkillRepository> _skillRepositoryMock;
        private readonly Mock<ISkillCategoryRepository> _skillCategoryRepositoryMock;
        private readonly SkillUseCase _sut;

        public SkillUseCaseTests()
        {
            _skillRepositoryMock = new Mock<ISkillRepository>();
            _skillCategoryRepositoryMock = new Mock<ISkillCategoryRepository>();
            _sut = new SkillUseCase(_skillRepositoryMock.Object, _skillCategoryRepositoryMock.Object);
        }

        [Fact]
        public async Task GetPagedSkillsAsync_MapsCategoryNamesCorrectly()
        {
            // Arrange
            var skills = new List<Skills>
            {
                new Skills { Id = 1, Name = "C#", CategoryId = 10, Status = SkillStatus.ACTIVE }
            };
            var categories = new List<SkillCategories>
            {
                new SkillCategories { Id = 10, Name = "Programming Languages" }
            };

            _skillRepositoryMock.Setup(r => r.GetPagedSkillsAsync(1, 10, null, null, null))
                .ReturnsAsync((skills, 1));
            _skillCategoryRepositoryMock.Setup(r => r.GetAllCategoriesAsync())
                .ReturnsAsync(categories);

            // Act
            var result = await _sut.GetPagedSkillsAsync(1, 10, null, null, null);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items[0].Name.Should().Be("C#");
            result.Data.Items[0].CategoryName.Should().Be("Programming Languages");
        }

        [Fact]
        public async Task CreateSkillAsync_WhenSkillNameExists_ReturnsErrorResponse()
        {
            // Arrange
            var dto = new CreateSkillDto { Name = "Python", CategoryId = 10 };
            _skillCategoryRepositoryMock.Setup(r => r.CategoryExistsAsync(10))
                .ReturnsAsync(true);
            _skillRepositoryMock.Setup(r => r.ExistsByNameAsync("Python", null))
                .ReturnsAsync(true);

            // Act
            var result = await _sut.CreateSkillAsync(dto, Guid.NewGuid());

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Skill name already exists in the system.");
        }

        [Fact]
        public async Task CreateSkillAsync_WhenValid_ReturnsCreatedSkill()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dto = new CreateSkillDto { Name = "TypeScript", CategoryId = 10 };
            var category = new SkillCategories { Id = 10, Name = "Frontend" };

            _skillCategoryRepositoryMock.Setup(r => r.CategoryExistsAsync(10))
                .ReturnsAsync(true);
            _skillRepositoryMock.Setup(r => r.ExistsByNameAsync("TypeScript", null))
                .ReturnsAsync(false);
            _skillCategoryRepositoryMock.Setup(r => r.GetAllCategoriesAsync())
                .ReturnsAsync(new List<SkillCategories> { category });

            // Act
            var result = await _sut.CreateSkillAsync(dto, userId);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be("TypeScript");
            _skillRepositoryMock.Verify(r => r.AddAsync(It.Is<Skills>(s => s.Name == "TypeScript")), Times.Once);
        }

        [Fact]
        public async Task DeleteSkillAsync_WhenSkillNotFound_ReturnsErrorResponse()
        {
            // Arrange
            _skillRepositoryMock.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Skills?)null);

            // Act
            var result = await _sut.DeleteSkillAsync(999);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Skill does not exist.");
        }

        [Fact]
        public async Task DeleteSkillAsync_WhenInUse_ReturnsErrorResponse()
        {
            // Arrange
            var skill = new Skills { Id = 1, Name = "In Use Framework" };

            _skillRepositoryMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(skill);
            _skillRepositoryMock.Setup(r => r.IsSkillInUseAsync(1))
                .ReturnsAsync(true);

            // Act
            var result = await _sut.DeleteSkillAsync(1);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Cannot delete this skill because there are candidates or jobs using it");
        }

        [Fact]
        public async Task DeleteSkillAsync_WhenValid_DeletesSkill()
        {
            // Arrange
            var skill = new Skills { Id = 1, Name = "Unused Skill" };

            _skillRepositoryMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(skill);
            _skillRepositoryMock.Setup(r => r.IsSkillInUseAsync(1))
                .ReturnsAsync(false);

            // Act
            var result = await _sut.DeleteSkillAsync(1);

            // Assert
            result.Success.Should().BeTrue();
            _skillRepositoryMock.Verify(r => r.DeleteAsync(skill), Times.Once);
        }
    }
}
