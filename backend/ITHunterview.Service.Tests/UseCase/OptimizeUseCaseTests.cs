using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Entities.Cv;
using ITHunterview.Service.Interface.Persistence;
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
        private readonly OptimizeUseCase _sut;

        public OptimizeUseCaseTests()
        {
            _mockSessionRepo = new Mock<IOptimizeSessionRepository>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _sut = new OptimizeUseCase(_mockSessionRepo.Object, _mockServiceProvider.Object, _mockHttpClientFactory.Object);
        }

        [Fact]
        public async Task CreateSessionAsync_ShouldThrowArgumentException_WhenCvUrlAndCvIdAreBothNull()
        {
            // Act
            Func<Task> act = async () => await _sut.CreateSessionAsync(Guid.NewGuid(), null, null);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Either CvUrl or CvId must be provided*");
        }

        [Fact]
        public async Task GetSuggestionsAsync_ShouldThrowKeyNotFoundException_WhenSessionDoesNotExist()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            _mockSessionRepo.Setup(x => x.GetByIdAsync(sessionId)).ReturnsAsync((OptimizeSession?)null);

            // Act
            Func<Task> act = async () => await _sut.GetSuggestionsAsync(sessionId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*Session not found*");
        }

        [Fact]
        public async Task ApplySuggestionAsync_ShouldThrowKeyNotFoundException_WhenSessionOrCvDocumentIsNull()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            _mockSessionRepo.Setup(x => x.GetByIdAsync(sessionId)).ReturnsAsync((OptimizeSession?)null);

            // Act
            Func<Task> act = async () => await _sut.ApplySuggestionAsync(sessionId, "sug1", "accept", "old", "old", "new");

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*Session or CV Document not found*");
        }

        [Fact]
        public async Task ApplySuggestionAsync_ShouldThrowArgumentException_WhenAcceptingWithoutRequiredTexts()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var session = new OptimizeSession
            {
                Id = sessionId,
                CvDocument = new CvDocument { Header = new CvHeader { FullName = "Test Candidate" } }
            };
            _mockSessionRepo.Setup(x => x.GetByIdAsync(sessionId)).ReturnsAsync(session);

            // Act
            Func<Task> act = async () => await _sut.ApplySuggestionAsync(sessionId, "sug1", "accept", null, null, null);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*OriginalText and SuggestedText are required*");
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
