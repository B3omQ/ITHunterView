using System;

namespace ITHunterview.Service.DTOs.PromptAdmin
{
    public class PromptDto
    {
        public Guid Id { get; set; }
        public string PromptKey { get; set; } = null!;
        public string? Description { get; set; }
        public string? ActiveVersionTag { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        public List<PromptVersionDto> Versions { get; set; } = new List<PromptVersionDto>();
    }
}
