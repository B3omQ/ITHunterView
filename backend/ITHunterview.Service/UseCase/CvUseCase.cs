using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
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
        private readonly ICandidateProfileRepository _candidateProfileRepository;
        private readonly IMemoryCache _cache;

        public CvUseCase(
            ICvRepository cvRepository,
            Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory,
            Microsoft.Extensions.Logging.ILogger<CvUseCase> logger,
            ITHunterview.Service.Interface.Service.Matching.ICvTextExtractorService textExtractorService,
            ICandidateProfileRepository candidateProfileRepository,
            IMemoryCache cache)
        {
            _cvRepository = cvRepository;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _textExtractorService = textExtractorService;
            _candidateProfileRepository = candidateProfileRepository;
            _cache = cache;
        }

        public async Task<CvResponseDto> CreateCvAsync(Guid userId, CreateCvRequestDto request)
        {
            string? warningMessage = null;

            if (request.IsPrimary)
            {
                if (!CheckAndRecordPrimaryCvRateLimit(userId, isCheckOnly: true))
                {
                    request.IsPrimary = false;
                    warningMessage = "Tải CV thành công nhưng không thể đặt làm CV Chính do bạn đã đạt giới hạn (3 lần/ngày hoặc chờ 20s).";
                }
                else
                {
                    await _cvRepository.ResetPrimaryCvAsync(userId);
                    CheckAndRecordPrimaryCvRateLimit(userId, isCheckOnly: false);
                }
            }
            
            if (request.IsTemporary)
            {
                request.IsPrimary = false;
            }
            else if (!request.IsPrimary)
            {
                bool hasPrimary = await _cvRepository.HasPrimaryCvAsync(userId);
                if (!hasPrimary)
                {
                    // Nếu là CV đầu tiên, không tính vào Rate Limit
                    request.IsPrimary = true;
                }
            }

            string extractedRawText = string.Empty;
            try 
            {
                extractedRawText = await _textExtractorService.ExtractTextFromUrlAsync(request.FileUrl);
                if (!string.IsNullOrEmpty(extractedRawText))
                {
                    extractedRawText = extractedRawText.Replace("\0", string.Empty);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract raw text immediately for CV upload.");
            }

            var cv = new Cvs
            {
                UserId = userId,
                FileUrl = request.FileUrl,
                FileName = request.FileName,
                FileSize = request.FileSize,
                FileType = request.FileType,
                IsPrimary = request.IsPrimary,
                ParsedData = request.ParsedData ?? string.Empty,
                ParseStatus = string.IsNullOrWhiteSpace(request.ParsedData) ? "PENDING" : "SUCCESS",
                RawText = extractedRawText,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DeletedAt = request.IsTemporary ? DateTime.UtcNow : null
            };

            var createdCv = await _cvRepository.CreateAsync(cv);

            if (createdCv.IsPrimary && createdCv.ParseStatus == "PENDING")
            {
                var profile = await _candidateProfileRepository.GetByUserIdAsync(userId);
                if (profile != null && profile.IsVisibleToRecruiters)
                {
                    bool isLocked = await _cvRepository.TryLockCvForParsingAsync(createdCv.Id);
                    if (isLocked)
                    {
                        _ = Task.Run(() => ParseCvBackgroundAsync(createdCv.Id, createdCv.RawText, createdCv.FileUrl));
                    }
                }
            }

            var responseDto = MapToDto(createdCv);
            responseDto.WarningMessage = warningMessage;
            return responseDto;
        }

        public async Task ParseCvBackgroundAsync(Guid cvId, string rawTextFallback, string fileUrl)
        {
            using var scope = _scopeFactory.CreateScope();
            var textExtractor = scope.ServiceProvider.GetRequiredService<ITHunterview.Service.Interface.Service.Matching.ICvTextExtractorService>();
            var cvRepo = scope.ServiceProvider.GetRequiredService<ICvRepository>();

            var cvToUpdate = await cvRepo.GetByIdAsync(cvId);
            if (cvToUpdate == null) return;

            try
            {
                _logger.LogInformation("Starting background parsing for CV {CvId}", cvId);
                // Note: ParseStatus is already set to PROCESSING by TryLockCvForParsingAsync before calling this background task.

                var parsedJson = await textExtractor.ExtractParsedDataFromUrlAsync(fileUrl, rawTextFallback);

                if (!string.IsNullOrEmpty(parsedJson))
                {
                    parsedJson = parsedJson.Replace("\0", string.Empty);
                }

                var freshCv = await cvRepo.GetByIdAsync(cvId);
                if (freshCv == null) return;

                if (!string.IsNullOrWhiteSpace(parsedJson))
                {
                    using (System.Text.Json.JsonDocument.Parse(parsedJson))
                    {
                        freshCv.ParsedData = parsedJson;
                        freshCv.ParseStatus = "SUCCESS";
                        freshCv.ParseError = null;
                        freshCv.UpdatedAt = DateTime.UtcNow;
                        await cvRepo.UpdateAsync(freshCv);
                        _logger.LogInformation("Successfully parsed and updated CV {CvId}", cvId);
                    }
                }
                else
                {
                    freshCv.ParseStatus = "FAILED";
                    freshCv.ParseError = "AI returned empty JSON content";
                    freshCv.UpdatedAt = DateTime.UtcNow;
                    await cvRepo.UpdateAsync(freshCv);
                    _logger.LogWarning("Background parsing resulted in empty JSON for CV {CvId}", cvId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse CV {CvId} in background", cvId);
                var freshCv = await cvRepo.GetByIdAsync(cvId);
                if (freshCv != null)
                {
                    freshCv.ParseStatus = "FAILED";
                    freshCv.ParseError = ex.Message;
                    freshCv.UpdatedAt = DateTime.UtcNow;
                    await cvRepo.UpdateAsync(freshCv);
                }
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

        public async Task SetPrimaryCvAsync(Guid id, Guid userId)
        {
            if (!CheckAndRecordPrimaryCvRateLimit(userId, isCheckOnly: true))
            {
                throw new InvalidOperationException("Bạn đã đạt giới hạn thay đổi CV chính (3 lần/ngày) hoặc thao tác quá nhanh (đợi 20s).");
            }

            await _cvRepository.SetPrimaryCvAsync(id, userId);
            CheckAndRecordPrimaryCvRateLimit(userId, isCheckOnly: false);
            
            // Check visibility and parse if needed
            var profile = await _candidateProfileRepository.GetByUserIdAsync(userId);
            if (profile != null && profile.IsVisibleToRecruiters)
            {
                var targetCv = await _cvRepository.GetByIdAsync(id);
                if (targetCv != null && targetCv.ParseStatus == "PENDING")
                {
                    bool isLocked = await _cvRepository.TryLockCvForParsingAsync(id);
                    if (isLocked)
                    {
                        // Fire and forget background task
                        _ = Task.Run(() => ParseCvBackgroundAsync(id, targetCv.RawText, targetCv.FileUrl));
                    }
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
                ParseStatus = cv.ParseStatus ?? "PENDING",
                ParseError = cv.ParseError,
                CreatedAt = cv.CreatedAt,
                UpdatedAt = cv.UpdatedAt
            };
        }

        private bool CheckAndRecordPrimaryCvRateLimit(Guid userId, bool isCheckOnly)
        {
            TimeZoneInfo vnTimeZone;
            try
            {
                vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            }
            
            var vnTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            string dateStr = vnTime.ToString("yyyyMMdd");

            string cooldownKey = $"CvPrimarySet_Cooldown_{userId}";
            string dailyLimitKey = $"CvPrimarySet_DailyCount_{userId}_{dateStr}";

            if (_cache.TryGetValue(cooldownKey, out _))
            {
                return false; // Cooldown not met
            }

            if (_cache.TryGetValue(dailyLimitKey, out int currentCount) && currentCount >= 3)
            {
                return false; // Daily limit exceeded
            }

            if (!isCheckOnly)
            {
                _cache.Set(cooldownKey, true, TimeSpan.FromSeconds(20));

                var eod = vnTime.Date.AddDays(1); // Midnight next day VN time
                var timeUntilEod = eod - vnTime;
                
                _cache.Set(dailyLimitKey, currentCount + 1, timeUntilEod);
            }

            return true;
        }
    }
}
