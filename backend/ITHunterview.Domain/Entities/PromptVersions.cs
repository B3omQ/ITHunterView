using System;

namespace ITHunterview.Domain.Entities
{
    public class PromptVersions
    {
        public Guid Id { get; set; }
        public Guid PromptId { get; set; }
        public string VersionTag { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? ModelConfig { get; set; } // JSONB
        public bool IsActive { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual Prompts Prompt { get; set; } = null!;
    }
}
