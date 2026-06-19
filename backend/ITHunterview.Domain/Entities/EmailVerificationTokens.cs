using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("email_verification_tokens")]
    public class EmailVerificationTokens
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("token")]
        public string Token { get; set; } = string.Empty;

        [Column("expires_at")]
        public DateTime ExpiresAt { get; set; }

        [Column("is_used")]
        public bool IsUsed { get; set; } = false;

        // Navigation
        public User User { get; set; } = null!;
    }
}
