using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Cv;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase
{
    public class CvUseCase : ICvUseCase
    {
        private readonly ICvRepository _cvRepository;
        private readonly Microsoft.Extensions.DependencyInjection.IServiceScopeFactory _scopeFactory;
        private readonly Microsoft.Extensions.Logging.ILogger<CvUseCase> _logger;

        public CvUseCase(
            ICvRepository cvRepository,
            Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory,
            Microsoft.Extensions.Logging.ILogger<CvUseCase> logger)
        {
            _cvRepository = cvRepository;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<CvResponseDto> CreateCvAsync(Guid userId, CreateCvRequestDto request)
        {
            if (request.IsPrimary)
            {
                await _cvRepository.ResetPrimaryCvAsync(userId);
            }
            else
            {
                bool hasPrimary = await _cvRepository.HasPrimaryCvAsync(userId);
                if (!hasPrimary)
                {
                    request.IsPrimary = true;
                }
            }

            var cv = new Cvs
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FileUrl = request.FileUrl,
                FileName = request.FileName,
                FileSize = request.FileSize,
                FileType = request.FileType,
                IsPrimary = request.IsPrimary,
                ParsedData = request.ParsedData ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdCv = await _cvRepository.CreateAsync(cv);

            if (string.IsNullOrWhiteSpace(createdCv.ParsedData))
            {
                _ = ParseCvBackgroundAsync(createdCv.Id, createdCv.FileUrl);
            }

            return MapToDto(createdCv);
        }

        private async Task ParseCvBackgroundAsync(Guid cvId, string fileUrl)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var textExtractor = scope.ServiceProvider.GetRequiredService<ITHunterview.Service.Interface.Service.Matching.ICvTextExtractorService>();
                var aiService = scope.ServiceProvider.GetRequiredService<ITHunterview.Service.Interface.Service.IAiService>();
                var cvRepo = scope.ServiceProvider.GetRequiredService<ICvRepository>();

                _logger.LogInformation("Starting background parsing for CV {CvId}", cvId);

                var rawText = await textExtractor.ExtractTextFromUrlAsync(fileUrl);
                if (string.IsNullOrWhiteSpace(rawText))
                {
                    _logger.LogWarning("Extraction returned empty text for CV {CvId}", cvId);
                    return;
                }

                if (rawText.Length > 20000) rawText = rawText.Substring(0, 20000);

                var prompt = ITHunterview.Service.Constant.Prompts.CvParsingPrompt.GetPrompt(rawText);
                var systemPrompt = ITHunterview.Service.Constant.Prompts.CvParsingPrompt.SystemPrompt;

                var aiResponse = await aiService.GenerateTextAsync(prompt, systemPrompt);

                // Try to extract JSON if it was wrapped in markdown
                string jsonString = aiResponse;
                if (jsonString.Contains("```json"))
                {
                    int start = jsonString.IndexOf("```json") + 7;
                    int end = jsonString.LastIndexOf("```");
                    if (end > start) jsonString = jsonString.Substring(start, end - start);
                }
                else if (jsonString.Contains("```"))
                {
                    int start = jsonString.IndexOf("```") + 3;
                    int end = jsonString.LastIndexOf("```");
                    if (end > start) jsonString = jsonString.Substring(start, end - start);
                }

                jsonString = jsonString.Trim();

                // Validate JSON
                using (System.Text.Json.JsonDocument.Parse(jsonString))
                {
                    var cvToUpdate = await cvRepo.GetByIdAsync(cvId);
                    if (cvToUpdate != null)
                    {
                        cvToUpdate.ParsedData = jsonString;
                        cvToUpdate.UpdatedAt = DateTime.UtcNow;
                        await cvRepo.UpdateAsync(cvToUpdate);
                        _logger.LogInformation("Successfully parsed and updated CV {CvId}", cvId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse CV {CvId} in background", cvId);
            }
        }

        public async Task<IEnumerable<CvResponseDto>> GetMyCvsAsync(Guid userId)
        {
            var cvs = await _cvRepository.GetByUserIdAsync(userId);
            return cvs.Select(MapToDto);
        }

        public async Task<CvResponseDto> GetCvByIdAsync(Guid id, Guid userId)
        {
            var cv = await _cvRepository.GetByIdAsync(id);
            if (cv == null || cv.UserId != userId)
            {
                throw new KeyNotFoundException("CV not found");
            }

            return MapToDto(cv);
        }

        public async Task DeleteCvAsync(Guid id, Guid userId)
        {
            var cv = await _cvRepository.GetByIdAsync(id);
            if (cv == null || cv.UserId != userId)
            {
                throw new KeyNotFoundException("CV not found");
            }

            await _cvRepository.DeleteAsync(cv);

            if (cv.IsPrimary)
            {
                var remainingCvs = await _cvRepository.GetByUserIdAsync(userId);
                var newestCv = remainingCvs.FirstOrDefault();
                if (newestCv != null)
                {
                    newestCv.IsPrimary = true;
                    newestCv.UpdatedAt = DateTime.UtcNow;
                    await _cvRepository.UpdateAsync(newestCv);
                }
            }
        }

        private CvResponseDto MapToDto(Cvs cv)
        {
            return new CvResponseDto
            {
                Id = cv.Id,
                UserId = cv.UserId,
                FileUrl = cv.FileUrl,
                FileName = cv.FileName,
                FileSize = cv.FileSize,
                FileType = cv.FileType,
                IsPrimary = cv.IsPrimary,
                ParsedData = cv.ParsedData,
                CreatedAt = cv.CreatedAt,
                UpdatedAt = cv.UpdatedAt
            };
        }
    }
}
