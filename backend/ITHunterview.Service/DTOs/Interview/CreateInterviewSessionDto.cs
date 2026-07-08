using System;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.Interview
{
    public class CreateInterviewSessionDto
    {
        public DifficultyLevel DifficultyLevel { get; set; } = DifficultyLevel.MEDIUM;
        public Guid? JobId { get; set; }
        public Guid? CvId { get; set; }
        public string? AiProvider { get; set; } = "Gemini";
    }
}
