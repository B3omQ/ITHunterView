using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Constant.Prompts;
using ITHunterview.Service.DTOs.PromptAdmin;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.UseCase;
using Moq;

namespace ITHunterview.Service.Tests.PromptAdmin;

public sealed class JdMatchingPromptManagementTests
{
    private static readonly Guid PromptId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid VersionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task CreateVersion_WhenCopyingActiveV2_StoresSemanticOnlyContent()
    {
        var repository = CreateRepositoryWithPrompt();
        PromptVersions? captured = null;
        repository.Setup(x => x.CreatePromptVersionAsync(It.IsAny<PromptVersions>(), false))
            .Callback<PromptVersions, bool>((version, _) => captured = version)
            .ReturnsAsync((PromptVersions version, bool _) => version);

        var fixture = ReadActivePrompt();
        var useCase = new PromptAdminUseCase(repository.Object);

        await useCase.CreatePromptVersionAsync(
            PromptId,
            new CreatePromptVersionDto
            {
                VersionTag = "semantic-copy",
                Content = fixture,
                ModelConfig = null,
                MakeActive = false
            },
            Guid.NewGuid());

        captured.Should().NotBeNull();
        captured!.Content.Should().Be(JdMatchingOutputSchema.NormalizeManagedContent(fixture).SemanticContent);
        captured.Content.Should().NotContain(JdMatchingOutputSchema.BeginMarker);
        captured.Content.Should().NotContain("SCHEMA OUTPUT BẮT BUỘC");
        captured.Content.Should().NotContain("Chỉ trả về JSON hợp lệ. Bắt đầu bằng { và kết thúc bằng }.");
        captured.Content.Should().Contain("HANDLER SCORING RULES (MANDATORY — follow exactly):");
    }

    [Fact]
    public async Task CreateVersion_PreservesSemanticSentenceThatMentionsScores()
    {
        var repository = CreateRepositoryWithPrompt();
        PromptVersions? captured = null;
        repository.Setup(x => x.CreatePromptVersionAsync(It.IsAny<PromptVersions>(), false))
            .Callback<PromptVersions, bool>((version, _) => captured = version)
            .ReturnsAsync((PromptVersions version, bool _) => version);

        const string content = "Use scores only as a result of the handler rules.\n[CV_TEXT]\n[PARSED_JD_REQUIREMENTS]";
        var useCase = new PromptAdminUseCase(repository.Object);

        await useCase.CreatePromptVersionAsync(
            PromptId,
            new CreatePromptVersionDto { VersionTag = "semantic", Content = content, ModelConfig = null },
            Guid.NewGuid());

        captured!.Content.Should().Be(content);
    }

