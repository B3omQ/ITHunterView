using System;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.JobApplication
{
    public class JobApplicationDetailDto
    {
        public Guid Id { get; set; }
        public Guid CandidateId { get; set; }
        public string? CandidateName { get; set; }
        public string? Email { get; set; }
        public ApplicationStatus Status { get; set; }
        public DateTime ApplyDate { get; set; }
        public string? AvatarUrl { get; set; }
        public string? CoverLetter { get; set; }
        public Guid? CvId { get; set; }
        public string? CvUrl { get; set; }
        public string? CvFileName { get; set; }
    }
}
