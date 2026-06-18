using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("ai_api_usage_logs")]
    public class AiApiUsageLogs
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("reference_id")]
        public Guid? ReferenceId { get; set; }

        [Column("prompt_tokens")]
        public int? PromptTokens { get; set; }

        [Column("completion_tokens")]
        public int? CompletionTokens { get; set; }

        [Column("model_name")]
        public string ModelName { get; set; }

        [Column("cost_usd")]
        public decimal? CostUsd { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

    }
}
