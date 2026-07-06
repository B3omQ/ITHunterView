using System.ComponentModel.DataAnnotations;

namespace ITHunterview.Service.DTOs.MasterData
{
    public class UpdateMajorDto
    {
        [Required(ErrorMessage = "Major name cannot be empty.")]
        [MinLength(3, ErrorMessage = "Major name must be at least 3 characters.")]
        [MaxLength(255, ErrorMessage = "Major name cannot exceed 255 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Major code cannot be empty.")]
        [MinLength(2, ErrorMessage = "Major code must be at least 2 characters.")]
        [MaxLength(50, ErrorMessage = "Major code cannot exceed 50 characters.")]
        [RegularExpression(@"^[A-Za-z0-9\-_]+$", ErrorMessage = "Major code can only contain letters, numbers, hyphens, and underscores.")]
        public string Code { get; set; } = string.Empty;

        public int? ParentId { get; set; }
    }
}

