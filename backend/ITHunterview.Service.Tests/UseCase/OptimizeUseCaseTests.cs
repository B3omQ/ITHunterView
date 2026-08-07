using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Entities.Cv;
using ITHunterview.Service.DTOs.Optimize;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.UseCase;
using Moq;
using Xunit;
using FluentAssertions;

namespace ITHunterview.Service.Tests.UseCase
{
    public class OptimizeUseCaseTests
    {
        private readonly Mock<IOptimizeSessionRepository> _mockSessionRepo;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<IAiService> _mockAiService;
        private readonly OptimizeUseCase _sut;

        public OptimizeUseCaseTests()
        {
            _mockSessionRepo = new Mock<IOptimizeSessionRepository>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockAiService = new Mock<IAiService>();
            _sut = new OptimizeUseCase(_mockSessionRepo.Object, _mockServiceProvider.Object, _mockHttpClientFactory.Object, _mockAiService.Object);
        }

        [Fact]
        public async Task CreateSessionAndAnalyzeAsync_ShouldThrowArgumentException_WhenCvUrlAndCvIdAreBothNull()
        {
            // Act
            Func<Task> act = async () => await _sut.CreateSessionAndAnalyzeAsync(Guid.NewGuid(), null, null);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Either CvUrl or CvId must be provided*");
        }

        [Fact]
        public async Task GetSessionResultAsync_ShouldThrowKeyNotFoundException_WhenSessionDoesNotExist()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            _mockSessionRepo.Setup(x => x.GetByIdAsync(sessionId)).ReturnsAsync((OptimizeSession?)null);

            // Act
            Func<Task> act = async () => await _sut.GetSessionResultAsync(sessionId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*Optimize session not found*");
        }

        [Fact]
        public async Task GetSessionResultAsync_ShouldReturnDeserializedDto_WhenSessionExists()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var resultDto = new CvOptimizationResultDto
            {
                SessionId = sessionId,
                OverallScore = 88,
                Summary = "Test summary"
            };

            var session = new OptimizeSession
            {
                Id = sessionId,
                OverallScore = 88,
                AnalysisResultJson = JsonSerializer.Serialize(resultDto)
            };

            _mockSessionRepo.Setup(x => x.GetByIdAsync(sessionId)).ReturnsAsync(session);

            // Act
            var result = await _sut.GetSessionResultAsync(sessionId);

            // Assert
            result.Should().NotBeNull();
            result.OverallScore.Should().Be(88);
            result.Summary.Should().Be("Test summary");
        }

        [Fact]
        public async Task GeneratePreviewAsync_ShouldThrowKeyNotFoundException_WhenSessionIsNull()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            _mockSessionRepo.Setup(x => x.GetByIdAsync(sessionId)).ReturnsAsync((OptimizeSession?)null);

            // Act
            Func<Task> act = async () => await _sut.GeneratePreviewAsync(sessionId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*Session not found*");
        }

        [Fact]
        public async Task GenerateFinalFileAsync_ShouldThrowKeyNotFoundException_WhenSessionIsNull()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            _mockSessionRepo.Setup(x => x.GetByIdAsync(sessionId)).ReturnsAsync((OptimizeSession?)null);

            // Act
            Func<Task> act = async () => await _sut.GenerateFinalFileAsync(sessionId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*Session not found*");
        }
    }
}
