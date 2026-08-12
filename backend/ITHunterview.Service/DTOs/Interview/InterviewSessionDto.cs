using System;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.Interview
{
    public class InterviewSessionDto
    {
        public Guid Id { get; set; }
        public Guid CandidateId { get; set; }
        public Guid? JobId { get; set; }
        public string? JobTitle { get; set; }
        public Guid? CvId { get; set; }
        public string? CvFileName { get; set; }
        public DifficultyLevel DifficultyLevel { get; set; }
        public InterviewSessionStatus Status { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string? AiProvider { get; set; }
        public string? Language { get; set; }
        public string? Title { get; set; }
    }
}
