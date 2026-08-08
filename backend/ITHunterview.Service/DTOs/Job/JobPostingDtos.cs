using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.Job
{
    public class JobPostingSummaryDto
    {
        public Guid Id { get; set; }
        public string JobCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;

        public JobStatus Status { get; set; }
        public int ApplicationCount { get; set; }
        public int ViewCount { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Level { get; set; }
        public string? WorkingModel { get; set; }
        public string? JobExpertise { get; set; }
        public System.Collections.Generic.List<string>? JobDomain { get; set; }
        public System.Collections.Generic.List<string> Skills { get; set; } = new();

        public bool IsBanned { get; set; }
        public string? BanReason { get; set; }

        public string ParseStatus { get; set; } = "PENDING";
        public string? ParseError { get; set; }
        public int AnalysisRevision { get; set; } = 1;
        public DateTime? PushedTopUntil { get; set; }
        public bool IsPushedTop => PushedTopUntil.HasValue && PushedTopUntil.Value >= DateTime.UtcNow;
    }

    public class JobPostingDetailDto
    {
        public Guid Id { get; set; }
        public string JobCode { get; set; } = string.Empty;
        public Guid RecruiterId { get; set; }
        public Guid CompanyId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public string Requirements { get; set; } = string.Empty;

        public string Benefits { get; set; } = string.Empty;
        public string IncomeText { get; set; } = string.Empty;
        public string WorkLocationText { get; set; } = string.Empty;
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
        public string Currency { get; set; } = "USD";
        public string Location { get; set; } = string.Empty;

        public JobStatus Status { get; set; }
        public int ApplicationCount { get; set; }
        public int ViewCount { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime? ApplicationDeadline { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Level { get; set; }
        public string? WorkingModel { get; set; }
        public string? JobExpertise { get; set; }
        public System.Collections.Generic.List<string>? JobDomain { get; set; }
        public System.Collections.Generic.List<JobSkillRequirementDto> Skills { get; set; } = new();
        public int AnalysisRevision { get; set; } = 1;

        public bool IsBanned { get; set; }
        public string? BanReason { get; set; }

        public string ParseStatus { get; set; } = "PENDING";
        public string? ParseError { get; set; }

        public DateTime? PushedTopUntil { get; set; }
        public bool IsPushedTop => PushedTopUntil.HasValue && PushedTopUntil.Value >= DateTime.UtcNow;
    }

    public class CreateJobPostingDto
    {
        public string JobCode { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        [StringLength(10000)]
        public string Description { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        [StringLength(10000)]
        public string Requirements { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        [StringLength(10000)]
        public string Benefits { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        [StringLength(4000)]
        public string IncomeText { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        [StringLength(4000)]
        public string WorkLocationText { get; set; } = string.Empty;

        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
        public string Currency { get; set; } = "USD";
        public string Location { get; set; } = string.Empty;

        public DateTime? ApplicationDeadline { get; set; }
        public string? Level { get; set; }
        public string? WorkingModel { get; set; }
        public string? JobExpertise { get; set; }
        public List<string>? JobDomain { get; set; }
    }

    public class UpdateJobPostingDto
    {
        public string JobCode { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        [StringLength(10000)]
        public string Description { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        [StringLength(10000)]
        public string Requirements { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        [StringLength(10000)]
        public string Benefits { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        [StringLength(4000)]
        public string IncomeText { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        [StringLength(4000)]
        public string WorkLocationText { get; set; } = string.Empty;

        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
        public string Currency { get; set; } = "USD";
        public string Location { get; set; } = string.Empty;

        public DateTime? ApplicationDeadline { get; set; }
        public string? Level { get; set; }
        public string? WorkingModel { get; set; }
        public string? JobExpertise { get; set; }
        public List<string>? JobDomain { get; set; }
    }

    public class JobSkillRequirementDto
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public bool IsMandatory { get; set; }
    }

    public class JobSkillRequirementInputDto
    {
        public int SkillId { get; set; }
        public bool IsMandatory { get; set; }
    }

    public class BanJobRequestDto
    {
        public string Reason { get; set; } = string.Empty;
    }
}
