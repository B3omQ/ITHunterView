using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.UseCase;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ITHunterview.Service.Tests.UseCase
{
    public class CandidateProfileUseCaseTests
    {
        private readonly Mock<ICandidateProfileRepository> _profileRepoMock;
        private readonly Mock<ICandidateExperienceRepository> _expRepoMock;
        private readonly Mock<ICandidateEducationRepository> _eduRepoMock;
        private readonly Mock<ICandidateCertificationRepository> _certRepoMock;
        private readonly Mock<ICandidateSkillRepository> _skillRepoMock;
        private readonly Mock<IFileUploadService> _fileUploadServiceMock;
        private readonly Mock<ICvRepository> _cvRepositoryMock;
        private readonly Mock<ICvUseCase> _cvUseCaseMock;
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<IWalletUseCase> _walletUseCaseMock;

        private readonly CandidateProfileUseCase _sut;

        public CandidateProfileUseCaseTests()
        {
            _profileRepoMock = new Mock<ICandidateProfileRepository>();
            _expRepoMock = new Mock<ICandidateExperienceRepository>();
            _eduRepoMock = new Mock<ICandidateEducationRepository>();
            _certRepoMock = new Mock<ICandidateCertificationRepository>();
            _skillRepoMock = new Mock<ICandidateSkillRepository>();
            _fileUploadServiceMock = new Mock<IFileUploadService>();
            _cvRepositoryMock = new Mock<ICvRepository>();
            _cvUseCaseMock = new Mock<ICvUseCase>();
            _userRepoMock = new Mock<IUserRepository>();
            _walletUseCaseMock = new Mock<IWalletUseCase>();

            _sut = new CandidateProfileUseCase(
                _profileRepoMock.Object,
                _expRepoMock.Object,
                _eduRepoMock.Object,
                _certRepoMock.Object,
                _skillRepoMock.Object,
                _fileUploadServiceMock.Object,
                _cvRepositoryMock.Object,
                _cvUseCaseMock.Object,
                _userRepoMock.Object,
                _walletUseCaseMock.Object
            );
        }

        private void SetupProfile(Guid userId, bool initialVisibility = false)
        {
            var profile = new CandidateProfiles 
            { 
                UserId = userId, 
                IsVisibleToRecruiters = initialVisibility,
                User = new User { Id = userId }
            };
            _profileRepoMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(profile);
            _profileRepoMock.Setup(x => x.CreateAsync(It.IsAny<CandidateProfiles>())).ReturnsAsync(profile);
        }

        [Fact]
        public async Task SetVisibilityAsync_UTCID01_IsVisibleFalse_UpdatesProfile()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupProfile(userId, true);

            // Act
            var result = await _sut.SetVisibilityAsync(userId, false);

            // Assert
            result.Should().BeFalse();
            _profileRepoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
            _cvRepositoryMock.Verify(x => x.HasPrimaryCvAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task SetVisibilityAsync_UTCID02_IsVisibleTrue_NoPrimaryCv_ThrowsException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupProfile(userId, false);

            _cvRepositoryMock.Setup(x => x.HasPrimaryCvAsync(userId)).ReturnsAsync(false);

            // Act
            Func<Task> action = async () => await _sut.SetVisibilityAsync(userId, true);

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*thiết lập CV Chính*");
            
            _profileRepoMock.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task SetVisibilityAsync_UTCID04_IsVisibleTrue_PrimaryCvNotPending_DoesNotParse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupProfile(userId, false);

            _cvRepositoryMock.Setup(x => x.HasPrimaryCvAsync(userId)).ReturnsAsync(true);
            
            var cvs = new List<Cvs> { new Cvs { IsPrimary = true, ParseStatus = "COMPLETED" } };
            _cvRepositoryMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(cvs);

            // Act
            var result = await _sut.SetVisibilityAsync(userId, true);

            // Assert
            result.Should().BeTrue();
            _profileRepoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
            _cvRepositoryMock.Verify(x => x.TryLockCvForParsingAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task SetVisibilityAsync_UTCID05_IsVisibleTrue_LockFails_DoesNotParse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var cvId = Guid.NewGuid();
            SetupProfile(userId, false);

            _cvRepositoryMock.Setup(x => x.HasPrimaryCvAsync(userId)).ReturnsAsync(true);
            
            var cvs = new List<Cvs> { new Cvs { Id = cvId, IsPrimary = true, ParseStatus = "PENDING" } };
            _cvRepositoryMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(cvs);
            
            _cvRepositoryMock.Setup(x => x.TryLockCvForParsingAsync(cvId)).ReturnsAsync(false);

            // Act
            var result = await _sut.SetVisibilityAsync(userId, true);

            // Assert
            result.Should().BeTrue();
            _profileRepoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
            _cvRepositoryMock.Verify(x => x.TryLockCvForParsingAsync(cvId), Times.Once);
        }

        [Fact]
        public async Task SetVisibilityAsync_UTCID06_IsVisibleTrue_HappyPath_TriggersBackgroundParse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var cvId = Guid.NewGuid();
            SetupProfile(userId, false);

            _cvRepositoryMock.Setup(x => x.HasPrimaryCvAsync(userId)).ReturnsAsync(true);
            
            var cvs = new List<Cvs> { new Cvs { Id = cvId, IsPrimary = true, ParseStatus = "PENDING" } };
            _cvRepositoryMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(cvs);
            
            _cvRepositoryMock.Setup(x => x.TryLockCvForParsingAsync(cvId)).ReturnsAsync(true);

            // Act
            var result = await _sut.SetVisibilityAsync(userId, true);

            // Assert
            result.Should().BeTrue();
            _profileRepoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
            _cvRepositoryMock.Verify(x => x.TryLockCvForParsingAsync(cvId), Times.Once);
        }

        [Fact]
        public async Task SetVisibilityAsync_UTCID03_IsVisibleTrue_PrimaryCvNull_DoesNotParse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupProfile(userId, false);

            // Mặc dù cờ báo là có CV chính...
            _cvRepositoryMock.Setup(x => x.HasPrimaryCvAsync(userId)).ReturnsAsync(true);
            
            // ...nhưng List lại rỗng
            var cvs = new List<Cvs>(); 
            _cvRepositoryMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(cvs);

            // Act
            var result = await _sut.SetVisibilityAsync(userId, true);

            // Assert
            result.Should().BeTrue();
            _profileRepoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
            _cvRepositoryMock.Verify(x => x.TryLockCvForParsingAsync(It.IsAny<Guid>()), Times.Never);
        }
    }
}
