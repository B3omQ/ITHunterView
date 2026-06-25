using System;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.JobSearch
{
    public class JobSearchQueryDto
    {
        public string? Keyword { get; set; }
        public string? Location { get; set; }
        public JobType? JobType { get; set; }
        public int? CategoryId { get; set; }
        public decimal? MinSalary { get; set; }
        public string? Currency { get; set; }
        public string? Skill { get; set; }
        public string? CompanyName { get; set; }
        public int? PostedWithinDays { get; set; }
        public JobStatus? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
