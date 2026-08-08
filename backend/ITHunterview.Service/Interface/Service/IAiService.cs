using System.Threading;
using System.Threading.Tasks;

namespace ITHunterview.Service.Interface.Service
{
    public interface IAiService
    {
        Task<string> GenerateTextAsync(string prompt, string systemPrompt = null, string providerName = null);
        Task<string> GenerateTextAsync(string prompt, string systemPrompt, string providerName, CancellationToken cancellationToken);
        Task<string> GenerateTextAsync(string prompt, string systemPrompt, string providerName, AiGenerationOptions options, CancellationToken cancellationToken);
        Task<string> GetActiveProviderNameAsync();
    }
}
