using System.ComponentModel.DataAnnotations;

namespace ITHunterview.Service.DTOs.Auth
{
    public class ForgotPasswordRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
