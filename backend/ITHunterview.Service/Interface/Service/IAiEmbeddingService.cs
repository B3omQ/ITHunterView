using System.Threading.Tasks;

namespace ITHunterview.Service.Interface.Service
{
    public interface IAiEmbeddingService
    {
        Task<float[]> GenerateEmbeddingAsync(string text);
    }
}
