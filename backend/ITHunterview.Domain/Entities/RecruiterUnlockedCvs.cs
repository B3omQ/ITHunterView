using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("recruiter_unlocked_cvs")]
    public class RecruiterUnlockedCvs
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("recruiter_id")]
        public Guid RecruiterId { get; set; }

        [Column("cv_id")]
        public Guid CvId { get; set; }

        [Column("job_id")]
        public Guid? JobId { get; set; }

        [Column("coins_spent")]
        public int CoinsSpent { get; set; }

        [Column("unlocked_via")]
        public string UnlockedVia { get; set; } = "COINS"; // "COINS" or "SUBSCRIPTION"

        [Column("unlocked_at")]
        public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;
    }
}
