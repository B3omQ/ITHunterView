using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.JobAnalysis;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Service;
using ITHunterview.Service.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ITHunterview.Service.Tests.JobAnalysis;

public sealed class JobAnalysisProcessorThreeStateTests
{
    [Fact]
    public async Task ProcessAsync_PartialButUsableAnalysis_CompletesInsteadOfFailing()
    {
        var runId = Guid.NewGuid();
        var systemVersionId = Guid.NewGuid();
        var userVersionId = Guid.NewGuid();
        var repository = new Mock<IJobAnalysisRepository>();
        repository.Setup(x => x.GetRunAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JobAnalysisRuns
            {
                Id = runId,
                Status = JobAnalysisStatus.PROCESSING,
                SystemPromptVersionId = systemVersionId,
                UserPromptVersionId = userVersionId,
                RawInputSnapshot = "{}"
            });
        repository.Setup(x => x.TryMarkProviderCallStartedAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(x => x.TryCompleteReadyAsync(
                runId, It.IsAny<JobAnalysisCompletion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var prompts = new Mock<IPromptManagementService>();
        prompts.Setup(x => x.GetPromptSnapshotByVersionIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PromptSnapshotDto { Content = "prompt" });

        var analysis = new ValidatedJobAnalysis
        {
            SchemaVersion = "jd-analysis/v3",
            Quality = JdAnalysisQuality.PARTIAL,
            Coverage = new JdAnalysisCoverage(2, 1, 1, 2, 1, 1, false),
            Diagnostics = [new JdAnalysisDiagnostic("INVALID_REQUIREMENT_GROUP", "$.matching_metrics.requirement_groups[1]")]
        };
        var extraction = new Mock<IJobAnalysisExtractionService>();
        extraction.Setup(x => x.ExtractAsync(It.IsAny<JobAnalysisInputSnapshot>(), "prompt", "prompt", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JobAnalysisExtractionResult
            {
                ProviderName = "test",
                RawJson = "raw",
                PersistableAnalysisJson = "raw",
                Quality = JdAnalysisQuality.PARTIAL,
                Coverage = new JdAnalysisCoverage(2, 1, 1, 2, 1, 1, false),
                Validation = new ValidationResult<ValidatedJobAnalysis>
                {
                    IsValid = false,
                    Quality = JdAnalysisQuality.PARTIAL,
                    FailureCode = "PARTIAL_JD_ANALYSIS",
                    Data = analysis
                }
            });
        extraction.Setup(x => x.SerializeEffectiveAnalysis(analysis, It.IsAny<IReadOnlyCollection<string>>()))
            .Returns("{\"schema_version\":\"jd-analysis/v3\"}");

        var resolver = new Mock<ISkillResolver>();
        resolver.Setup(x => x.ResolveAsync(It.IsAny<IReadOnlyList<ValidatedSkillMention>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SkillResolution>());

        var processor = new JobAnalysisProcessor(
            repository.Object,
            prompts.Object,
            extraction.Object,
            resolver.Object,
            NullLogger<JobAnalysisProcessor>.Instance);

        await processor.ProcessAsync(runId);

        repository.Verify(x => x.TryCompleteReadyAsync(
            runId, It.Is<JobAnalysisCompletion>(completion =>
                completion.Quality == JdAnalysisQuality.PARTIAL &&
                completion.RawAnalysisJson == "raw"),
            It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(x => x.MarkFailedAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
