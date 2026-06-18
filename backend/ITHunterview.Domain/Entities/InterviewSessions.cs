using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("interview_sessions")]
    public class InterviewSessions
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("candidate_id")]
        public Guid CandidateId { get; set; }

        [Column("job_id")]
        public Guid? JobId { get; set; }

        [Column("cv_id")]
        public Guid? CvId { get; set; }

        [Column("difficulty_level")]
        public DifficultyLevel DifficultyLevel { get; set; }

        [Column("status")]
        public InterviewSessionStatus Status { get; set; }

        [Column("started_at")]
        public DateTime? StartedAt { get; set; }

        [Column("ended_at")]
        public DateTime? EndedAt { get; set; }

    }
}
