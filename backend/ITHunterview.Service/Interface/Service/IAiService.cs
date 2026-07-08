using System.Threading.Tasks;

namespace ITHunterview.Service.Interface.Service
{
    public interface IAiService
    {
        Task<string> GenerateTextAsync(string prompt, string systemPrompt = null, string providerName = null);
        Task<string> GetActiveProviderNameAsync();
    }
}
