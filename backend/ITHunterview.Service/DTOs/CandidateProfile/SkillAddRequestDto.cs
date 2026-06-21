using System.ComponentModel.DataAnnotations;

namespace ITHunterview.Service.DTOs.CandidateProfile
{
    public class SkillAddRequestDto
    {
        [Required]
        public int SkillId { get; set; }

        /// <summary>1–5. Null nếu không chỉ định.</summary>
        [Range(1, 5)]
        public int? ProficiencyLevel { get; set; }
    }
}
