using System;
using System.Text.Json;

namespace ITHunterview.Service.DTOs.LearningPath
{
    public class LearningPathResponseDto
    {
        public Guid Id { get; set; }
        public Guid CandidateId { get; set; }
        public Guid? SessionId { get; set; }
        public JsonDocument PathData { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
