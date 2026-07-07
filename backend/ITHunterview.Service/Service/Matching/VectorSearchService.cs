using System;
using System.Collections.Generic;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Service.Matching;
using Microsoft.Extensions.Logging;

namespace ITHunterview.Service.Service.Matching
{
    public class VectorSearchService : IVectorSearchService
    {
        private readonly ILogger<VectorSearchService> _logger;

        public VectorSearchService(ILogger<VectorSearchService> logger)
        {
            _logger = logger;
        }

        public Dictionary<string, List<CvChunkDto>> SearchTopChunks(
            List<JdRequirementDto> requirements,
            List<float[]> reqEmbeddings,
            List<CvChunkDto> cvChunks,
            int topK = 3)
        {
            throw new NotImplementedException("Giai đoạn 3: Implement Cosine Similarity search C#.");
        }
    }
}
