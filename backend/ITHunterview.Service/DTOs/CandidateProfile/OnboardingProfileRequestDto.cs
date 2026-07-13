using System.ComponentModel.DataAnnotations;

namespace ITHunterview.Service.DTOs.CandidateProfile
{
    public class OnboardingProfileRequestDto
    {
        [Required(ErrorMessage = "Tên không được để trống")]
        [StringLength(50, ErrorMessage = "Tên không được vượt quá 50 ký tự")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ không được để trống")]
        [StringLength(50, ErrorMessage = "Họ không được vượt quá 50 ký tự")]
        public string LastName { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string? Location { get; set; }
    }
}
