using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Service.Matching;
using Microsoft.Extensions.Logging;

namespace ITHunterview.Service.Service.Matching
{
    public class CvQualityScoringService : ICvQualityScoringService
    {
        private readonly ILogger<CvQualityScoringService> _logger;

        public CvQualityScoringService(ILogger<CvQualityScoringService> logger)
        {
            _logger = logger;
        }

        public Task<CvQualityResultDto> ScoreAsync(
            string rawCvText,
            List<CvChunkDto> cvChunks)
        {
            throw new NotImplementedException("Giai đoạn 3: Gọi LLM đánh giá CvQuality.");
        }
    }
}
