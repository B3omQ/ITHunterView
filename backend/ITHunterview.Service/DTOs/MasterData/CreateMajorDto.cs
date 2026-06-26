using System.ComponentModel.DataAnnotations;

namespace ITHunterview.Service.DTOs.MasterData
{
    public class CreateMajorDto
    {
        [Required(ErrorMessage = "Major name cannot be empty.")]
        [MaxLength(255, ErrorMessage = "Major name cannot exceed 255 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Major code cannot be empty.")]
        [MaxLength(50, ErrorMessage = "Major code cannot exceed 50 characters.")]
        public string Code { get; set; } = string.Empty;

        public int? ParentId { get; set; }
    }
}