    [Fact]
    public async Task CreateVersion_WhenSchemaFieldIsChanged_RejectsBeforeRepositoryWrite()
    {
        var repository = CreateRepositoryWithPrompt();
        var useCase = new PromptAdminUseCase(repository.Object);
        var mutated = ReadActivePrompt().Replace("\"criticalGaps\"", "\"criticalGapsMutated\"", StringComparison.Ordinal);

        var action = () => useCase.CreatePromptVersionAsync(
            PromptId,
            new CreatePromptVersionDto
            {
                VersionTag = "invalid",
                Content = mutated,
                ModelConfig = null
            },
            Guid.NewGuid());

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*MATCHING_PROMPT_SCHEMA_MUTATION*");
        repository.Verify(x => x.CreatePromptVersionAsync(It.IsAny<PromptVersions>(), It.IsAny<bool>()), Times.Never);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"contract\":\"historical-contract\"}")]
    public async Task CreateVersion_WhenMatchingModelConfigIsNonBlank_RejectsBeforeWrite(string modelConfig)
    {
        var repository = CreateRepositoryWithPrompt();
        var useCase = new PromptAdminUseCase(repository.Object);

        var action = () => useCase.CreatePromptVersionAsync(
            PromptId,
            new CreatePromptVersionDto
            {
                VersionTag = "invalid-config",
                Content = "Follow the matching rules.\n[CV_TEXT]\n[PARSED_JD_REQUIREMENTS]",
                ModelConfig = modelConfig
            },
            Guid.NewGuid());

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*JD_MATCHING_MODEL_CONFIG_NOT_ALLOWED*");
        repository.Verify(
            x => x.CreatePromptVersionAsync(It.IsAny<PromptVersions>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateVersion_WhenMatchingModelConfigIsBlank_PersistsNull()
    {
        var repository = CreateRepositoryWithPrompt();
        PromptVersions? captured = null;
        repository.Setup(x => x.CreatePromptVersionAsync(It.IsAny<PromptVersions>(), false))
            .Callback<PromptVersions, bool>((version, _) => captured = version)
            .ReturnsAsync((PromptVersions version, bool _) => version);

        var useCase = new PromptAdminUseCase(repository.Object);
        await useCase.CreatePromptVersionAsync(
            PromptId,
            new CreatePromptVersionDto
            {
                VersionTag = "config-independent",
                Content = "Follow the matching rules.\n[CV_TEXT]\n[PARSED_JD_REQUIREMENTS]",
                ModelConfig = "   "
            },
            Guid.NewGuid());

        captured.Should().NotBeNull();
        captured!.ModelConfig.Should().BeNull();
    }

    [Fact]
    public async Task CreateVersion_WhenPlaceholderIsMissingOrDuplicated_RejectsBeforeWrite()
    {
        foreach (var content in new[]
                 {
                     "Rules.\n[CV_TEXT]",
                     "Rules.\n[CV_TEXT]\n[CV_TEXT]\n[PARSED_JD_REQUIREMENTS]",
                     "--- START CV ---\n[CV_TEXT]\n--- END CV ---\n--- START CV ---\n[CV_TEXT]\n--- END CV ---\n[PARSED_JD_REQUIREMENTS]"
                 })
        {
            var repository = CreateRepositoryWithPrompt();
            var useCase = new PromptAdminUseCase(repository.Object);

            await Assert.ThrowsAsync<ArgumentException>(() => useCase.CreatePromptVersionAsync(
                PromptId,
                new CreatePromptVersionDto { VersionTag = "bad", Content = content },
                Guid.NewGuid()));

            repository.Verify(x => x.CreatePromptVersionAsync(It.IsAny<PromptVersions>(), It.IsAny<bool>()), Times.Never);
        }
    }

    [Fact]
    public async Task ActivateVersion_ValidatesBeforeRepositoryCanDeactivateCurrentActiveVersion()
    {
        var repository = CreateRepositoryWithPrompt();
        repository.Setup(x => x.GetPromptVersionAsync(VersionId))
            .ReturnsAsync(new PromptVersions
            {
                Id = VersionId,
                PromptId = PromptId,
                Content = ReadActivePrompt().Replace("\"criticalGaps\"", "\"criticalGapsMutated\"", StringComparison.Ordinal),
                ModelConfig = "{}",
                Prompt = new Prompts { Id = PromptId, PromptKey = JdMatchingPromptContract.PromptKey }
            });

        var useCase = new PromptAdminUseCase(repository.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ActivatePromptVersionAsync(
            PromptId,
            VersionId,
            Guid.NewGuid()));

        repository.Verify(x => x.ActivatePromptVersionAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ActivateVersion_AllowsReviewedHistoricalEmbeddedSchema()
    {
        var repository = CreateRepositoryWithPrompt();
        repository.Setup(x => x.GetPromptVersionAsync(VersionId))
            .ReturnsAsync(new PromptVersions
            {
                Id = VersionId,
                PromptId = PromptId,
                Content = ReadActivePrompt(),
                ModelConfig = null,
                Prompt = new Prompts { Id = PromptId, PromptKey = JdMatchingPromptContract.PromptKey }
            });

        var useCase = new PromptAdminUseCase(repository.Object);

        await useCase.ActivatePromptVersionAsync(PromptId, VersionId, Guid.NewGuid());

        repository.Verify(x => x.ActivatePromptVersionAsync(PromptId, VersionId), Times.Once);
    }

    private static Mock<IPromptAdminRepository> CreateRepositoryWithPrompt()
    {
        var repository = new Mock<IPromptAdminRepository>();
        repository.Setup(x => x.GetPromptWithHistoryAsync(PromptId))
            .ReturnsAsync(new Prompts
            {
                Id = PromptId,
                PromptKey = JdMatchingPromptContract.PromptKey
            });
        return repository;
    }

    private static string ReadActivePrompt() => File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Matching", "Fixtures", "jd-matching-v2-active-prompt.txt"));
}
