using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Domain.Entities
{
    [Table("job_skill_decisions")]
    public class JobSkillDecisions
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("job_analysis_run_id")]
        public Guid JobAnalysisRunId { get; set; }

        [Column("raw_mention")]
        public string RawMention { get; set; } = string.Empty;

        [Column("normalized_mention")]
        public string NormalizedMention { get; set; } = string.Empty;

        [Column("category")]
        public string Category { get; set; } = string.Empty;

        [Column("importance")]
        public string Importance { get; set; } = string.Empty;

        [Column("source_section")]
        public string SourceSection { get; set; } = string.Empty;

        [Column("evidence_text")]
        public string EvidenceText { get; set; } = string.Empty;

        [Column("suggested_skill_id")]
        public int? SuggestedSkillId { get; set; }

        [Column("resolved_skill_id")]
        public int? ResolvedSkillId { get; set; }

        [Column("resolution_status")]
        public SkillResolutionStatus ResolutionStatus { get; set; }

        [Column("decision_status")]
        public SkillDecisionStatus DecisionStatus { get; set; }

        [Column("confidence")]
        public decimal? Confidence { get; set; }

        [Column("decision_version")]
        public int DecisionVersion { get; set; }

        [Column("decided_by")]
        public Guid? DecidedBy { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey(nameof(JobAnalysisRunId))]
        public virtual JobAnalysisRuns? JobAnalysisRun { get; set; }

        [ForeignKey(nameof(SuggestedSkillId))]
        public virtual Skills? SuggestedSkill { get; set; }

        [ForeignKey(nameof(ResolvedSkillId))]
        public virtual Skills? ResolvedSkill { get; set; }
    }
}
