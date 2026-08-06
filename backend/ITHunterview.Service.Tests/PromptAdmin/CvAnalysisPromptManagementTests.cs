using System;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Config;
using ITHunterview.Service.Constant.Prompts;
using ITHunterview.Service.DTOs.PromptAdmin;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Service.Matching;
using ITHunterview.Service.UseCase;
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
                ModelConfig = "{\"contract\":\"cv-analysis/v1\",\"role\":\"system\"}"
            });
        repository.Setup(x => x.GetPromptVersionAsync(userVersionId))
            .ReturnsAsync(new PromptVersions
            {
                Id = userVersionId,
                PromptId = userPromptId,
                Prompt = new Prompts { Id = userPromptId, PromptKey = CvAnalysisPromptContract.UserPromptKey },
                ModelConfig = "{\"contract\":\"cv-analysis/v1\",\"role\":\"user\"}"
            });

        var useCase = new PromptAdminUseCase(repository.Object);

        await useCase.ActivateCvAnalysisPromptPairAsync(systemVersionId, userVersionId, Guid.NewGuid());

        repository.Verify(x => x.ActivatePromptPairAsync(systemPromptId, systemVersionId, userPromptId, userVersionId), Times.Once);
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
        aiService
            .Setup(x => x.GenerateTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("{}");

        var promptService = new Mock<IPromptManagementService>();
        var responseValidator = new Mock<ICvAnalysisResponseValidator>();
        responseValidator
            .Setup(x => x.ValidateAndCanonicalize(It.IsAny<string>(), It.IsAny<ITHunterview.Service.DTOs.Cv.Matching.CvAnalysisInputSnapshot>()))
            .Returns(ITHunterview.Service.DTOs.Cv.Matching.CvAnalysisValidationResult.Success("{\"canonical\":true}"));
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
            "system from database",
            It.IsAny<string>()), Times.Once);
        responseValidator.Verify(x => x.ValidateAndCanonicalize(
            "{}",
            It.Is<ITHunterview.Service.DTOs.Cv.Matching.CvAnalysisInputSnapshot>(input =>
                input.RawText == "Jane Doe\nC# developer\n" &&
                input.SourceType == "pasted_text" &&
                input.FileName == "resume.txt")), Times.Once);
    }

    [Fact]
    public async Task ExtractParsedDataFromRawTextAsync_WhenTypedValidationFails_PreservesFailureCode()
    {
        var aiService = new Mock<IAiService>();
        aiService
            .Setup(x => x.GenerateTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("{}");

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
            .Setup(x => x.ValidateAndCanonicalize(It.IsAny<string>(), It.IsAny<ITHunterview.Service.DTOs.Cv.Matching.CvAnalysisInputSnapshot>()))
            .Returns(ITHunterview.Service.DTOs.Cv.Matching.CvAnalysisValidationResult.Failure("CV_ANALYSIS_EVIDENCE_NOT_GROUNDED"));

        var service = new CvTextExtractorService(
            NullLogger<CvTextExtractorService>.Instance,
            Mock.Of<System.Net.Http.IHttpClientFactory>(),
            Options.Create(new AiSettings()),
            aiService.Object,
            Mock.Of<ISystemConfigRepository>(),
            promptService.Object,
            responseValidator.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ExtractParsedDataFromRawTextAsync("Jane Doe\nC# developer\n", "pasted_text", "resume.txt"));

        Assert.Equal("CV_ANALYSIS_EVIDENCE_NOT_GROUNDED", exception.Message);
    }
}
