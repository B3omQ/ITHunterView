using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Service.Matching;
using Microsoft.Extensions.Logging;

namespace ITHunterview.Service.Service.Matching
{
    public class CvTextExtractorService : ICvTextExtractorService
    {
        private readonly ILogger<CvTextExtractorService> _logger;

        public CvTextExtractorService(ILogger<CvTextExtractorService> logger)
        {
            _logger = logger;
        }

        public Task<string> ExtractTextFromUrlAsync(string fileUrl)
        {
            throw new NotImplementedException("Giai đoạn 3: Implement PdfPig to parse PDF from URL.");
        }

        public List<CvChunkDto> ChunkCvText(string rawCvText)
        {
            throw new NotImplementedException("Giai đoạn 3: Implement logic chia đoạn text CV.");
        }
    }
}
