namespace ITHunterview.Service.DTOs.PromptAdmin
{
    public class CvAnalysisPromptPairDto
    {
        public PromptDto SystemPrompt { get; set; } = new();
        public PromptDto UserPrompt { get; set; } = new();
    }
}
