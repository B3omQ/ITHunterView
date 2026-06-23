using System;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.JobApplication
{
    public class ApplicantDto
    {
        public Guid Id { get; set; }
        public Guid CandidateId { get; set; }
        public string? CandidateName { get; set; }
        public string? Email { get; set; }
        public ApplicationStatus Status { get; set; }
        public DateTime ApplyDate { get; set; }
        public string? AvatarUrl { get; set; }
        public Guid? CvId { get; set; }
    }
}
