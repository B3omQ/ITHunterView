using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ITHunterview.Domain.Enums;

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

        [Column("status")]
        public RecruiterCvUnlockStatus Status { get; set; }

        [Column("source_scan_result_id")]
        public Guid? SourceScanResultId { get; set; }

        [Column("snapshot_storage_key")]
        public string? SnapshotStorageKey { get; set; }

        [Column("snapshot_file_name")]
        public string? SnapshotFileName { get; set; }

        [Column("snapshot_content_hash")]
        public string? SnapshotContentHash { get; set; }

        [Column("snapshot_created_at")]
        public DateTime? SnapshotCreatedAt { get; set; }

        [Column("failure_code")]
        public string? FailureCode { get; set; }
    }
}
