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
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
