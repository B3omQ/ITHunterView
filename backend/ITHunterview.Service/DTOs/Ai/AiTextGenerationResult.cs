namespace ITHunterview.Service.DTOs.Ai;

public enum AiCompletionState
{
    Complete,
    OutputLimited,
    Interrupted,
    Unknown
}

public sealed record AiTextGenerationResult(
    string Text,
    AiCompletionState CompletionState,
    string? FinishReason = null,
    int? PromptTokens = null,
    int? CandidateTokens = null,
    int? ThoughtTokens = null,
    int? TotalTokens = null);
