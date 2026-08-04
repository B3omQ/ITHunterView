using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Config;
using ITHunterview.Service.DTOs.Ai;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.UseCase;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using FluentAssertions;

namespace ITHunterview.Service.Tests.UseCase
{
    public class AiConfigUseCaseTests
    {
        private readonly Mock<IAiProviderFactory> _mockProviderFactory;
        private readonly Mock<ISystemConfigRepository> _mockSystemConfigRepository;
        private readonly AiSettings _settings;
        private readonly AiConfigUseCase _sut;

        public AiConfigUseCaseTests()
        {
            _mockProviderFactory = new Mock<IAiProviderFactory>();
            _mockSystemConfigRepository = new Mock<ISystemConfigRepository>();
            _settings = new AiSettings
            {
                DefaultProvider = "Gemini",
                Providers = new Dictionary<string, ProviderConfig>
                {
                    ["Gemini"] = new ProviderConfig
                    {
                        Model = "gemini-2.5-flash",
                        ApiKey = "sk-valid-key-123456789"
                    }
                }
            };
            var options = Options.Create(_settings);
            _sut = new AiConfigUseCase(_mockProviderFactory.Object, _mockSystemConfigRepository.Object, options);
        }

        [Fact]
        public async Task GetAiConfigAsync_ShouldReturnActiveProviderAndConfiguredProviders()
        {
            // Arrange
            _mockSystemConfigRepository
                .Setup(x => x.GetByKeyAsync("ActiveAiProvider"))
                .ReturnsAsync(new SystemConfigs { ConfigKey = "ActiveAiProvider", ConfigValue = "Gemini" });

            _mockSystemConfigRepository
                .Setup(x => x.GetByKeyAsync("AiRateLimit"))
                .ReturnsAsync(new SystemConfigs { ConfigKey = "AiRateLimit", ConfigValue = "120" });

            // Act
            var result = await _sut.GetAiConfigAsync();

            // Assert
            result.Should().NotBeNull();
            result.ActiveProvider.Should().Be("Gemini");
            result.RequestsPerMinute.Should().Be(120);
            result.AvailableProviders.Should().HaveCount(1);
            result.AvailableProviders[0].IsConfigured.Should().BeTrue();
            result.AvailableProviders[0].ApiKeyPreview.Should().Contain("***");
        }

        [Fact]
        public async Task GetAiConfigAsync_ShouldFallbackToDefaultSettings_WhenDatabaseConfigIsEmpty()
        {
            // Arrange
            _mockSystemConfigRepository
                .Setup(x => x.GetByKeyAsync("ActiveAiProvider"))
                .ReturnsAsync((SystemConfigs?)null);

            _mockSystemConfigRepository
                .Setup(x => x.GetByKeyAsync("AiRateLimit"))
                .ReturnsAsync((SystemConfigs?)null);

            // Act
            var result = await _sut.GetAiConfigAsync();

            // Assert
            result.Should().NotBeNull();
            result.ActiveProvider.Should().Be("Gemini");
            result.RequestsPerMinute.Should().Be(60, "Default RPM is 60");
        }

        [Fact]
        public async Task GetAiConfigAsync_ShouldMarkUnconfigured_WhenApiKeyIsPlaceholderOrEmpty()
        {
            // Arrange
            var unconfiguredSettings = new AiSettings
            {
                DefaultProvider = "Gemini",
                Providers = new Dictionary<string, ProviderConfig>
                {
                    ["Gemini"] = new ProviderConfig
                    {
                        Model = "gemini-2.5-flash",
                        ApiKey = "YOUR_API_KEY_HERE" // Placeholder
                    }
                }
            };
            var sut = new AiConfigUseCase(_mockProviderFactory.Object, _mockSystemConfigRepository.Object, Options.Create(unconfiguredSettings));

            // Act
            var result = await sut.GetAiConfigAsync();

            // Assert
            result.AvailableProviders[0].IsConfigured.Should().BeFalse();
            result.AvailableProviders[0].ApiKeyPreview.Should().BeEmpty();
        }

        [Fact]
        public async Task UpdateAiConfigAsync_ShouldSaveConfiguration_WhenValidProviderProvided()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var mockProvider = new Mock<IAiProvider>();
            mockProvider.Setup(p => p.ProviderName).Returns("Gemini");

            _mockProviderFactory
                .Setup(f => f.GetProvider("Gemini"))
                .Returns(mockProvider.Object);

            var dto = new UpdateAiConfigRequestDto
            {
                ProviderName = "Gemini",
                RequestsPerMinute = 90,
                ApiKey = "sk-new-api-key-999"
            };

            // Act
            await _sut.UpdateAiConfigAsync(userId, dto);

            // Assert
            _mockSystemConfigRepository.Verify(x => x.SaveAsync(It.Is<SystemConfigs>(c => c.ConfigKey == "ActiveAiProvider" && c.ConfigValue == "Gemini")), Times.Once);
            _mockSystemConfigRepository.Verify(x => x.SaveAsync(It.Is<SystemConfigs>(c => c.ConfigKey == "AiRateLimit" && c.ConfigValue == "90")), Times.Once);
            _mockSystemConfigRepository.Verify(x => x.SaveAsync(It.Is<SystemConfigs>(c => c.ConfigKey == "AiApiKey_Gemini" && c.ConfigValue == "sk-new-api-key-999")), Times.Once);
        }

        [Fact]
        public async Task UpdateAiConfigAsync_ShouldNotSaveApiKey_WhenApiKeyContainsMaskedStars()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var mockProvider = new Mock<IAiProvider>();
            mockProvider.Setup(p => p.ProviderName).Returns("Gemini");

            _mockProviderFactory
                .Setup(f => f.GetProvider("Gemini"))
                .Returns(mockProvider.Object);

            var dto = new UpdateAiConfigRequestDto
            {
                ProviderName = "Gemini",
                RequestsPerMinute = 60,
                ApiKey = "sk-***1234" // Masked key preview string sent back by client
            };

            // Act
            await _sut.UpdateAiConfigAsync(userId, dto);

            // Assert
            _mockSystemConfigRepository.Verify(x => x.SaveAsync(It.Is<SystemConfigs>(c => c.ConfigKey.StartsWith("AiApiKey_"))), Times.Never);
        }

        [Fact]
        public async Task TestConnectionAsync_ShouldReturnSuccess_WhenProviderReturnsResponse()
        {
            // Arrange
            var mockProvider = new Mock<IAiProvider>();
            mockProvider
                .Setup(p => p.GenerateTextAsync("Test prompt", It.IsAny<string>()))
                .ReturnsAsync("OK");

            _mockProviderFactory
                .Setup(f => f.GetProvider("Gemini"))
                .Returns(mockProvider.Object);

            // Act
            var result = await _sut.TestConnectionAsync("Gemini", "Test prompt");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.ResponseText.Should().Be("OK");
        }

        [Fact]
        public async Task TestConnectionAsync_ShouldReturnFailure_WhenProviderThrowsException()
        {
            // Arrange
            var mockProvider = new Mock<IAiProvider>();
            mockProvider
                .Setup(p => p.GenerateTextAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("API Key Invalid"));

            _mockProviderFactory
                .Setup(f => f.GetProvider("Gemini"))
                .Returns(mockProvider.Object);

            // Act
            var result = await _sut.TestConnectionAsync("Gemini", "Test prompt");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("API Key Invalid");
        }
    }
}

