using System;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.JobApplication
{
    public class CandidateAppliedJobDto
    {
        public Guid Id { get; set; }
        public Guid JobId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string? CompanyLogoUrl { get; set; }
        public ApplicationStatus Status { get; set; }
        public DateTime ApplyDate { get; set; }
    }
}
