using System.ComponentModel.DataAnnotations;

namespace ITHunterview.Service.DTOs.LearningPath
{
    public class GeneratePathRequestDto
    {
        // 1. Core Information (Manual Mode)
        public Guid? TargetRoleTemplateId { get; set; }

        // 1. Core Information (Custom AI Mode)
        public string? CustomTargetRoleName { get; set; }
        public List<CustomSfiaSkillDto>? CustomTargetSkills { get; set; }

        // 2. Technical Information (Used only in Manual Mode)
        public List<CandidateSfiaSkillDto> CurrentSkills { get; set; } = new List<CandidateSfiaSkillDto>();

        // 3. Additional Context
        public string? PersonalContext { get; set; }
    }

    public class CandidateSfiaSkillDto
    {
        [Required]
        public string SkillCode { get; set; }

        [Required]
        [Range(0, 7)]
        public int CurrentLevel { get; set; }
    }

    public class CustomSfiaSkillDto
    {
        [Required]
        public string SkillCode { get; set; }

        [Required]
        [Range(1, 7)]
        public int TargetLevel { get; set; }

        [Required]
        [Range(0, 7)]
        public int CurrentLevel { get; set; }
    }
}
