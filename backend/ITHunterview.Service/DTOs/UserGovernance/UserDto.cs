using System;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.UserGovernance
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public int? RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public UserStatus Status { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DeactiveAt { get; set; }
    }
}
