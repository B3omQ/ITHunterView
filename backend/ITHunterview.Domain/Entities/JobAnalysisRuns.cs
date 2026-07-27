using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Domain.Entities
{
    [Table("job_analysis_runs")]
    public class JobAnalysisRuns
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("job_id")]
        public Guid JobId { get; set; }

        [Column("input_revision")]
        public int InputRevision { get; set; }

        [Column("input_hash")]
        public string InputHash { get; set; } = string.Empty;

        [Column("status")]
        public JobAnalysisStatus Status { get; set; }

        [Column("system_prompt_version_id")]
        public Guid SystemPromptVersionId { get; set; }

        [Column("user_prompt_version_id")]
        public Guid UserPromptVersionId { get; set; }

        [Column("schema_version")]
        public string SchemaVersion { get; set; } = "jd-analysis/v2";

        [Column("raw_input_snapshot", TypeName = "jsonb")]
        public string RawInputSnapshot { get; set; } = string.Empty;

        [Column("raw_analysis_json", TypeName = "jsonb")]
        public string? RawAnalysisJson { get; set; }

        [Column("effective_analysis_json", TypeName = "jsonb")]
        public string? EffectiveAnalysisJson { get; set; }

        [Column("validation_errors_json", TypeName = "jsonb")]
        public string? ValidationErrorsJson { get; set; }

        [Column("provider_name")]
        public string? ProviderName { get; set; }

        [Column("model_name")]
        public string? ModelName { get; set; }

        [Column("requested_by")]
        public Guid RequestedBy { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("started_at")]
        public DateTime? StartedAt { get; set; }

        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [Column("attempt_number")]
        public int AttemptNumber { get; set; } = 1;

        [Column("idempotency_key")]
        public string? IdempotencyKey { get; set; }

        [Column("decision_version")]
        public int DecisionVersion { get; set; } = 0;

        [Column("lease_expires_at")]
        public DateTime? LeaseExpiresAt { get; set; }

        [Column("last_heartbeat_at")]
        public DateTime? LastHeartbeatAt { get; set; }

        [Column("provider_call_started_at")]
        public DateTime? ProviderCallStartedAt { get; set; }

        [Column("failure_code")]
        public string? FailureCode { get; set; }

        // Navigation properties
        [ForeignKey(nameof(JobId))]
        public virtual JobPostings? Job { get; set; }

        [ForeignKey(nameof(SystemPromptVersionId))]
        public virtual PromptVersions? SystemPromptVersion { get; set; }

        [ForeignKey(nameof(UserPromptVersionId))]
        public virtual PromptVersions? UserPromptVersion { get; set; }
    }
}
