using System;
using System.ComponentModel.DataAnnotations;

namespace ITHunterview.Service.DTOs.PromptAdmin;

public sealed class ActivateJdAnalysisPromptPairDto
{
    [Required]
    public Guid SystemVersionId { get; set; }

    [Required]
    public Guid UserVersionId { get; set; }
}
