using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.CandidateProfile;
using ITHunterview.Service.DTOs.Cv;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.UseCase;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ITHunterview.Service.Tests.UseCase
{
    public class CvUseCaseTests
    {
        private readonly Mock<ICvRepository> _cvRepositoryMock;
        private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
        private readonly Mock<ILogger<CvUseCase>> _loggerMock;
        private readonly Mock<ICvTextExtractorService> _textExtractorServiceMock;
        private readonly Mock<ICandidateProfileRepository> _candidateProfileRepositoryMock;
        private readonly IMemoryCache _cache;
        private readonly CvUseCase _sut;

        public CvUseCaseTests()
        {
            _cvRepositoryMock = new Mock<ICvRepository>();
            _scopeFactoryMock = new Mock<IServiceScopeFactory>();
            _loggerMock = new Mock<ILogger<CvUseCase>>();
            _textExtractorServiceMock = new Mock<ICvTextExtractorService>();
            _candidateProfileRepositoryMock = new Mock<ICandidateProfileRepository>();
            _cache = new MemoryCache(new MemoryCacheOptions());

            _sut = new CvUseCase(
                _cvRepositoryMock.Object,
                _scopeFactoryMock.Object,
                _loggerMock.Object,
                _textExtractorServiceMock.Object,
                _candidateProfileRepositoryMock.Object,
                _cache
            );
        }

        [Fact]
        public async Task CreateCvAsync_UTCID01_RateLimitFails_SetsIsPrimaryFalseAndWarning()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new CreateCvRequestDto { IsPrimary = true, FileUrl = "url", FileName = "f", FileSize = 1, FileType = "pdf" };
            
            // Force rate limit failure by setting daily limit cache key
            string dateStr = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")).ToString("yyyyMMdd");
            string dailyLimitKey = $"CvPrimarySet_DailyCount_{userId}_{dateStr}";
            _cache.Set(dailyLimitKey, 3);
            _cache.Set($"CvPrimarySet_Cooldown_{userId}", true);
            
            // MUST set HasPrimaryCvAsync = true so it doesn't force IsPrimary = true back
            _cvRepositoryMock.Setup(x => x.HasPrimaryCvAsync(userId)).ReturnsAsync(true);

            _textExtractorServiceMock.Setup(x => x.ExtractTextFromUrlAsync(It.IsAny<string>())).ReturnsAsync("extracted");
            _cvRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Cvs>())).ReturnsAsync((Cvs cv) => { cv.Id = Guid.NewGuid(); return cv; });

            // Act
            var result = await _sut.CreateCvAsync(userId, request);

            // Assert
            result.IsPrimary.Should().BeFalse();
            result.WarningMessage.Should().NotBeNullOrEmpty();
            result.WarningMessage.Should().Contain("không thể đặt làm CV Chính do bạn đã đạt giới hạn");
            
            _cvRepositoryMock.Verify(x => x.ResetPrimaryCvAsync(userId), Times.Never);
            _cvRepositoryMock.Verify(x => x.CreateAsync(It.Is<Cvs>(c => !c.IsPrimary && c.DeletedAt == null && c.RawText == "extracted")), Times.Once);
        }

        [Fact]
        public async Task CreateCvAsync_UTCID04_FirstCv_SetsIsPrimaryTrueAutomatically()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new CreateCvRequestDto { IsPrimary = false, IsTemporary = false, FileUrl = "url", FileName = "f", FileSize = 1, FileType = "pdf" };
            
            _cvRepositoryMock.Setup(x => x.HasPrimaryCvAsync(userId)).ReturnsAsync(false);
            _textExtractorServiceMock.Setup(x => x.ExtractTextFromUrlAsync(It.IsAny<string>())).ReturnsAsync("ABC");
            _cvRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Cvs>())).ReturnsAsync((Cvs cv) => { cv.Id = Guid.NewGuid(); return cv; });

            // Act
            var result = await _sut.CreateCvAsync(userId, request);

            // Assert
            result.IsPrimary.Should().BeTrue();
            result.WarningMessage.Should().BeNull();
            
            _cvRepositoryMock.Verify(x => x.ResetPrimaryCvAsync(userId), Times.Never);
            _cvRepositoryMock.Verify(x => x.CreateAsync(It.Is<Cvs>(c => c.IsPrimary && c.DeletedAt == null && c.RawText == "ABC")), Times.Once);
        }

        [Fact]
        public async Task CreateCvAsync_UTCID03_TemporaryCv_SetsIsPrimaryFalseAndDeletedAt()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new CreateCvRequestDto { IsPrimary = false, IsTemporary = true, FileUrl = "url", FileName = "f", FileSize = 1, FileType = "pdf" };
            
            _textExtractorServiceMock.Setup(x => x.ExtractTextFromUrlAsync(It.IsAny<string>())).ReturnsAsync("ABC");
            _cvRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Cvs>())).ReturnsAsync((Cvs cv) => { cv.Id = Guid.NewGuid(); return cv; });

            // Act
            var result = await _sut.CreateCvAsync(userId, request);

            // Assert
            result.IsPrimary.Should().BeFalse();
            result.WarningMessage.Should().BeNull();
            
            _cvRepositoryMock.Verify(x => x.CreateAsync(It.Is<Cvs>(c => !c.IsPrimary && c.DeletedAt != null && c.RawText == "ABC")), Times.Once);
        }

        [Fact]
        public async Task CreateCvAsync_UTCID05_TextExtractorThrows_SavesEmptyRawText()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new CreateCvRequestDto { IsPrimary = false, IsTemporary = false, FileUrl = "url", FileName = "f", FileSize = 1, FileType = "pdf" };
            
            _cvRepositoryMock.Setup(x => x.HasPrimaryCvAsync(userId)).ReturnsAsync(true);
            _textExtractorServiceMock.Setup(x => x.ExtractTextFromUrlAsync(It.IsAny<string>())).ThrowsAsync(new Exception("API Error"));
            _cvRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Cvs>())).ReturnsAsync((Cvs cv) => { cv.Id = Guid.NewGuid(); return cv; });

            // Act
            var result = await _sut.CreateCvAsync(userId, request);

            // Assert
            result.WarningMessage.Should().BeNull();
            
            _cvRepositoryMock.Verify(x => x.CreateAsync(It.Is<Cvs>(c => c.RawText == string.Empty)), Times.Once);
        }

        [Fact]
        public async Task CreateCvAsync_UTCID07_ProfileNotVisible_DoesNotTriggerBackgroundParse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new CreateCvRequestDto { IsPrimary = true, FileUrl = "url", FileName = "f", FileSize = 1, FileType = "pdf" };
            
            _textExtractorServiceMock.Setup(x => x.ExtractTextFromUrlAsync(It.IsAny<string>())).ReturnsAsync("ABC");
            _cvRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Cvs>())).ReturnsAsync((Cvs cv) => { cv.Id = Guid.NewGuid(); cv.IsPrimary = true; cv.ParseStatus = "PENDING"; return cv; });
            
            var profile = new CandidateProfiles { IsVisibleToRecruiters = false };
            _candidateProfileRepositoryMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(profile);

            // Act
            var result = await _sut.CreateCvAsync(userId, request);

            // Assert
            result.IsPrimary.Should().BeTrue();
            result.WarningMessage.Should().BeNull();
            
            _cvRepositoryMock.Verify(x => x.ResetPrimaryCvAsync(userId), Times.Once);
            _cvRepositoryMock.Verify(x => x.TryLockCvForParsingAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task CreateCvAsync_UTCID06_HappyPath_TriggersBackgroundParse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new CreateCvRequestDto { IsPrimary = true, FileUrl = "url", FileName = "f", FileSize = 1, FileType = "pdf" };
            
            _textExtractorServiceMock.Setup(x => x.ExtractTextFromUrlAsync(It.IsAny<string>())).ReturnsAsync("ABC");
            _cvRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Cvs>())).ReturnsAsync((Cvs cv) => { cv.Id = Guid.NewGuid(); cv.IsPrimary = true; cv.ParseStatus = "PENDING"; return cv; });
            
            var profile = new CandidateProfiles { IsVisibleToRecruiters = true };
            _candidateProfileRepositoryMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(profile);
            _cvRepositoryMock.Setup(x => x.TryLockCvForParsingAsync(It.IsAny<Guid>())).ReturnsAsync(true);

            // Act
            var result = await _sut.CreateCvAsync(userId, request);

            // Assert
            result.IsPrimary.Should().BeTrue();
            result.WarningMessage.Should().BeNull();
            
            _cvRepositoryMock.Verify(x => x.ResetPrimaryCvAsync(userId), Times.Once);
            _cvRepositoryMock.Verify(x => x.TryLockCvForParsingAsync(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task CreateCvAsync_UTCID02_LockFails_DoesNotTriggerBackgroundParse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new CreateCvRequestDto { IsPrimary = true, FileUrl = "url", FileName = "f", FileSize = 1, FileType = "pdf" };
            
            _textExtractorServiceMock.Setup(x => x.ExtractTextFromUrlAsync(It.IsAny<string>())).ReturnsAsync("ABC");
            _cvRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Cvs>())).ReturnsAsync((Cvs cv) => { cv.Id = Guid.NewGuid(); cv.IsPrimary = true; cv.ParseStatus = "PENDING"; return cv; });
            
            var profile = new CandidateProfiles { IsVisibleToRecruiters = true };
            _candidateProfileRepositoryMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(profile);
            _cvRepositoryMock.Setup(x => x.TryLockCvForParsingAsync(It.IsAny<Guid>())).ReturnsAsync(false);

            // Act
            var result = await _sut.CreateCvAsync(userId, request);

            // Assert
            result.IsPrimary.Should().BeTrue();
            result.WarningMessage.Should().BeNull();
            
            _cvRepositoryMock.Verify(x => x.ResetPrimaryCvAsync(userId), Times.Once);
            _cvRepositoryMock.Verify(x => x.TryLockCvForParsingAsync(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task SetPrimaryCvAsync_UTCID01_RateLimitFails_ThrowsException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var cvId = Guid.NewGuid();
            
            string cooldownKey = $"CvPrimarySet_Cooldown_{userId}";
            _cache.Set(cooldownKey, true);

            // Act
            Func<Task> action = async () => await _sut.SetPrimaryCvAsync(cvId, userId);

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*đã đạt giới hạn thay đổi CV chính*");
            
            _cvRepositoryMock.Verify(x => x.SetPrimaryCvAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task SetPrimaryCvAsync_UTCID02_ProfileNull_DoesNotParse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var cvId = Guid.NewGuid();
            
            _candidateProfileRepositoryMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync((CandidateProfiles)null);

            // Act
            await _sut.SetPrimaryCvAsync(cvId, userId);

            // Assert
            _cvRepositoryMock.Verify(x => x.SetPrimaryCvAsync(cvId, userId), Times.Once);
            _cvRepositoryMock.Verify(x => x.TryLockCvForParsingAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task SetPrimaryCvAsync_UTCID03_ProfileNotVisible_DoesNotParse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var cvId = Guid.NewGuid();
            
            var profile = new CandidateProfiles { IsVisibleToRecruiters = false };
            _candidateProfileRepositoryMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(profile);

            // Act
            await _sut.SetPrimaryCvAsync(cvId, userId);

            // Assert
            _cvRepositoryMock.Verify(x => x.SetPrimaryCvAsync(cvId, userId), Times.Once);
            _cvRepositoryMock.Verify(x => x.TryLockCvForParsingAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task SetPrimaryCvAsync_UTCID04_TargetCvNull_DoesNotParse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var cvId = Guid.NewGuid();
            
            var profile = new CandidateProfiles { IsVisibleToRecruiters = true };
            _candidateProfileRepositoryMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(profile);
            
            _cvRepositoryMock.Setup(x => x.GetByIdAsync(cvId)).ReturnsAsync((Cvs)null);

            // Act
            await _sut.SetPrimaryCvAsync(cvId, userId);

            // Assert
            _cvRepositoryMock.Verify(x => x.SetPrimaryCvAsync(cvId, userId), Times.Once);
            _cvRepositoryMock.Verify(x => x.TryLockCvForParsingAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task SetPrimaryCvAsync_UTCID05_TargetCvNotPending_DoesNotParse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var cvId = Guid.NewGuid();
            
            var profile = new CandidateProfiles { IsVisibleToRecruiters = true };
            _candidateProfileRepositoryMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(profile);
            
            var cv = new Cvs { Id = cvId, ParseStatus = "COMPLETED" };
            _cvRepositoryMock.Setup(x => x.GetByIdAsync(cvId)).ReturnsAsync(cv);

            // Act
            await _sut.SetPrimaryCvAsync(cvId, userId);

            // Assert
            _cvRepositoryMock.Verify(x => x.SetPrimaryCvAsync(cvId, userId), Times.Once);
            _cvRepositoryMock.Verify(x => x.TryLockCvForParsingAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task SetPrimaryCvAsync_UTCID06_TryLockFails_DoesNotParse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var cvId = Guid.NewGuid();
            
            var profile = new CandidateProfiles { IsVisibleToRecruiters = true };
            _candidateProfileRepositoryMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(profile);
            
            var cv = new Cvs { Id = cvId, ParseStatus = "PENDING" };
            _cvRepositoryMock.Setup(x => x.GetByIdAsync(cvId)).ReturnsAsync(cv);
            
            _cvRepositoryMock.Setup(x => x.TryLockCvForParsingAsync(cvId)).ReturnsAsync(false);

            // Act
            await _sut.SetPrimaryCvAsync(cvId, userId);

            // Assert
            _cvRepositoryMock.Verify(x => x.SetPrimaryCvAsync(cvId, userId), Times.Once);
            _cvRepositoryMock.Verify(x => x.TryLockCvForParsingAsync(cvId), Times.Once);
        }

        [Fact]
        public async Task SetPrimaryCvAsync_UTCID07_HappyPath_TriggersBackgroundParse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var cvId = Guid.NewGuid();
            
            var profile = new CandidateProfiles { IsVisibleToRecruiters = true };
            _candidateProfileRepositoryMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(profile);
            
            var cv = new Cvs { Id = cvId, ParseStatus = "PENDING" };
            _cvRepositoryMock.Setup(x => x.GetByIdAsync(cvId)).ReturnsAsync(cv);
            
            _cvRepositoryMock.Setup(x => x.TryLockCvForParsingAsync(cvId)).ReturnsAsync(true);

            // Act
            await _sut.SetPrimaryCvAsync(cvId, userId);

            // Assert
            _cvRepositoryMock.Verify(x => x.SetPrimaryCvAsync(cvId, userId), Times.Once);
            _cvRepositoryMock.Verify(x => x.TryLockCvForParsingAsync(cvId), Times.Once);
        }

        // --- Helper Methods cho Test private ---
        private bool InvokeCheckAndRecordPrimaryCvRateLimit(Guid userId, bool isCheckOnly)
        {
            var method = typeof(CvUseCase).GetMethod("CheckAndRecordPrimaryCvRateLimit", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            try
            {
                return (bool)method.Invoke(_sut, new object[] { userId, isCheckOnly });
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        private string GetVnDateString()
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
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone).ToString("yyyyMMdd");
        }

        [Fact]
        public void CheckAndRecordPrimaryCvRateLimit_UTCID01_CooldownActive_ReturnsFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            string cooldownKey = $"CvPrimarySet_Cooldown_{userId}";
            _cache.Set(cooldownKey, true); // Set cooldown

            // Act
            var result = InvokeCheckAndRecordPrimaryCvRateLimit(userId, isCheckOnly: false);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void CheckAndRecordPrimaryCvRateLimit_UTCID02_DailyLimitReached_ReturnsFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            string dateStr = GetVnDateString();
            string dailyLimitKey = $"CvPrimarySet_DailyCount_{userId}_{dateStr}";
            _cache.Set(dailyLimitKey, 3); // 3 lần là full giới hạn

            // Act
            var result = InvokeCheckAndRecordPrimaryCvRateLimit(userId, isCheckOnly: false);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void CheckAndRecordPrimaryCvRateLimit_UTCID03_IsCheckOnly_ReturnsTrueAndDoesNotRecord()
        {
            // Arrange
            var userId = Guid.NewGuid();
            string cooldownKey = $"CvPrimarySet_Cooldown_{userId}";
            string dailyLimitKey = $"CvPrimarySet_DailyCount_{userId}_{GetVnDateString()}";

            // Act
            var result = InvokeCheckAndRecordPrimaryCvRateLimit(userId, isCheckOnly: true);

            // Assert
            result.Should().BeTrue();
            _cache.TryGetValue(cooldownKey, out _).Should().BeFalse("Không được lưu cache vì isCheckOnly = true");
            _cache.TryGetValue(dailyLimitKey, out _).Should().BeFalse("Không được lưu cache vì isCheckOnly = true");
        }

        [Fact]
        public void CheckAndRecordPrimaryCvRateLimit_UTCID04_HappyPath_ReturnsTrueAndRecords()
        {
            // Arrange
            var userId = Guid.NewGuid();
            string cooldownKey = $"CvPrimarySet_Cooldown_{userId}";
            string dailyLimitKey = $"CvPrimarySet_DailyCount_{userId}_{GetVnDateString()}";
            
            _cache.Set(dailyLimitKey, 1); // Đã dùng 1 lần trong ngày

            // Act
            var result = InvokeCheckAndRecordPrimaryCvRateLimit(userId, isCheckOnly: false);

            // Assert
            result.Should().BeTrue();
            
            // Verify Cooldown was set
            _cache.TryGetValue(cooldownKey, out _).Should().BeTrue("Cần set cooldown key");
            
            // Verify Daily Limit count increased
            _cache.TryGetValue(dailyLimitKey, out int currentCount).Should().BeTrue();
            currentCount.Should().Be(2, "Count phải được tăng lên thành 2");
        }
    }
}
