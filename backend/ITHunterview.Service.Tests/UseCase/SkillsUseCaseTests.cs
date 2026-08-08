using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Skill;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.UseCase;
using Moq;
using Xunit;

namespace ITHunterview.Service.Tests.UseCase
{
    public class SkillsUseCaseTests
    {
        private readonly Mock<ISkillRepository> _skillRepositoryMock;
        private readonly SkillsUseCase _sut;

        public SkillsUseCaseTests()
        {
            _skillRepositoryMock = new Mock<ISkillRepository>();
            _sut = new SkillsUseCase(_skillRepositoryMock.Object);
        }

        [Fact]
        public async Task GetActiveSkillsAsync_ReturnsMappedSkillsList()
        {
            // Arrange
            var list = new List<(Skills Skill, string CategoryName)>
            {
                (new Skills { Id = 1, Name = "React", CategoryId = 5 }, "Frontend"),
                (new Skills { Id = 2, Name = "PostgreSQL", CategoryId = 6 }, "Database")
            };

            _skillRepositoryMock.Setup(r => r.GetActiveSkillsWithCategoryAsync())
                .ReturnsAsync(list);

            // Act
            var result = await _sut.GetActiveSkillsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            result.Data![0].Name.Should().Be("React");
            result.Data[0].CategoryName.Should().Be("Frontend");
            result.Data[1].Name.Should().Be("PostgreSQL");
            result.Data[1].CategoryName.Should().Be("Database");
        }
    }
}
