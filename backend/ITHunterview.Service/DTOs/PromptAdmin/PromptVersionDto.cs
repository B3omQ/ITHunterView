using System;

namespace ITHunterview.Service.DTOs.PromptAdmin
{
    public class PromptVersionDto
    {
        public Guid Id { get; set; }
        public Guid PromptId { get; set; }
        public string VersionTag { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? ModelConfig { get; set; }
        public bool IsActive { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
