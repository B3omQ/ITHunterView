using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("cv_optimizations")]
    public class CvOptimizations
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("candidate_id")]
        public Guid CandidateId { get; set; }

        [Column("cv_id")]
        public Guid CvId { get; set; }

        [Column("target_jd_text")]
        public string? TargetJdText { get; set; }

        [Column("feedback_data")]
        public string FeedbackData { get; set; } // Stores the JSON string

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public Cvs Cv { get; set; } = null!;
        public User Candidate { get; set; } = null!;
    }
}
