using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.CandidateProfile;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.UseCase;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace ITHunterview.Service.Tests.UseCase
{
    public class CandidateSkillUseCaseTests
    {
        private readonly Mock<ICandidateSkillRepository> _skillRepoMock;
        private readonly CandidateSkillUseCase _sut;

        public CandidateSkillUseCaseTests()
        {
            _skillRepoMock = new Mock<ICandidateSkillRepository>();
            _sut = new CandidateSkillUseCase(_skillRepoMock.Object);
        }

        [Fact]
        public async Task AddSkillAsync_UTCID01_MasterSkillNotFound_ThrowsException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new SkillAddRequestDto { SkillId = 99, ProficiencyLevel = 3 };

            _skillRepoMock.Setup(x => x.GetMasterSkillByIdAsync(request.SkillId)).ReturnsAsync((Skills)null);

            // Act
            Func<Task> action = async () => await _sut.AddSkillAsync(userId, request);

            // Assert
            await action.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*không tồn tại trong hệ thống*");
            
            _skillRepoMock.Verify(x => x.GetUserSkillAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
            _skillRepoMock.Verify(x => x.AddAsync(It.IsAny<UserSkills>()), Times.Never);
        }

        [Fact]
        public async Task AddSkillAsync_UTCID02_UserSkillExists_ThrowsException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new SkillAddRequestDto { SkillId = 1, ProficiencyLevel = 3 };
            
            var masterSkill = new Skills { Id = 1, Name = "C#" };
            _skillRepoMock.Setup(x => x.GetMasterSkillByIdAsync(request.SkillId)).ReturnsAsync(masterSkill);

            var existingSkill = new UserSkills { UserId = userId, SkillId = 1 };
            _skillRepoMock.Setup(x => x.GetUserSkillAsync(userId, request.SkillId)).ReturnsAsync(existingSkill);

            // Act
            Func<Task> action = async () => await _sut.AddSkillAsync(userId, request);

            // Assert
            await action.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*đã thêm skill*vào profile rồi*");
            
            _skillRepoMock.Verify(x => x.AddAsync(It.IsAny<UserSkills>()), Times.Never);
        }

        [Fact]
        public async Task AddSkillAsync_UTCID03_HappyPath_ReturnsSkillResponseDto()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new SkillAddRequestDto { SkillId = 1, ProficiencyLevel = 3 };
            
            var masterSkill = new Skills { Id = 1, Name = "C#" };
            _skillRepoMock.Setup(x => x.GetMasterSkillByIdAsync(request.SkillId)).ReturnsAsync(masterSkill);

            _skillRepoMock.Setup(x => x.GetUserSkillAsync(userId, request.SkillId)).ReturnsAsync((UserSkills)null);

            // Act
            var result = await _sut.AddSkillAsync(userId, request);

            // Assert
            result.Should().NotBeNull();
            result.SkillId.Should().Be(1);
            result.Name.Should().Be("C#");
            result.ProficiencyLevel.Should().Be(3);

            _skillRepoMock.Verify(x => x.AddAsync(It.Is<UserSkills>(us => 
                us.UserId == userId && 
                us.SkillId == 1 && 
                us.ProficiencyLevel == 3
            )), Times.Once);
        }
    }
}
