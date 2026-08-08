using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.CandidateProfile;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.UseCase;
using Moq;
using Xunit;

namespace ITHunterview.Service.Tests.UseCase
{
    public class CandidatePublicProfileUseCaseTests
    {
        private readonly Mock<ICandidateProfileRepository> _profileRepoMock;
        private readonly Mock<ICandidateProfileUseCase> _profileUseCaseMock;
        private readonly Mock<ICandidateSkillUseCase> _skillUseCaseMock;
        private readonly Mock<ICandidateExperienceUseCase> _experienceUseCaseMock;
        private readonly Mock<ICandidateEducationUseCase> _educationUseCaseMock;
        private readonly Mock<ICandidateCertificationUseCase> _certificationUseCaseMock;

        private readonly CandidatePublicProfileUseCase _sut;

        public CandidatePublicProfileUseCaseTests()
        {
            _profileRepoMock = new Mock<ICandidateProfileRepository>();
            _profileUseCaseMock = new Mock<ICandidateProfileUseCase>();
            _skillUseCaseMock = new Mock<ICandidateSkillUseCase>();
            _experienceUseCaseMock = new Mock<ICandidateExperienceUseCase>();
            _educationUseCaseMock = new Mock<ICandidateEducationUseCase>();
            _certificationUseCaseMock = new Mock<ICandidateCertificationUseCase>();

            _sut = new CandidatePublicProfileUseCase(
                _profileRepoMock.Object,
                _profileUseCaseMock.Object,
                _skillUseCaseMock.Object,
                _experienceUseCaseMock.Object,
                _educationUseCaseMock.Object,
                _certificationUseCaseMock.Object
            );
        }

        [Fact]
        public async Task GetPublicProfileAsync_UTCID01_ProfileNull_ThrowsException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _profileRepoMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync((CandidateProfiles)null);

            // Act
            Func<Task> action = async () => await _sut.GetPublicProfileAsync(userId);

            // Assert
            await action.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Candidate profile not found.");
            
            _profileUseCaseMock.Verify(x => x.GetPersonalInfoAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetPublicProfileAsync_UTCID02_ProfileNotVisible_ThrowsException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var profile = new CandidateProfiles { IsVisibleToRecruiters = false };
            _profileRepoMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(profile);

            // Act
            Func<Task> action = async () => await _sut.GetPublicProfileAsync(userId);

            // Assert
            await action.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("This candidate's profile is not public.");
            
            _profileUseCaseMock.Verify(x => x.GetPersonalInfoAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetPublicProfileAsync_UTCID03_HappyPath_ReturnsFullProfileDto()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var profile = new CandidateProfiles { IsVisibleToRecruiters = true };
            _profileRepoMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(profile);

            var personalInfo = new PersonalInfoResponseDto { FirstName = "John" };
            var skills = new List<SkillResponseDto> { new SkillResponseDto { Name = "C#" } };
            var experiences = new List<ExperienceResponseDto> { new ExperienceResponseDto { CompanyName = "Tech" } };
            var educations = new List<EducationResponseDto> { new EducationResponseDto { InstitutionName = "Uni" } };
            var certifications = new List<CertificationResponseDto> { new CertificationResponseDto { Name = "Cert" } };

            _profileUseCaseMock.Setup(x => x.GetPersonalInfoAsync(userId)).ReturnsAsync(personalInfo);
            _skillUseCaseMock.Setup(x => x.GetSkillsAsync(userId)).ReturnsAsync((skills, 1));
            _experienceUseCaseMock.Setup(x => x.GetExperiencesAsync(userId)).ReturnsAsync(experiences);
            _educationUseCaseMock.Setup(x => x.GetEducationsAsync(userId)).ReturnsAsync(educations);
            _certificationUseCaseMock.Setup(x => x.GetCertificationsAsync(userId)).ReturnsAsync(certifications);

            // Act
            var result = await _sut.GetPublicProfileAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.PersonalInfo.Should().Be(personalInfo);
            result.Skills.Should().BeEquivalentTo(skills);
            result.Experiences.Should().BeEquivalentTo(experiences);
            result.Educations.Should().BeEquivalentTo(educations);
            result.Certifications.Should().BeEquivalentTo(certifications);

            _profileUseCaseMock.Verify(x => x.GetPersonalInfoAsync(userId), Times.Once);
            _skillUseCaseMock.Verify(x => x.GetSkillsAsync(userId), Times.Once);
            _experienceUseCaseMock.Verify(x => x.GetExperiencesAsync(userId), Times.Once);
            _educationUseCaseMock.Verify(x => x.GetEducationsAsync(userId), Times.Once);
            _certificationUseCaseMock.Verify(x => x.GetCertificationsAsync(userId), Times.Once);
        }
    }
}
