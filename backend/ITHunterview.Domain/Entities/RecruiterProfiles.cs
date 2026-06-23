using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("recruiter_profiles")]
    public class RecruiterProfiles
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("company_id")]
        public Guid? CompanyId { get; set; }

        [Column("full_name")]
        public string? FullName { get; set; }

        [Column("phone")]
        public string? Phone { get; set; }

        [Column("position_title")]
        public string? PositionTitle { get; set; }

        [Column("avatar_url")]
        public string? AvatarUrl { get; set; }

        // Navigation
        public User User { get; set; } = null!;

        [ForeignKey("CompanyId")]
        public Companies? Company { get; set; }
    }
}
