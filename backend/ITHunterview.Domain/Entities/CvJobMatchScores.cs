using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ITHunterview.Domain.Enums;

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

        [Column("processing_stage")]
        public string? ProcessingStage { get; set; }

        [Column("error_message")]
        public string? ErrorMessage { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("match_type")]
        public string MatchType { get; set; } = "AI";

        [Column("product_scope")]
        public CvJobMatchProductScope? ProductScope { get; set; }

        [Column("sfia_extract_result")]
        public string? SfiaExtractResult { get; set; }

        [Column("input_snapshot_json")]
        public string? InputSnapshotJson { get; set; }

        [Column("input_hash")]
        public string? InputHash { get; set; }

        [Column("idempotency_key")]
        public string? IdempotencyKey { get; set; }

        [Column("idempotency_request_hash")]
        public string? IdempotencyRequestHash { get; set; }

        [Column("attempt_count")]
        public int AttemptCount { get; set; }

        [Column("max_attempts")]
        public int MaxAttempts { get; set; } = 3;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("started_at")]
        public DateTime? StartedAt { get; set; }

        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [Column("next_attempt_at")]
        public DateTime? NextAttemptAt { get; set; }

        [Column("lease_owner")]
        public string? LeaseOwner { get; set; }

        [Column("lease_token")]
        public Guid? LeaseToken { get; set; }

        [Column("lease_expires_at")]
        public DateTime? LeaseExpiresAt { get; set; }

        [Column("last_heartbeat_at")]
        public DateTime? LastHeartbeatAt { get; set; }

        [Column("billing_reservation_id")]
        public Guid? BillingReservationId { get; set; }

        [Column("error_code")]
        public string? ErrorCode { get; set; }

        [Column("jd_analysis_quality")]
        public JdAnalysisQuality? JdAnalysisQuality { get; set; }

        [Column("jd_analysis_coverage_json", TypeName = "jsonb")]
        public string? JdAnalysisCoverageJson { get; set; }

        [Column("jd_analysis_diagnostics_json", TypeName = "jsonb")]
        public string? JdAnalysisDiagnosticsJson { get; set; }

        [Column("cv_analysis_quality")]
        public CvAnalysisQuality? CvAnalysisQuality { get; set; }

        [Column("cv_analysis_coverage_json", TypeName = "jsonb")]
        public string? CvAnalysisCoverageJson { get; set; }

        [Column("cv_analysis_diagnostics_json", TypeName = "jsonb")]
        public string? CvAnalysisDiagnosticsJson { get; set; }

        [Column("manual_retry_used")]
        public bool ManualRetryUsed { get; set; }

        [Column("retry_of_job_id")]
        public Guid? RetryOfJobId { get; set; }

        [Column("history_hidden_at")]
        public DateTime? HistoryHiddenAt { get; set; }

    }
}
