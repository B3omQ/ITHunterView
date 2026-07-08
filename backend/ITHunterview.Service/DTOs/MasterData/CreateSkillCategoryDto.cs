using System.ComponentModel.DataAnnotations;

namespace ITHunterview.Service.DTOs.MasterData
{
    public class CreateSkillCategoryDto
    {
        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(255, ErrorMessage = "Category name cannot exceed 255 characters.")]
        public string Name { get; set; } = null!;
    }
}
