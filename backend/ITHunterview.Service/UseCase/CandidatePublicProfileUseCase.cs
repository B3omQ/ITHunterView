using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.CandidateProfile;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase
{
    public class CandidatePublicProfileUseCase : ICandidatePublicProfileUseCase
    {
        private readonly ICandidateProfileRepository _profileRepo;
        private readonly ICandidateProfileUseCase _candidateProfileUseCase;
        private readonly ICandidateSkillUseCase _skillUseCase;
        private readonly ICandidateExperienceUseCase _experienceUseCase;
        private readonly ICandidateEducationUseCase _educationUseCase;
        private readonly ICandidateCertificationUseCase _certificationUseCase;

        public CandidatePublicProfileUseCase(
            ICandidateProfileRepository profileRepo,
            ICandidateProfileUseCase candidateProfileUseCase,
            ICandidateSkillUseCase skillUseCase,
            ICandidateExperienceUseCase experienceUseCase,
            ICandidateEducationUseCase educationUseCase,
            ICandidateCertificationUseCase certificationUseCase)
        {
            _profileRepo = profileRepo;
            _candidateProfileUseCase = candidateProfileUseCase;
            _skillUseCase = skillUseCase;
            _experienceUseCase = experienceUseCase;
            _educationUseCase = educationUseCase;
            _certificationUseCase = certificationUseCase;
        }

        public async Task<CandidateFullProfileDto> GetPublicProfileAsync(Guid userId)
        {
            var profile = await _profileRepo.GetByUserIdAsync(userId);
            if (profile == null)
            {
                throw new KeyNotFoundException("Candidate profile not found.");
            }

            if (!profile.IsVisibleToRecruiters)
            {
                throw new UnauthorizedAccessException("This candidate's profile is not public.");
            }

            var personalInfo = await _candidateProfileUseCase.GetPersonalInfoAsync(userId);
            var skillsResult = await _skillUseCase.GetSkillsAsync(userId);
            var experiences = await _experienceUseCase.GetExperiencesAsync(userId);
            var educations = await _educationUseCase.GetEducationsAsync(userId);
            var certifications = await _certificationUseCase.GetCertificationsAsync(userId);

            return new CandidateFullProfileDto
            {
                PersonalInfo = personalInfo,
                Skills = skillsResult.Skills ?? new List<SkillResponseDto>(),
                Experiences = experiences ?? new List<ExperienceResponseDto>(),
                Educations = educations ?? new List<EducationResponseDto>(),
                Certifications = certifications ?? new List<CertificationResponseDto>()
            };
        }
    }
}
