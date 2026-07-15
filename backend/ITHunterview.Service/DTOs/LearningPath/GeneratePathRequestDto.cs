using System.ComponentModel.DataAnnotations;

namespace ITHunterview.Service.DTOs.LearningPath
{
    public class GeneratePathRequestDto
    {
        // 1. Core Information
        [Required]
        public Guid TargetRoleTemplateId { get; set; }

        // 2. Technical Information
        [Required]
        public List<CandidateSfiaSkillDto> CurrentSkills { get; set; } = new List<CandidateSfiaSkillDto>();
    }

    public class CandidateSfiaSkillDto
    {
        [Required]
        public string SkillCode { get; set; }

        [Required]
        [Range(0, 7)]
        public int CurrentLevel { get; set; }
    }
}
