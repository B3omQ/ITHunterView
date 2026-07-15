using System.ComponentModel.DataAnnotations;

namespace ITHunterview.Service.DTOs.LearningPath
{
    public class GeneratePathRequestDto
    {
        // 1. Core Information
        [Required]
        public Guid TargetRoleTemplateId { get; set; }
        
        [Required]
        public string SpecificGoal { get; set; }
        
        [Required]
        public string ExperienceLevel { get; set; }

        // 2. Technical Information
        [Required]
        public List<CandidateSfiaSkillDto> CurrentSkills { get; set; } = new List<CandidateSfiaSkillDto>();

        // 3. Personalization
        public string LearningStyle { get; set; }
        
        public string AdditionalPreferences { get; set; }
    }

    public class CandidateSfiaSkillDto
    {
        [Required]
        public string SkillCode { get; set; }

        [Required]
        [Range(1, 7)]
        public int CurrentLevel { get; set; }
    }
}
