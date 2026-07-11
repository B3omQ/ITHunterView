using System;
using System.Text.Json;

namespace ITHunterview.Service.DTOs.CvOptimizer
{
    public class CvOptimizationResponseDto
    {
        public Guid Id { get; set; }
        public Guid CandidateId { get; set; }
        public Guid CvId { get; set; }
        public string? TargetJdText { get; set; }
        public JsonDocument FeedbackData { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
