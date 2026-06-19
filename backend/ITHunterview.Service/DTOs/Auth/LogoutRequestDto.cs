using System.ComponentModel.DataAnnotations;

namespace ITHunterview.Service.DTOs.Auth
{
    public class LogoutRequestDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
