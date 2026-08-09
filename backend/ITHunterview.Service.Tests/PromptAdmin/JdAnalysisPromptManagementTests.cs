using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Constant.Prompts;
using ITHunterview.Service.DTOs.PromptAdmin;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.UseCase;
using Moq;

namespace ITHunterview.Service.Tests.PromptAdmin;

public class JdAnalysisPromptManagementTests
{
    private const string V6SystemFixtureHash = "fb382fb3745878ed2a4a80f398a0493f4b6f6e637b3c5217230353e1a3724bce";
    private const string V6UserFixtureHash = "e003aad9808ca95daf179d35ac4ecafb9b0ab52064df1dac664526518431ca1b";

    [Fact]
    public void ReviewedV6SemanticFixtures_AreSchemaFreeStableAndSemanticallyComplete()
    {
        var systemContent = ReadFixture("jd-analysis-v6-system-semantic.txt");
        var userContent = ReadFixture("jd-analysis-v6-user-semantic.txt");

        Hash(systemContent).Should().Be(V6SystemFixtureHash);
        Hash(userContent).Should().Be(V6UserFixtureHash);
        Count(systemContent, JdAnalysisPromptContract.UserPlaceholder).Should().Be(0);
        Count(userContent, JdAnalysisPromptContract.UserPlaceholder).Should().Be(1);

        systemContent.Should().Contain("untrusted job data");
        systemContent.Should().Contain("RESPONSIBILITY VERSUS REQUIREMENT");
        systemContent.Should().Contain("physical input field");
        systemContent.Should().Contain("one one_of group");
        systemContent.Should().Contain("separate rows");
        systemContent.Should().Contain("same source_requirement_id");
        systemContent.Should().Contain("intent experience_duration only");
        systemContent.Should().Contain("The backend will not recreate this split");
        systemContent.Should().Contain("Do not merge distinct source clauses");

        systemContent.Contains("schema_version", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
        systemContent.Contains("requirement_groups", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
        systemContent.Contains("OUTPUT CONTRACT", StringComparison.Ordinal).Should().BeFalse();
        systemContent.Contains(JdAnalysisOutputSchema.BeginMarker, StringComparison.Ordinal).Should().BeFalse();
        systemContent.Contains("confidence", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
        systemContent.Contains("seniority_fit", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
        systemContent.Contains("taxonomy", StringComparison.OrdinalIgnoreCase).Should().BeFalse();

        var normalizedSystem = JdAnalysisOutputSchema.NormalizeManagedContent(systemContent);
        normalizedSystem.RemovedKnownSchema.Should().BeFalse();
        normalizedSystem.SemanticContent.Should().Be(systemContent.Trim());
        JdAnalysisOutputSchema.ComposeSystemPrompt(systemContent)
            .Split(JdAnalysisOutputSchema.BeginMarker, StringSplitOptions.None)
            .Length.Should().Be(2);

        JdAnalysisPromptContract.CurrentPairContract.Should().Be("jd-analysis-prompt/v6");
        $"{{\"contract\":\"{JdAnalysisPromptContract.CurrentPairContract}\",\"role\":\"{JdAnalysisPromptContract.SystemRole}\"}}"
            .Should().Be("{\"contract\":\"jd-analysis-prompt/v6\",\"role\":\"system\"}");
        $"{{\"contract\":\"{JdAnalysisPromptContract.CurrentPairContract}\",\"role\":\"{JdAnalysisPromptContract.UserRole}\"}}"
            .Should().Be("{\"contract\":\"jd-analysis-prompt/v6\",\"role\":\"user\"}");
    }

    [Fact]
    public async Task CreateVersion_WhenJdUserPromptMissesPlaceholder_RejectsDraft()
    {
        var repository = new Mock<IPromptAdminRepository>();
        var promptId = Guid.NewGuid();
        repository.Setup(x => x.GetPromptWithHistoryAsync(promptId))
            .ReturnsAsync(new Prompts { Id = promptId, PromptKey = JdAnalysisPromptContract.UserPromptKey });

        var useCase = new PromptAdminUseCase(repository.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => useCase.CreatePromptVersionAsync(
            promptId,
            new CreatePromptVersionDto
            {
                VersionTag = "v3.0.0",
                Content = "Analyze this job.",
                ModelConfig = "{\"contract\":\"jd-analysis/v3\",\"role\":\"user\"}",
                MakeActive = false
            },
            Guid.NewGuid()));

        Assert.Contains(JdAnalysisPromptContract.UserPlaceholder, exception.Message);
        repository.Verify(x => x.CreatePromptVersionAsync(It.IsAny<PromptVersions>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task CreateVersion_WhenJdPromptWouldActivateIndividually_RejectsRequest()
    {
        var repository = new Mock<IPromptAdminRepository>();
        var promptId = Guid.NewGuid();
        repository.Setup(x => x.GetPromptWithHistoryAsync(promptId))
            .ReturnsAsync(new Prompts { Id = promptId, PromptKey = JdAnalysisPromptContract.UserPromptKey });

        var useCase = new PromptAdminUseCase(repository.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => useCase.CreatePromptVersionAsync(
            promptId,
            new CreatePromptVersionDto
            {
                VersionTag = "v3.0.0",
                Content = $"Analyze this job: {JdAnalysisPromptContract.UserPlaceholder}",
                ModelConfig = "{\"contract\":\"jd-analysis/v3\",\"role\":\"user\"}",
                MakeActive = true
            },
            Guid.NewGuid()));

        Assert.Contains("prompt-pair", exception.Message);
        repository.Verify(x => x.CreatePromptVersionAsync(It.IsAny<PromptVersions>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task ActivatePair_WhenJdContractsMatch_ActivatesBothVersionsAtomically()
    {
        var repository = new Mock<IPromptAdminRepository>();
        var systemPromptId = Guid.NewGuid();
        var userPromptId = Guid.NewGuid();
        var systemVersionId = Guid.NewGuid();
        var userVersionId = Guid.NewGuid();

        repository.Setup(x => x.GetPromptVersionAsync(systemVersionId))
            .ReturnsAsync(new PromptVersions
            {
                Id = systemVersionId,
                PromptId = systemPromptId,
                Prompt = new Prompts { Id = systemPromptId, PromptKey = JdAnalysisPromptContract.SystemPromptKey },
                Content = "Keep explicit requirements grounded.",
                ModelConfig = "{\"contract\":\"jd-analysis/v3\",\"role\":\"system\"}"
            });
        repository.Setup(x => x.GetPromptVersionAsync(userVersionId))
            .ReturnsAsync(new PromptVersions
            {
                Id = userVersionId,
                PromptId = userPromptId,
                Prompt = new Prompts { Id = userPromptId, PromptKey = JdAnalysisPromptContract.UserPromptKey },
                Content = $"Parse this job: {JdAnalysisPromptContract.UserPlaceholder}",
                ModelConfig = "{\"contract\":\"jd-analysis/v3\",\"role\":\"user\"}"
            });

        var useCase = new PromptAdminUseCase(repository.Object);

        await useCase.ActivateJdAnalysisPromptPairAsync(systemVersionId, userVersionId, Guid.NewGuid());

        repository.Verify(x => x.ActivatePromptPairAsync(systemPromptId, systemVersionId, userPromptId, userVersionId), Times.Once);
    }

    [Fact]
    public async Task ActivatePair_WhenJdContractsDiffer_RejectsWithoutChangingActiveVersions()
    {
        var repository = new Mock<IPromptAdminRepository>();
        var systemVersionId = Guid.NewGuid();
        var userVersionId = Guid.NewGuid();

        repository.Setup(x => x.GetPromptVersionAsync(systemVersionId))
            .ReturnsAsync(new PromptVersions
            {
                Id = systemVersionId,
                PromptId = Guid.NewGuid(),
                Prompt = new Prompts { PromptKey = JdAnalysisPromptContract.SystemPromptKey },
                Content = "Keep explicit requirements grounded.",
                ModelConfig = "{\"contract\":\"jd-analysis/v3\",\"role\":\"system\"}"
            });
        repository.Setup(x => x.GetPromptVersionAsync(userVersionId))
            .ReturnsAsync(new PromptVersions
            {
                Id = userVersionId,
                PromptId = Guid.NewGuid(),
                Prompt = new Prompts { PromptKey = JdAnalysisPromptContract.UserPromptKey },
                Content = $"Parse this job: {JdAnalysisPromptContract.UserPlaceholder}",
                ModelConfig = "{\"contract\":\"jd-analysis/v2\",\"role\":\"user\"}"
            });

        var useCase = new PromptAdminUseCase(repository.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ActivateJdAnalysisPromptPairAsync(
            systemVersionId, userVersionId, Guid.NewGuid()));

        repository.Verify(x => x.ActivatePromptPairAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task CreateVersion_WhenJdSystemContainsKnownSchema_StoresSemanticOnlyContent()
    {
        var repository = new Mock<IPromptAdminRepository>();
        var promptId = Guid.NewGuid();
        repository.Setup(x => x.GetPromptWithHistoryAsync(promptId))
            .ReturnsAsync(new Prompts { Id = promptId, PromptKey = JdAnalysisPromptContract.SystemPromptKey });
        repository.Setup(x => x.CreatePromptVersionAsync(It.IsAny<PromptVersions>(), false))
            .ReturnsAsync((PromptVersions version, bool _) => version);
        var useCase = new PromptAdminUseCase(repository.Object);

        await useCase.CreatePromptVersionAsync(
            promptId,
            new CreatePromptVersionDto
            {
                VersionTag = "v6.0.0",
                Content = JdAnalysisOutputSchema.ComposeSystemPrompt("Keep explicit requirements grounded."),
                ModelConfig = "{\"contract\":\"jd-analysis-prompt/v6\",\"role\":\"system\"}"
            },
            Guid.NewGuid());

        repository.Verify(x => x.CreatePromptVersionAsync(
            It.Is<PromptVersions>(version =>
                version.Content == "Keep explicit requirements grounded." &&
                !version.Content.Contains(JdAnalysisOutputSchema.BeginMarker, StringComparison.Ordinal)),
            false), Times.Once);
    }

    [Fact]
    public async Task CreateVersion_WhenJdSchemaIsUnknown_RejectsBeforeRepositoryWrite()
    {
        var repository = new Mock<IPromptAdminRepository>();
        var promptId = Guid.NewGuid();
        repository.Setup(x => x.GetPromptWithHistoryAsync(promptId))
            .ReturnsAsync(new Prompts { Id = promptId, PromptKey = JdAnalysisPromptContract.SystemPromptKey });
        var useCase = new PromptAdminUseCase(repository.Object);

        var action = () => useCase.CreatePromptVersionAsync(
            promptId,
            new CreatePromptVersionDto
            {
                VersionTag = "mutated",
                Content = "Rules. { \"schema_version\":\"jd-analysis/v99\", \"matching_metrics\": { \"requirement_groups\": [] } }",
                ModelConfig = "{\"contract\":\"jd-analysis-prompt/v6\",\"role\":\"system\"}"
            },
            Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<ArgumentException>(action);
        Assert.Contains("JD_ANALYSIS_PROMPT_SCHEMA_MUTATION", exception.Message, StringComparison.Ordinal);
        repository.Verify(x => x.CreatePromptVersionAsync(It.IsAny<PromptVersions>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task ActivatePair_WhenSelectedJdSchemaIsMutated_RejectsWithoutRepositoryWrite()
    {
        var repository = new Mock<IPromptAdminRepository>();
        var systemVersionId = Guid.NewGuid();
        var userVersionId = Guid.NewGuid();
        repository.Setup(x => x.GetPromptVersionAsync(systemVersionId))
            .ReturnsAsync(new PromptVersions
            {
                Id = systemVersionId,
                PromptId = Guid.NewGuid(),
                Prompt = new Prompts { PromptKey = JdAnalysisPromptContract.SystemPromptKey },
                Content = JdAnalysisOutputSchema.LockedBlock.Replace(
                    "\"source_requirement_id\"",
                    "\"source_requirement_identifier\"",
                    StringComparison.Ordinal),
                ModelConfig = "{\"contract\":\"jd-analysis-prompt/v6\",\"role\":\"system\"}"
            });
        repository.Setup(x => x.GetPromptVersionAsync(userVersionId))
            .ReturnsAsync(new PromptVersions
            {
                Id = userVersionId,
                PromptId = Guid.NewGuid(),
                Prompt = new Prompts { PromptKey = JdAnalysisPromptContract.UserPromptKey },
                Content = $"Parse {JdAnalysisPromptContract.UserPlaceholder}",
                ModelConfig = "{\"contract\":\"jd-analysis-prompt/v6\",\"role\":\"user\"}"
            });
        var useCase = new PromptAdminUseCase(repository.Object);

        var action = () => useCase.ActivateJdAnalysisPromptPairAsync(
            systemVersionId,
            userVersionId,
            Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<ArgumentException>(action);
        Assert.Contains("JD_ANALYSIS_PROMPT_SCHEMA_MUTATION", exception.Message, StringComparison.Ordinal);
        repository.Verify(x => x.ActivatePromptPairAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    private static string ReadFixture(string name) => File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "PromptAdmin", "Fixtures", name));

    private static string Hash(string content) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();

    private static int Count(string content, string value) =>
        content.Split(value, StringSplitOptions.None).Length - 1;
}
