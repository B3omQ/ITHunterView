using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("interview_reports")]
    public class InterviewReports
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("session_id")]
        public Guid SessionId { get; set; }

        [Column("total_score")]
        public decimal? TotalScore { get; set; }

        [Column("overall_feedback")]
        public string OverallFeedback { get; set; }

    }
}
