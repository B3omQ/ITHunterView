using System.ComponentModel.DataAnnotations;

namespace ITHunterview.Service.DTOs.JobAnalysis;

public sealed class JdAnalysisTestRequestDto
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(20000, MinimumLength = 1)]
    public string JdText { get; set; } = string.Empty;
}
