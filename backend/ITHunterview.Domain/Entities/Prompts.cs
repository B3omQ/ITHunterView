using System;
using System.Collections.Generic;

namespace ITHunterview.Domain.Entities
{
    public class Prompts
    {
        public Guid Id { get; set; }
        public string PromptKey { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        public virtual ICollection<PromptVersions> Versions { get; set; } = new List<PromptVersions>();
    }
}
