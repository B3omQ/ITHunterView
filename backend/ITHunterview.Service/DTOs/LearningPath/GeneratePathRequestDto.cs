using System.ComponentModel.DataAnnotations;

namespace ITHunterview.Service.DTOs.LearningPath
{
    public class GeneratePathRequestDto
    {
        [Required]
        public string TargetRole { get; set; }
        
        [Required]
        public string CurrentSkills { get; set; }

        [Required]
        public string TargetSkills { get; set; }

        public int TimeframeInWeeks { get; set; } = 12;
    }
}
