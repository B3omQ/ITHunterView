using System.ComponentModel.DataAnnotations;

namespace ITHunterview.Service.DTOs.PromptAdmin
{
    public class CreatePromptVersionDto
    {
        [Required(ErrorMessage = "Version Tag is required")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Version Tag must be between 1 and 50 characters")]
        public string VersionTag { get; set; } = null!;

        [Required(ErrorMessage = "Content is required")]
        public string Content { get; set; } = null!;

        public string? ModelConfig { get; set; }

        public bool MakeActive { get; set; }
    }
}
