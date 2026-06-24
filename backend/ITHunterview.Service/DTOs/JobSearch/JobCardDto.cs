using System;
using System.Collections.Generic;

namespace ITHunterview.Service.DTOs.JobSearch
{
    public class JobCardDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string CompanyName { get; set; }
        public string LogoUrl { get; set; }
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
        public string Currency { get; set; }
        public string Location { get; set; }
        public string JobType { get; set; }
        public DateTime? PublishedAt { get; set; }
        public bool? IsSaved { get; set; }
        public List<string> Skills { get; set; } = new();
    }
}
