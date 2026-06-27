using System;
using System.Collections.Generic;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.JobSearch
{
    public class JobSearchQueryDto
    {
        public string? Keyword { get; set; }
        public string? Location { get; set; }

        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
        public string? Skill { get; set; }
        public string? CompanyName { get; set; }
        public int? PostedWithinDays { get; set; }
        public JobStatus? Status { get; set; }
        public List<string>? Levels { get; set; }
        public List<string>? WorkingModels { get; set; }
        public List<string>? JobDomains { get; set; }
        public List<string>? CompanyIndustries { get; set; }
        public List<string>? CompanyTypes { get; set; }
        public List<string>? JobExpertises { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
