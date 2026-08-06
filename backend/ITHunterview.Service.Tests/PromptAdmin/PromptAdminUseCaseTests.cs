using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.PromptAdmin;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.UseCase;
using Moq;
using Xunit;
using FluentAssertions;

namespace ITHunterview.Service.Tests.PromptAdmin
{
    public class PromptAdminUseCaseTests
    {
        private readonly Mock<IPromptAdminRepository> _mockPromptRepository;
        private readonly PromptAdminUseCase _sut;

        public PromptAdminUseCaseTests()
        {
            _mockPromptRepository = new Mock<IPromptAdminRepository>();
            _sut = new PromptAdminUseCase(_mockPromptRepository.Object);
        }

        [Fact]
        public async Task GetPagedPromptsAsync_ShouldReturnPagedResult_WhenPromptsExist()
        {
            // Arrange
            var prompts = new List<Prompts>
            {
                new Prompts
                {
                    Id = Guid.NewGuid(),
                    PromptKey = "general_chat",
                    Description = "General Chat Prompt",
                    CreatedAt = DateTime.UtcNow,
                    Versions = new List<PromptVersions>
                    {
                        new PromptVersions { VersionTag = "v1.0" }
                    }
                }
            };

            _mockPromptRepository
                .Setup(r => r.GetPagedPromptsAsync(1, 10))
                .ReturnsAsync((prompts, 1));

            // Act
            var result = await _sut.GetPagedPromptsAsync(1, 10);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(1);
            result.Items.Should().HaveCount(1);
            result.Items[0].PromptKey.Should().Be("general_chat");
            result.Items[0].ActiveVersionTag.Should().Be("v1.0");
        }

        [Fact]
        public async Task GetPromptHistoryAsync_ShouldReturnPromptDto_WhenPromptExists()
        {
            // Arrange
            var promptId = Guid.NewGuid();
            var prompt = new Prompts
            {
                Id = promptId,
                PromptKey = "MOCK_INTERVIEW_START",
                Description = "Interview prompt",
                CreatedAt = DateTime.UtcNow,
                Versions = new List<PromptVersions>()
            };

            _mockPromptRepository
                .Setup(r => r.GetPromptWithHistoryAsync(promptId))
                .ReturnsAsync(prompt);

            // Act
            var result = await _sut.GetPromptHistoryAsync(promptId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(promptId);
            result.PromptKey.Should().Be("MOCK_INTERVIEW_START");
        }

        [Fact]
        public async Task GetPromptHistoryAsync_ShouldThrowKeyNotFoundException_WhenPromptDoesNotExist()
        {
            // Arrange
            var promptId = Guid.NewGuid();
            _mockPromptRepository
                .Setup(r => r.GetPromptWithHistoryAsync(promptId))
                .ReturnsAsync((Prompts?)null);

            // Act
            Func<Task> act = async () => await _sut.GetPromptHistoryAsync(promptId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*Prompt not found*");
        }

        [Fact]
        public async Task GetPromptVersionAsync_ShouldReturnVersionDto_WhenVersionExists()
        {
            // Arrange
            var versionId = Guid.NewGuid();
            var version = new PromptVersions
            {
                Id = versionId,
                PromptId = Guid.NewGuid(),
                VersionTag = "v1.2",
                Content = "Sample prompt content",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _mockPromptRepository
                .Setup(r => r.GetPromptVersionAsync(versionId))
                .ReturnsAsync(version);

            // Act
            var result = await _sut.GetPromptVersionAsync(versionId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(versionId);
            result.VersionTag.Should().Be("v1.2");
        }

        [Fact]
        public async Task GetPromptVersionAsync_ShouldThrowKeyNotFoundException_WhenVersionDoesNotExist()
        {
            // Arrange
            var versionId = Guid.NewGuid();
            _mockPromptRepository
                .Setup(r => r.GetPromptVersionAsync(versionId))
                .ReturnsAsync((PromptVersions?)null);

            // Act
            Func<Task> act = async () => await _sut.GetPromptVersionAsync(versionId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*Prompt version not found*");
        }

        [Fact]
        public async Task CreatePromptVersionAsync_ShouldThrowKeyNotFoundException_WhenPromptNotFound()
        {
            // Arrange
            var promptId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var dto = new CreatePromptVersionDto
            {
                VersionTag = "v2.0",
                Content = "Test content",
                ModelConfig = "{}"
            };

            _mockPromptRepository
                .Setup(r => r.GetPromptWithHistoryAsync(promptId))
                .ReturnsAsync((Prompts?)null);

            // Act
            Func<Task> act = async () => await _sut.CreatePromptVersionAsync(promptId, dto, adminId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*Prompt not found*");
        }

        [Fact]
        public async Task CreatePromptVersionAsync_ShouldThrowArgumentException_WhenMissingRequiredPlaceholders()
        {
            // Arrange
            var promptId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var prompt = new Prompts
            {
                Id = promptId,
                PromptKey = "JD_MATCHING_PROMPT", // Requires [CV_TEXT] and [PARSED_JD_REQUIREMENTS]
                Versions = new List<PromptVersions>()
            };

            _mockPromptRepository
                .Setup(r => r.GetPromptWithHistoryAsync(promptId))
                .ReturnsAsync(prompt);

            var dto = new CreatePromptVersionDto
            {
                VersionTag = "v2.0",
                Content = "Content without required placeholders",
                ModelConfig = "{}"
            };

            // Act
            Func<Task> act = async () => await _sut.CreatePromptVersionAsync(promptId, dto, adminId);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Missing required placeholders in prompt content*");
        }

        [Fact]
        public async Task CreatePromptVersionAsync_ShouldThrowArgumentException_WhenManagedAnalysisPromptMakeActiveIsTrue()
        {
            // Arrange
            var promptId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var prompt = new Prompts
            {
                Id = promptId,
                PromptKey = "CV_ANALYSIS_SYSTEM", // Managed analysis prompt key
                Versions = new List<PromptVersions>()
            };

            _mockPromptRepository
                .Setup(r => r.GetPromptWithHistoryAsync(promptId))
                .ReturnsAsync(prompt);

            var dto = new CreatePromptVersionDto
            {
                VersionTag = "v2.0",
                Content = "Valid system prompt content",
                ModelConfig = "{\"contract\":\"v1\",\"role\":\"system\"}",
                MakeActive = true // Must throw error!
            };

            // Act
            Func<Task> act = async () => await _sut.CreatePromptVersionAsync(promptId, dto, adminId);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Analysis prompt versions must be activated through their system/user prompt-pair activation endpoint*");
        }
    }
}

