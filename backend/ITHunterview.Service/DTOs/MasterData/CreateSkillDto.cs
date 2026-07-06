using System.ComponentModel.DataAnnotations;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.MasterData
{
    public class CreateSkillDto
    {
        [Required(ErrorMessage = "Skill name cannot be empty.")]
        [MaxLength(255, ErrorMessage = "Skill name cannot exceed 255 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Skill category cannot be empty.")]
        public int CategoryId { get; set; }

        public SkillStatus Status { get; set; } = SkillStatus.ACTIVE;
    }
}
