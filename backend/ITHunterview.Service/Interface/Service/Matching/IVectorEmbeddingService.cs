using System.Collections.Generic;
using System.Threading.Tasks;

namespace ITHunterview.Service.Interface.Service.Matching
{
    public interface IVectorEmbeddingService
    {
        Task<float[]> EmbedTextAsync(string text);
        Task<List<float[]>> EmbedBatchAsync(List<string> texts);
    }
}
