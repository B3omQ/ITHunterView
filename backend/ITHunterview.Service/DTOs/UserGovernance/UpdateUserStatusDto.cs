using System.ComponentModel.DataAnnotations;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.UserGovernance
{
    public class UpdateUserStatusDto
    {
        [Required(ErrorMessage = "Trạng thái người dùng là bắt buộc")]
        public UserStatus Status { get; set; }

        [Required(ErrorMessage = "Lý do cập nhật là bắt buộc để lưu nhật ký Audit Logs")]
        [MinLength(5, ErrorMessage = "Lý do phải có ít nhất 5 ký tự")]
        public string Reason { get; set; } = string.Empty;
    }
}
