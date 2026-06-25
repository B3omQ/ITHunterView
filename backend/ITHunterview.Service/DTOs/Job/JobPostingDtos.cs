using System;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.Job
{
    public class JobPostingSummaryDto
    {
        public Guid Id { get; set; }
        public string JobCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public JobType JobType { get; set; }
        public JobStatus Status { get; set; }
        public int ApplicationCount { get; set; }
        public int ViewCount { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class JobPostingDetailDto
    {
        public Guid Id { get; set; }
        public string JobCode { get; set; } = string.Empty;
        public Guid RecruiterId { get; set; }
        public Guid CompanyId { get; set; }
        public int? CategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Responsibilities { get; set; } = string.Empty;
        public string Requirements { get; set; } = string.Empty;
        public string Benefits { get; set; } = string.Empty;
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
        public string Currency { get; set; } = "USD";
        public string Location { get; set; } = string.Empty;
        public JobType JobType { get; set; }
        public JobStatus Status { get; set; }
        public int ApplicationCount { get; set; }
        public int ViewCount { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Level { get; set; }
        public string? WorkingModel { get; set; }
        public string? JobDomain { get; set; }
        public System.Collections.Generic.List<JobSkillRequirementDto> Skills { get; set; } = new();
    }

    public class CreateJobPostingDto
    {
        public string JobCode { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Responsibilities { get; set; } = string.Empty;
        public string Requirements { get; set; } = string.Empty;
        public string Benefits { get; set; } = string.Empty;
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
        public string Currency { get; set; } = "USD";
        public string Location { get; set; } = string.Empty;
        public JobType JobType { get; set; }
        public JobStatus Status { get; set; } = JobStatus.DRAFT;
        public string? Level { get; set; }
        public string? WorkingModel { get; set; }
        public string? JobDomain { get; set; }
        public System.Collections.Generic.List<JobSkillRequirementInputDto>? Skills { get; set; }
    }

    public class UpdateJobPostingDto
    {
        public string JobCode { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Responsibilities { get; set; } = string.Empty;
        public string Requirements { get; set; } = string.Empty;
        public string Benefits { get; set; } = string.Empty;
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
        public string Currency { get; set; } = "USD";
        public string Location { get; set; } = string.Empty;
        public JobType JobType { get; set; }
        public JobStatus Status { get; set; }
        public string? Level { get; set; }
        public string? WorkingModel { get; set; }
        public string? JobDomain { get; set; }
        public System.Collections.Generic.List<JobSkillRequirementInputDto>? Skills { get; set; }
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
}
