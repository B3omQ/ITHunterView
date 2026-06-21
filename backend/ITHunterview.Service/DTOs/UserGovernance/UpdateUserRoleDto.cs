using System.ComponentModel.DataAnnotations;

namespace ITHunterview.Service.DTOs.UserGovernance
{
    public class UpdateUserRoleDto
    {
        [Required(ErrorMessage = "Mã vai trò (RoleId) là bắt buộc")]
        public int RoleId { get; set; }
    }
}
