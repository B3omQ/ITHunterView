using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Ai;

namespace ITHunterview.Service.Interface.Service
{
    public interface IAiService
    {
        Task<string> GenerateTextAsync(string prompt, string systemPrompt = null, string providerName = null, string featureCode = "GENERAL_GENERATE");
        Task<string> GenerateTextAsync(string prompt, string systemPrompt, string providerName, CancellationToken cancellationToken, string featureCode = "GENERAL_GENERATE");
        Task<string> GenerateTextAsync(string prompt, string systemPrompt, string providerName, AiGenerationOptions options, CancellationToken cancellationToken, string featureCode = "GENERAL_GENERATE");
        Task<AiTextGenerationResult> GenerateTextWithMetadataAsync(string prompt, string systemPrompt, string providerName, AiGenerationOptions options, CancellationToken cancellationToken, string featureCode = "GENERAL_GENERATE");
        Task<string> GetActiveProviderNameAsync();
    }
}
