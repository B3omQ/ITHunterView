using System;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Service.Matching;
using Microsoft.Extensions.Logging;

namespace ITHunterview.Service.Service.Matching
{
    public class JdExtractionService : IJdExtractionService
    {
        private readonly ILogger<JdExtractionService> _logger;

        public JdExtractionService(ILogger<JdExtractionService> logger)
        {
            _logger = logger;
        }

        public Task<JdExtractionResultDto> ExtractRequirementsAsync(string rawJdText)
        {
            throw new NotImplementedException("Giai đoạn 3: Gọi LLM trích xuất JD.");
        }
    }
}
