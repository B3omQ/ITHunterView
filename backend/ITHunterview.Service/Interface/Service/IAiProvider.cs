using System.Threading;
using System.Threading.Tasks;

namespace ITHunterview.Service.Interface.Service
{
    public interface IAiProvider
    {
        string ProviderName { get; }
        Task<string> GenerateTextAsync(string prompt, string systemPrompt = null);
        Task<string> GenerateTextAsync(string prompt, string systemPrompt, CancellationToken cancellationToken);
        Task<string> GenerateTextAsync(
            string prompt,
            string systemPrompt,
            AiGenerationOptions options,
            CancellationToken cancellationToken)
            => GenerateTextAsync(prompt, systemPrompt, cancellationToken);
    }
}
