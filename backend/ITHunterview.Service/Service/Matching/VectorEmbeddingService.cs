using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Service.Interface.Service.Matching;
using Microsoft.Extensions.Logging;

namespace ITHunterview.Service.Service.Matching
{
    public class VectorEmbeddingService : IVectorEmbeddingService
    {
        private readonly ILogger<VectorEmbeddingService> _logger;

        public VectorEmbeddingService(ILogger<VectorEmbeddingService> logger)
        {
            _logger = logger;
        }

        public Task<float[]> EmbedTextAsync(string text)
        {
            throw new NotImplementedException("Giai đoạn 3: Gọi Embedding API.");
        }

        public Task<List<float[]>> EmbedBatchAsync(List<string> texts)
        {
            throw new NotImplementedException("Giai đoạn 3: Gọi Embedding API theo batch.");
        }
    }
}
