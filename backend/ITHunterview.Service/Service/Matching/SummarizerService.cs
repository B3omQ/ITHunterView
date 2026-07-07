using System;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Service.Matching;
using Microsoft.Extensions.Logging;

namespace ITHunterview.Service.Service.Matching
{
    public class SummarizerService : ISummarizerService
    {
        private readonly ILogger<SummarizerService> _logger;

        public SummarizerService(ILogger<SummarizerService> logger)
        {
            _logger = logger;
        }

        public Task<SummaryFeedbackDto> GenerateFeedbackAsync(
            JdFitResultDto? jdFit,
            CvQualityResultDto? cvQuality,
            string jdTitle,
            string jdLevel)
        {
            throw new NotImplementedException("Giai đoạn 3: Gọi LLM tạo summary feedback.");
        }
    }
}
