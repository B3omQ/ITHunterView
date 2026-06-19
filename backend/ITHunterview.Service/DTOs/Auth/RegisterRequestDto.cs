using System.ComponentModel.DataAnnotations;

namespace ITHunterview.Service.DTOs.Auth
{
    public class RegisterRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        /// <summary>
        /// "candidate" or "recruiter"
        /// </summary>
        [Required]
        public string RoleType { get; set; } = "candidate";
    }
}
