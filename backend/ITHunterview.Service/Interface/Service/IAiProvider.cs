using System.Threading.Tasks;

namespace ITHunterview.Service.Interface.Service
{
    public interface IAiProvider
    {
        string ProviderName { get; }
        Task<string> GenerateTextAsync(string prompt, string systemPrompt = null);
    }
}
