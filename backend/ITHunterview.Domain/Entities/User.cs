using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Domain.Entities
{
    [Table("users")]
    public class User : BaseEntity
    {
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("role_id")]
        public int? RoleId { get; set; }

        [Column("status")]
        public UserStatus Status { get; set; }

        [Column("deactive_at")]
        public DateTime? DeactiveAt { get; set; }

        // Navigation properties
        public Roles? Role { get; set; }
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public CandidateProfiles? CandidateProfile { get; set; }
        public RecruiterProfiles? RecruiterProfile { get; set; }
    }
}
