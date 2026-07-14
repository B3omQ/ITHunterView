using System.ComponentModel.DataAnnotations;

namespace ITHunterview.Service.DTOs.LearningPath
{
    public class GeneratePathRequestDto
    {
        // 1. Core Information
        [Required]
        public string TargetRole { get; set; }
        
        [Required]
        public string SpecificGoal { get; set; }
        
        [Required]
        public string ExperienceLevel { get; set; }
        
        public int TimeframeInWeeks { get; set; } = 12;
        
        public int HoursPerWeek { get; set; } = 10;

        // 2. Technical Information
        [Required]
        public string CurrentSkills { get; set; }

        public string TargetCompanyType { get; set; }
        
        public string Strengths { get; set; }
        
        public string Weaknesses { get; set; }

        // 3. Personalization
        public string LearningStyle { get; set; }
        
        public string AdditionalPreferences { get; set; }
    }
}
