using System.Threading.Tasks;

namespace ITHunterview.Service.Interface.Service
{
    public interface IAiService
    {
        Task<string> GenerateTextAsync(string prompt, string systemPrompt = null);
        Task<string> GetActiveProviderNameAsync();
    }
}
