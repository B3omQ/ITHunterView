using System.Collections.Generic;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.Service.Matching
{
    public interface IVectorSearchService
    {
        Dictionary<string, List<CvChunkDto>> SearchTopChunks(
            List<JdRequirementDto> requirements,
            List<float[]> reqEmbeddings,
            List<CvChunkDto> cvChunks,
            int topK = 3);
    }
}
