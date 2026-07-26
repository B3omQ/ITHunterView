using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.JobAnalysis
{
    public sealed class AnalyzeJobRequestDto
    {
        [Range(0, int.MaxValue)]
        public int ExpectedRevision { get; set; }

        [StringLength(100)]
        public string? IdempotencyKey { get; set; }
    }

    public sealed class UpdateJobSkillDecisionsDto
    {
        public int ExpectedJobRevision { get; set; }
        public int ExpectedDecisionVersion { get; set; }

        [MinLength(1)]
        public List<JobSkillDecisionInputDto> Decisions { get; set; } = new();
    }

    public sealed class JobSkillDecisionInputDto
    {
        public Guid DecisionId { get; set; }
        public SkillDecisionStatus Decision { get; set; }
        public int? ResolvedSkillId { get; set; }
        public string Importance { get; set; } = string.Empty;
    }

    public sealed class FinalizeJobRequestDto
    {
        public Guid AnalysisRunId { get; set; }
        public int ExpectedJobRevision { get; set; }
        public int ExpectedDecisionVersion { get; set; }
    }

    public sealed class JobAnalysisStatusDto
    {
        public Guid JobId { get; set; }
        public Guid AnalysisRunId { get; set; }
        public int InputRevision { get; set; }
        public JobAnalysisStatus Status { get; set; }
        public string? FailureCode { get; set; }
        public string? Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public sealed class JobAnalysisPreviewDto
    {
        public Guid JobId { get; set; }
        public Guid AnalysisRunId { get; set; }
        public int InputRevision { get; set; }
        public JobAnalysisStatus Status { get; set; }
        public int DecisionVersion { get; set; }
        public string? FailureCode { get; set; }
        public List<JobSkillDecisionDto> Suggestions { get; set; } = new();
        public List<OtherRequirementDto> OtherRequirements { get; set; } = new();
        public bool CanFinalize { get; set; }
        public List<string> BlockingReasons { get; set; } = new();
        public string FinalActionLabel { get; set; } = "Publish";
        public string FinalTargetStatus { get; set; } = "PUBLISHED";
    }

    public sealed class JobSkillDecisionDto
    {
        public Guid Id { get; set; }
        public string RawMention { get; set; } = string.Empty;
        public string NormalizedMention { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Importance { get; set; } = string.Empty;
        public string SourceSection { get; set; } = string.Empty;
        public string EvidenceText { get; set; } = string.Empty;
        public int? SuggestedSkillId { get; set; }
        public string? SuggestedSkillName { get; set; }
        public int? ResolvedSkillId { get; set; }
        public string? ResolvedSkillName { get; set; }
        public SkillResolutionStatus ResolutionStatus { get; set; }
        public SkillDecisionStatus DecisionStatus { get; set; }
        public decimal? Confidence { get; set; }
    }

    public sealed class OtherRequirementDto
    {
        public string Category { get; set; } = string.Empty;
        public string Importance { get; set; } = string.Empty;
        public string SkillName { get; set; } = string.Empty;
        public string DetailVerbatim { get; set; } = string.Empty;
        public string Evidence { get; set; } = string.Empty;
    }

    public sealed class FinalizeJobResponseDto
    {
        public Guid JobId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? PublishedAt { get; set; }
        public int SkillCount { get; set; }
    }
}
