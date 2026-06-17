using System;

namespace ITHunterview.Domain.Entities
{
    public class RefreshToken : BaseEntity
    {
        public string Token { get; set; } = string.Empty;
        public string JwtId { get; set; } = string.Empty;
        public bool IsUsed { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime ExpiryDate { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
