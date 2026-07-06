using System.ComponentModel.DataAnnotations;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.UserGovernance
{
    public class UpdateUserStatusDto
    {
        [Required(ErrorMessage = "User status is required.")]
        public UserStatus Status { get; set; }

        [Required(ErrorMessage = "Update reason is required for audit logs.")]
        [MinLength(5, ErrorMessage = "Reason must be at least 5 characters.")]
        public string Reason { get; set; } = string.Empty;
    }
}
