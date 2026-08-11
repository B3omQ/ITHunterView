using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Config;
using ITHunterview.Service.Constant.Prompts;
using ITHunterview.Service.DTOs.PromptAdmin;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Exceptions;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Service.Matching;
using ITHunterview.Service.UseCase;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace ITHunterview.Service.Tests.PromptAdmin;

public class CvAnalysisPromptManagementTests
{
    [Fact]
    public async Task CreateVersion_WhenCvUserPromptMissesPlaceholder_RejectsDraft()
    {
        var repository = new Mock<IPromptAdminRepository>();
        var promptId = Guid.NewGuid();
        repository.Setup(x => x.GetPromptWithHistoryAsync(promptId))
            .ReturnsAsync(new Prompts { Id = promptId, PromptKey = CvAnalysisPromptContract.UserPromptKey });

        var useCase = new PromptAdminUseCase(repository.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => useCase.CreatePromptVersionAsync(
            promptId,
            new CreatePromptVersionDto
            {
                VersionTag = "v1.0.1",
                Content = "Extract this CV.",
                ModelConfig = "{\"contract\":\"cv-analysis/v1\",\"role\":\"user\"}",
                MakeActive = false
            },
            Guid.NewGuid()));

        Assert.Contains(CvAnalysisPromptContract.UserPlaceholder, exception.Message);
        repository.Verify(x => x.CreatePromptVersionAsync(It.IsAny<PromptVersions>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task CreateVersion_WhenCvPromptWouldActivateIndividually_RejectsRequest()
    {
        var repository = new Mock<IPromptAdminRepository>();
        var promptId = Guid.NewGuid();
        repository.Setup(x => x.GetPromptWithHistoryAsync(promptId))
            .ReturnsAsync(new Prompts { Id = promptId, PromptKey = CvAnalysisPromptContract.UserPromptKey });

        var useCase = new PromptAdminUseCase(repository.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => useCase.CreatePromptVersionAsync(
            promptId,
            new CreatePromptVersionDto
            {
                VersionTag = "v1.0.1",
                Content = $"Extract CV: {CvAnalysisPromptContract.UserPlaceholder}",
                ModelConfig = "{\"contract\":\"cv-analysis/v1\",\"role\":\"user\"}",
                MakeActive = true
            },
            Guid.NewGuid()));

        Assert.Contains("prompt-pair", exception.Message);
        repository.Verify(x => x.CreatePromptVersionAsync(It.IsAny<PromptVersions>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task ActivatePair_WhenContractsMatch_ActivatesBothVersionsAtomically()
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
                Prompt = new Prompts { Id = systemPromptId, PromptKey = CvAnalysisPromptContract.SystemPromptKey },
                Content = "Keep evidence grounded.",
                ModelConfig = "{\"contract\":\"cv-analysis/v1\",\"role\":\"system\"}"
            });
        repository.Setup(x => x.GetPromptVersionAsync(userVersionId))
            .ReturnsAsync(new PromptVersions
            {
                Id = userVersionId,
                PromptId = userPromptId,
                Prompt = new Prompts { Id = userPromptId, PromptKey = CvAnalysisPromptContract.UserPromptKey },
                Content = $"Parse the CV. {CvAnalysisPromptContract.UserPlaceholder}",
                ModelConfig = "{\"contract\":\"cv-analysis/v1\",\"role\":\"user\"}"
            });

        var useCase = new PromptAdminUseCase(repository.Object);

        await useCase.ActivateCvAnalysisPromptPairAsync(systemVersionId, userVersionId, Guid.NewGuid());

        repository.Verify(x => x.ActivatePromptPairAsync(systemPromptId, systemVersionId, userPromptId, userVersionId), Times.Once);
    }

    [Fact]
    public async Task CreateVersion_WhenCvSystemContainsKnownSchema_StoresSemanticOnlyContent()
    {
        var repository = new Mock<IPromptAdminRepository>();
        var promptId = Guid.NewGuid();
        repository.Setup(x => x.GetPromptWithHistoryAsync(promptId))
            .ReturnsAsync(new Prompts
            {
                Id = promptId,
                PromptKey = CvAnalysisPromptContract.SystemPromptKey
            });
        repository.Setup(x => x.CreatePromptVersionAsync(It.IsAny<PromptVersions>(), false))
            .ReturnsAsync((PromptVersions version, bool _) => version);
        var useCase = new PromptAdminUseCase(repository.Object);

        await useCase.CreatePromptVersionAsync(
            promptId,
            new CreatePromptVersionDto
            {
                VersionTag = "v3.1.0",
                Content = CvAnalysisOutputSchema.ComposeSystemPrompt("Keep semantic evidence rules."),
                ModelConfig = "{\"contract\":\"cv-analysis/v3\",\"role\":\"system\"}",
                MakeActive = false
            },
            Guid.NewGuid());

        repository.Verify(x => x.CreatePromptVersionAsync(
            It.Is<PromptVersions>(version =>
                version.Content == "Keep semantic evidence rules." &&
                !version.Content.Contains(CvAnalysisOutputSchema.BeginMarker, StringComparison.Ordinal)),
            false), Times.Once);
    }

    [Fact]
    public async Task CreateVersion_WhenCvSystemContainsHistoricalV1Schema_PreservesSemanticInstructions()
    {
        var repository = new Mock<IPromptAdminRepository>();
        var promptId = Guid.NewGuid();
        repository.Setup(x => x.GetPromptWithHistoryAsync(promptId))
            .ReturnsAsync(new Prompts
            {
                Id = promptId,
                PromptKey = CvAnalysisPromptContract.SystemPromptKey
            });
        PromptVersions? captured = null;
        repository.Setup(x => x.CreatePromptVersionAsync(It.IsAny<PromptVersions>(), false))
            .Callback<PromptVersions, bool>((version, _) => captured = version)
            .ReturnsAsync((PromptVersions version, bool _) => version);

        var historicalContent = ReadHistoricalCvPrompt("cv_system");
        var useCase = new PromptAdminUseCase(repository.Object);

        await useCase.CreatePromptVersionAsync(
            promptId,
            new CreatePromptVersionDto
            {
                VersionTag = "v1-semantic-copy",
                Content = historicalContent,
                ModelConfig = "{\"contract\":\"cv-analysis/v1\",\"role\":\"system\"}"
            },
            Guid.NewGuid());

        captured.Should().NotBeNull();
        Assert.Equal(
            CvAnalysisOutputSchema.NormalizeManagedContent(historicalContent).SemanticContent,
            captured!.Content);
        Assert.Contains("CRITICAL RULE", captured.Content, StringComparison.Ordinal);
        Assert.Contains("If any information is missing", captured.Content, StringComparison.Ordinal);
        Assert.Contains("Ensure the output is 100% valid JSON.", captured.Content, StringComparison.Ordinal);
        Assert.DoesNotContain(CvAnalysisOutputSchema.BeginMarker, captured.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateVersion_WhenCvUserTemplateIsValid_PersistsExactlyOneCvPlaceholder()
    {
        var repository = new Mock<IPromptAdminRepository>();
        var promptId = Guid.NewGuid();
        repository.Setup(x => x.GetPromptWithHistoryAsync(promptId))
            .ReturnsAsync(new Prompts
            {
                Id = promptId,
                PromptKey = CvAnalysisPromptContract.UserPromptKey
            });
        PromptVersions? captured = null;
        repository.Setup(x => x.CreatePromptVersionAsync(It.IsAny<PromptVersions>(), false))
            .Callback<PromptVersions, bool>((version, _) => captured = version)
            .ReturnsAsync((PromptVersions version, bool _) => version);

        const string content = "Extract the CV without changing its wording.\n--- CV TEXT ---\n[CV_TEXT]\n--- END CV TEXT ---";
        var useCase = new PromptAdminUseCase(repository.Object);

        await useCase.CreatePromptVersionAsync(
            promptId,
            new CreatePromptVersionDto
            {
                VersionTag = "v3.1.0",
                Content = content,
                ModelConfig = "{\"contract\":\"cv-analysis/v3\",\"role\":\"user\"}"
            },
            Guid.NewGuid());

        captured.Should().NotBeNull();
        Assert.Equal(content, captured!.Content);
        Assert.Equal(1, CountOccurrences(captured.Content, CvAnalysisPromptContract.UserPlaceholder));
    }

    [Fact]
    public async Task CreateVersion_WhenCvSchemaIsUnknown_RejectsBeforeRepositoryWrite()
    {
        var repository = new Mock<IPromptAdminRepository>();
        var promptId = Guid.NewGuid();
        repository.Setup(x => x.GetPromptWithHistoryAsync(promptId))
            .ReturnsAsync(new Prompts
            {
                Id = promptId,
                PromptKey = CvAnalysisPromptContract.SystemPromptKey
            });
        var useCase = new PromptAdminUseCase(repository.Object);

        var action = () => useCase.CreatePromptVersionAsync(
            promptId,
            new CreatePromptVersionDto
            {
                VersionTag = "unknown-schema",
                Content = "Keep the extraction rules.\n{\"schema_version\":\"cv-analysis/v9\",\"verbatim_sections\":{},\"matching_metrics\":{}}",
                ModelConfig = "{\"contract\":\"cv-analysis/v3\",\"role\":\"system\"}"
            },
            Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<ArgumentException>(action);
        Assert.Contains("CV_ANALYSIS_PROMPT_SCHEMA_MUTATION", exception.Message, StringComparison.Ordinal);
        repository.Verify(x => x.CreatePromptVersionAsync(It.IsAny<PromptVersions>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task ActivatePair_WhenSelectedCvSchemaIsMutated_RejectsWithoutRepositoryWrite()
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
                Prompt = new Prompts { Id = systemPromptId, PromptKey = CvAnalysisPromptContract.SystemPromptKey },
                Content = CvAnalysisOutputSchema.LockedBlock.Replace(
                    "\"matching_evidence\"",
                    "\"matching_evidence_changed\"",
                    StringComparison.Ordinal),
                ModelConfig = "{\"contract\":\"cv-analysis/v3\",\"role\":\"system\"}"
            });
        repository.Setup(x => x.GetPromptVersionAsync(userVersionId))
            .ReturnsAsync(new PromptVersions
            {
                Id = userVersionId,
                PromptId = userPromptId,
                Prompt = new Prompts { Id = userPromptId, PromptKey = CvAnalysisPromptContract.UserPromptKey },
                Content = $"Parse {CvAnalysisPromptContract.UserPlaceholder}",
                ModelConfig = "{\"contract\":\"cv-analysis/v3\",\"role\":\"user\"}"
            });
        var useCase = new PromptAdminUseCase(repository.Object);

        var action = () => useCase.ActivateCvAnalysisPromptPairAsync(
            systemVersionId, userVersionId, Guid.NewGuid());

        await Assert.ThrowsAsync<ArgumentException>(action);
        repository.Verify(x => x.ActivatePromptPairAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ActivatePair_WhenSelectedCvSchemaIsUnknown_RejectsBeforeRepositoryWrite()
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
                Prompt = new Prompts { Id = systemPromptId, PromptKey = CvAnalysisPromptContract.SystemPromptKey },
                Content = "Rules.\n{\"schema_version\":\"cv-analysis/v9\",\"verbatim_sections\":{},\"matching_metrics\":{}}",
                ModelConfig = "{\"contract\":\"cv-analysis/v3\",\"role\":\"system\"}"
            });
        repository.Setup(x => x.GetPromptVersionAsync(userVersionId))
            .ReturnsAsync(new PromptVersions
            {
                Id = userVersionId,
                PromptId = userPromptId,
                Prompt = new Prompts { Id = userPromptId, PromptKey = CvAnalysisPromptContract.UserPromptKey },
                Content = $"Parse {CvAnalysisPromptContract.UserPlaceholder}",
                ModelConfig = "{\"contract\":\"cv-analysis/v3\",\"role\":\"user\"}"
            });
        var useCase = new PromptAdminUseCase(repository.Object);

        var action = () => useCase.ActivateCvAnalysisPromptPairAsync(
            systemVersionId, userVersionId, Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<ArgumentException>(action);
        Assert.Contains("CV_ANALYSIS_PROMPT_SCHEMA_MUTATION", exception.Message, StringComparison.Ordinal);
        repository.Verify(x => x.ActivatePromptPairAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ActivatePair_WhenHistoricalV1ContentsAndContractsMatch_ActivatesPair()
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
                Prompt = new Prompts { Id = systemPromptId, PromptKey = CvAnalysisPromptContract.SystemPromptKey },
                Content = ReadHistoricalCvPrompt("cv_system"),
                ModelConfig = "{\"contract\":\"cv-analysis/v1\",\"role\":\"system\"}"
            });
        repository.Setup(x => x.GetPromptVersionAsync(userVersionId))
            .ReturnsAsync(new PromptVersions
            {
                Id = userVersionId,
                PromptId = userPromptId,
                Prompt = new Prompts { Id = userPromptId, PromptKey = CvAnalysisPromptContract.UserPromptKey },
                Content = ReadHistoricalCvPrompt("cv_user"),
                ModelConfig = "{\"contract\":\"cv-analysis/v1\",\"role\":\"user\"}"
            });
        var useCase = new PromptAdminUseCase(repository.Object);

        await useCase.ActivateCvAnalysisPromptPairAsync(systemVersionId, userVersionId, Guid.NewGuid());

        repository.Verify(x => x.ActivatePromptPairAsync(
            systemPromptId, systemVersionId, userPromptId, userVersionId), Times.Once);
    }

    [Fact]
    public async Task ActivatePair_WhenContractsDiffer_RejectsWithoutChangingActiveVersions()
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
                Prompt = new Prompts { Id = systemPromptId, PromptKey = CvAnalysisPromptContract.SystemPromptKey },
                Content = "System rules.",
                ModelConfig = "{\"contract\":\"cv-analysis/v1\",\"role\":\"system\"}"
            });
        repository.Setup(x => x.GetPromptVersionAsync(userVersionId))
            .ReturnsAsync(new PromptVersions
            {
                Id = userVersionId,
                PromptId = userPromptId,
                Prompt = new Prompts { Id = userPromptId, PromptKey = CvAnalysisPromptContract.UserPromptKey },
                Content = $"Parse {CvAnalysisPromptContract.UserPlaceholder}",
                ModelConfig = "{\"contract\":\"cv-analysis/v2\",\"role\":\"user\"}"
            });
        var useCase = new PromptAdminUseCase(repository.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ActivateCvAnalysisPromptPairAsync(systemVersionId, userVersionId, Guid.NewGuid()));

        Assert.Contains("same contract", exception.Message, StringComparison.Ordinal);
        repository.Verify(x => x.ActivatePromptPairAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetCvAnalysisPromptPair_ReturnsBothPromptHistoriesByFixedKeys()
    {
        var repository = new Mock<IPromptAdminRepository>();
        repository.Setup(x => x.GetPromptWithHistoryByKeyAsync(CvAnalysisPromptContract.SystemPromptKey))
            .ReturnsAsync(new Prompts
            {
                Id = Guid.NewGuid(),
                PromptKey = CvAnalysisPromptContract.SystemPromptKey,
                Versions = { new PromptVersions { Id = Guid.NewGuid(), VersionTag = "v1.0.0", IsActive = true } }
            });
        repository.Setup(x => x.GetPromptWithHistoryByKeyAsync(CvAnalysisPromptContract.UserPromptKey))
            .ReturnsAsync(new Prompts
            {
                Id = Guid.NewGuid(),
                PromptKey = CvAnalysisPromptContract.UserPromptKey,
                Versions = { new PromptVersions { Id = Guid.NewGuid(), VersionTag = "v1.0.0", IsActive = true } }
            });

        var useCase = new PromptAdminUseCase(repository.Object);

        var result = await useCase.GetCvAnalysisPromptPairAsync();

        Assert.Equal(CvAnalysisPromptContract.SystemPromptKey, result.SystemPrompt.PromptKey);
        Assert.Equal(CvAnalysisPromptContract.UserPromptKey, result.UserPrompt.PromptKey);
        Assert.Equal("v1.0.0", result.SystemPrompt.ActiveVersionTag);
        Assert.Equal("v1.0.0", result.UserPrompt.ActiveVersionTag);
    }

    [Fact]
    public async Task ExtractParsedDataFromRawTextAsync_UsesActiveCvPromptPair()
    {
        var aiService = new Mock<IAiService>();
        aiService.Setup(x => x.GetActiveProviderNameAsync()).ReturnsAsync("Gemini");
        aiService
            .Setup(x => x.GenerateTextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<AiGenerationOptions>(),
                It.IsAny<CancellationToken>(),
                "CV_EXTRACTION"))
            .ReturnsAsync("{\"schema_version\":\"cv-analysis/v2\"}");

        var promptService = new Mock<IPromptManagementService>();
        var responseValidator = new Mock<ICvAnalysisResponseValidator>();
        responseValidator
            .Setup(x => x.ValidateAndCanonicalize(It.IsAny<string>()))
            .Returns(CvAnalysisValidationResult.Complete("{\"canonical\":true}", EmptyCoverage()));
        promptService
            .Setup(x => x.GetActivePromptPairSnapshotAsync(
                CvAnalysisPromptContract.SystemPromptKey,
                CvAnalysisPromptContract.UserPromptKey,
                default))
            .ReturnsAsync(new PromptPairSnapshotDto
            {
                Contract = CvAnalysisPromptContract.ContractV1,
                System = new PromptSnapshotDto
                {
                    PromptKey = CvAnalysisPromptContract.SystemPromptKey,
                    VersionTag = "v1.0.0",
                    Content = "system from database"
                },
                User = new PromptSnapshotDto
                {
                    PromptKey = CvAnalysisPromptContract.UserPromptKey,
                    VersionTag = "v1.0.0",
                    Content = $"user template {CvAnalysisPromptContract.UserPlaceholder}"
                }
            });

        var service = new CvTextExtractorService(
            NullLogger<CvTextExtractorService>.Instance,
            Mock.Of<System.Net.Http.IHttpClientFactory>(),
            Options.Create(new AiSettings()),
            aiService.Object,
            Mock.Of<ISystemConfigRepository>(),
            promptService.Object,
            responseValidator.Object);

        var result = await service.ExtractParsedDataFromRawTextAsync("Jane Doe\nC# developer\n", "pasted_text", "resume.txt");

        Assert.Equal("{\"canonical\":true}", result);
        promptService.Verify(x => x.GetActivePromptPairSnapshotAsync(
            CvAnalysisPromptContract.SystemPromptKey,
            CvAnalysisPromptContract.UserPromptKey,
            default), Times.Once);
        aiService.Verify(x => x.GenerateTextAsync(
            It.Is<string>(prompt => prompt.StartsWith("user template {") &&
                                    prompt.Contains("\"raw_text\":\"Jane Doe\\nC# developer\\n\"") &&
                                    prompt.Contains("\"source_type\":\"pasted_text\"") &&
                                    prompt.Contains("\"file_name\":\"resume.txt\"")),
            It.Is<string>(system =>
                system.Contains(CvAnalysisOutputSchema.BeginMarker, StringComparison.Ordinal) &&
                system.Contains("\"schema_version\": \"cv-analysis/v2\"", StringComparison.Ordinal)),
            "Gemini",
            It.Is<AiGenerationOptions>(options =>
                options.ProfileId == "cv-analysis-json/v1" &&
                options.ResponseMimeType == "application/json"),
            It.IsAny<CancellationToken>(),
            "CV_EXTRACTION"), Times.Once);
        responseValidator.Verify(x => x.ValidateAndCanonicalize("{\"schema_version\":\"cv-analysis/v2\"}"), Times.Once);
    }

    [Fact]
    public async Task ExtractParsedDataFromRawTextAsync_WhenTypedValidationFails_PreservesFailureCode()
    {
        var aiService = new Mock<IAiService>();
        aiService.Setup(x => x.GetActiveProviderNameAsync()).ReturnsAsync("Gemini");
        aiService
            .Setup(x => x.GenerateTextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<AiGenerationOptions>(),
                It.IsAny<CancellationToken>(),
                "CV_EXTRACTION"))
            .ReturnsAsync("{\"schema_version\":\"cv-analysis/v2\"}");

        var promptService = new Mock<IPromptManagementService>();
        promptService
            .Setup(x => x.GetActivePromptPairSnapshotAsync(
                CvAnalysisPromptContract.SystemPromptKey,
                CvAnalysisPromptContract.UserPromptKey,
                default))
            .ReturnsAsync(new PromptPairSnapshotDto
            {
                Contract = "cv-analysis/v2",
                System = new PromptSnapshotDto
                {
                    PromptKey = CvAnalysisPromptContract.SystemPromptKey,
                    VersionTag = "v2.0",
                    Content = "system"
                },
                User = new PromptSnapshotDto
                {
                    PromptKey = CvAnalysisPromptContract.UserPromptKey,
                    VersionTag = "v2.0",
                    Content = $"parse {CvAnalysisPromptContract.UserPlaceholder}"
                }
            });

        var responseValidator = new Mock<ICvAnalysisResponseValidator>();
        responseValidator
            .Setup(x => x.ValidateAndCanonicalize(It.IsAny<string>()))
            .Returns(CvAnalysisValidationResult.Invalid("CV_ANALYSIS_INVALID_JSON", "JSON_PARSE_FAILED", "$"));

        var service = new CvTextExtractorService(
            NullLogger<CvTextExtractorService>.Instance,
            Mock.Of<System.Net.Http.IHttpClientFactory>(),
            Options.Create(new AiSettings()),
            aiService.Object,
            Mock.Of<ISystemConfigRepository>(),
            promptService.Object,
            responseValidator.Object);

        var exception = await Assert.ThrowsAsync<CvAnalysisValidationException>(() =>
            service.ExtractParsedDataFromRawTextAsync("Jane Doe\nC# developer\n", "pasted_text", "resume.txt"));

        Assert.Equal("CV_ANALYSIS_INVALID_JSON", exception.FailureCode);
        promptService.Verify(x => x.GetActivePromptPairSnapshotAsync(
            CvAnalysisPromptContract.SystemPromptKey,
            CvAnalysisPromptContract.UserPromptKey,
            default), Times.Once);
        aiService.Verify(x => x.GenerateTextAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<AiGenerationOptions>(), It.IsAny<CancellationToken>(), "CV_EXTRACTION"), Times.Exactly(2));
    }

    private static CvAnalysisCoverage EmptyCoverage() => new(
        0, 0, 0,
        0, 0, 0,
        0, 0, 0,
        false, false, false, false);

    private static string ReadHistoricalCvPrompt(string tag)
    {
        var migrationType = typeof(ITHunterviewContext).Assembly
            .GetTypes()
            .Single(type => type.GetCustomAttribute<MigrationAttribute>()?.Id
                .EndsWith("_AddCvAnalysisPromptManagement", StringComparison.Ordinal) == true);
        var migration = Activator.CreateInstance(migrationType);
        var method = migrationType.GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic);
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        method!.Invoke(migration, [builder]);
        var sql = string.Join("\n", builder.Operations.OfType<SqlOperation>().Select(operation => operation.Sql));
        var delimiter = $"${tag}$";
        var start = sql.IndexOf(delimiter, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0);
        start += delimiter.Length;
        var end = sql.IndexOf(delimiter, start, StringComparison.Ordinal);
        end.Should().BeGreaterThan(start);
        return sql[start..end];
    }

    private static int CountOccurrences(string value, string pattern)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += pattern.Length;
        }

        return count;
    }
}
