namespace ITHunterview.Service.Exceptions;

/// <summary>
/// Signals that a provider stopped a response at its output-token boundary.
/// Carries bounded diagnostics only; prompt and response content are excluded.
/// </summary>
public sealed class AiProviderOutputTruncatedException : InvalidOperationException
{
    public AiProviderOutputTruncatedException(
        string finishReason,
        int? promptTokenCount,
        int? candidateTokenCount,
        int? thoughtsTokenCount,
        int? totalTokenCount,
        int answerPartCount,
        int responseLength)
        : base("AI_OUTPUT_TRUNCATED")
    {
        FinishReason = finishReason;
        PromptTokenCount = promptTokenCount;
        CandidateTokenCount = candidateTokenCount;
        ThoughtsTokenCount = thoughtsTokenCount;
        TotalTokenCount = totalTokenCount;
        AnswerPartCount = answerPartCount;
        ResponseLength = responseLength;
    }

    public string FinishReason { get; }
    public int? PromptTokenCount { get; }
    public int? CandidateTokenCount { get; }
    public int? ThoughtsTokenCount { get; }
    public int? TotalTokenCount { get; }
    public int AnswerPartCount { get; }
    public int ResponseLength { get; }
}
