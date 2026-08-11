using System.ComponentModel.DataAnnotations;

namespace ITHunterview.Service.DTOs.UserGovernance
{
    public class CreateStaffDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email format.")]
        [MaxLength(100, ErrorMessage = "Email must not exceed 100 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        [MaxLength(50, ErrorMessage = "Password must not exceed 50 characters.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#^()_+\-=\[\]{}|;:,.<>/]).{8,}$", ErrorMessage = "Password must be at least 8 characters long and contain at least 1 uppercase letter, 1 digit, and 1 special character.")]
        public string Password { get; set; } = string.Empty;

        [MaxLength(100, ErrorMessage = "Full Name must not exceed 100 characters.")]
        public string? FullName { get; set; }

        [MaxLength(20, ErrorMessage = "Phone number must not exceed 20 characters.")]
        public string? Phone { get; set; }
    }
}
