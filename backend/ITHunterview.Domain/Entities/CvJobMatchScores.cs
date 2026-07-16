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
        public Guid? CvId { get; set; }

        [Column("cv_file_name")]
        public string? CvFileName { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("job_id")]
        public Guid? JobId { get; set; }

        [Column("raw_jd_text")]
        public string? RawJdText { get; set; }

        [Column("jd_title")]
        public string? JdTitle { get; set; }

        [Column("match_score")]
        public decimal? MatchScore { get; set; }

        [Column("match_details")]
        public string MatchDetails { get; set; } = string.Empty;

        [Column("status")]
        public string Status { get; set; } = "Pending";

        [Column("error_message")]
        public string? ErrorMessage { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("match_type")]
        public string MatchType { get; set; } = "AI";

        [Column("sfia_extract_result")]
        public string? SfiaExtractResult { get; set; }

    }
}
