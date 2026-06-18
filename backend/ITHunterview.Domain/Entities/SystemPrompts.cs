using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("system_prompts")]
    public class SystemPrompts
    {
        [Key]
        [Column("prompt_key")]
        public string PromptKey { get; set; }

        [Column("content")]
        public string Content { get; set; }

        [Column("version")]
        public int Version { get; set; }

        [Column("updated_by")]
        public Guid? UpdatedBy { get; set; }

    }
}
