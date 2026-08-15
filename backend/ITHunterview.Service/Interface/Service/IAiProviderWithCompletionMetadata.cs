using ITHunterview.Service.DTOs.Ai;

namespace ITHunterview.Service.Interface.Service;

public interface IAiProviderWithCompletionMetadata
{
    Task<AiTextGenerationResult> GenerateTextWithMetadataAsync(
        string prompt,
        string systemPrompt,
        AiGenerationOptions? options,
        CancellationToken cancellationToken);
}
