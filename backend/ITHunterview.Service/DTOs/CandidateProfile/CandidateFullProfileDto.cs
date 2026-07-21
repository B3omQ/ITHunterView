using System.Collections.Generic;

namespace ITHunterview.Service.DTOs.CandidateProfile
{
    public class CandidateFullProfileDto
    {
        public PersonalInfoResponseDto PersonalInfo { get; set; } = new PersonalInfoResponseDto();
        public List<SkillResponseDto> Skills { get; set; } = new List<SkillResponseDto>();
        public List<ExperienceResponseDto> Experiences { get; set; } = new List<ExperienceResponseDto>();
        public List<EducationResponseDto> Educations { get; set; } = new List<EducationResponseDto>();
        public List<CertificationResponseDto> Certifications { get; set; } = new List<CertificationResponseDto>();
    }
}
