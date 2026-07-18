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
        private readonly ITHunterview.Service.Interface.Service.Matching.ICvTextExtractorService _textExtractorService;

        public CvUseCase(
            ICvRepository cvRepository,
            Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory,
            Microsoft.Extensions.Logging.ILogger<CvUseCase> logger,
            ITHunterview.Service.Interface.Service.Matching.ICvTextExtractorService textExtractorService)
        {
            _cvRepository = cvRepository;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _textExtractorService = textExtractorService;
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

            string extractedRawText = string.Empty;
            try 
            {
                extractedRawText = await _textExtractorService.ExtractTextFromUrlAsync(request.FileUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract raw text immediately for CV upload.");
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
                RawText = extractedRawText,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdCv = await _cvRepository.CreateAsync(cv);

            if (string.IsNullOrWhiteSpace(createdCv.ParsedData))
            {
                _ = ParseCvBackgroundAsync(createdCv.Id, createdCv.RawText, createdCv.FileUrl);
            }

            return MapToDto(createdCv);
        }

        private async Task ParseCvBackgroundAsync(Guid cvId, string rawTextFallback, string fileUrl)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var textExtractor = scope.ServiceProvider.GetRequiredService<ITHunterview.Service.Interface.Service.Matching.ICvTextExtractorService>();
                var cvRepo = scope.ServiceProvider.GetRequiredService<ICvRepository>();

                _logger.LogInformation("Starting background parsing for CV {CvId}", cvId);

                var parsedJson = await textExtractor.ExtractParsedDataFromUrlAsync(fileUrl, rawTextFallback);

                if (!string.IsNullOrWhiteSpace(parsedJson))
                {
                    // Basic validation to ensure it's JSON
                    try 
                    {
                        using (System.Text.Json.JsonDocument.Parse(parsedJson))
                        {
                            var cvToUpdate = await cvRepo.GetByIdAsync(cvId);
                            if (cvToUpdate != null)
                            {
                                cvToUpdate.ParsedData = parsedJson;
                                cvToUpdate.UpdatedAt = DateTime.UtcNow;
                                await cvRepo.UpdateAsync(cvToUpdate);
                                _logger.LogInformation("Successfully parsed and updated CV {CvId}", cvId);
                            }
                        }
                    }
                    catch (System.Text.Json.JsonException ex)
                    {
                        _logger.LogError(ex, "Failed to validate parsed JSON for CV {CvId}", cvId);
                    }
                }
                else
                {
                    _logger.LogWarning("Background parsing resulted in empty JSON for CV {CvId}", cvId);
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
