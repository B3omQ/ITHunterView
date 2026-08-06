namespace ITHunterview.Service.DTOs.PromptAdmin;

public sealed class JdAnalysisPromptPairDto
{
    public PromptDto SystemPrompt { get; set; } = new();
    public PromptDto UserPrompt { get; set; } = new();
}
