using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("cv_job_match_scores")]
    public class CvJobMatchScores
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("cv_id")]
        public Guid CvId { get; set; }

        [Column("job_id")]
        public Guid JobId { get; set; }

        [Column("raw_jd_text")]
        public string RawJdText { get; set; }

        [Column("match_score")]
        public decimal? MatchScore { get; set; }

        [Column("match_details")]
        public string MatchDetails { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

    }
}
