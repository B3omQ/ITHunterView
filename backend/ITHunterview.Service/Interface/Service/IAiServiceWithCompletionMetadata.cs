using ITHunterview.Service.DTOs.Ai;

namespace ITHunterview.Service.Interface.Service;

public interface IAiServiceWithCompletionMetadata
{
    Task<AiTextGenerationResult> GenerateTextWithMetadataAsync(
        string prompt,
        string systemPrompt,
        string providerName,
        AiGenerationOptions? options,
        CancellationToken cancellationToken,
        string featureCode = "GENERAL_GENERATE");
}
