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
    [Theory]
    [InlineData(JdAnalysisQuality.COMPLETE)]
    [InlineData(JdAnalysisQuality.PARTIAL)]
    public async Task ProcessAsync_UsableExtractionAndResolverFailure_PersistsReadyStructuredResult(
        JdAnalysisQuality quality)
    {
        var context = CreateContext(quality);
        context.Resolver.Setup(resolver => resolver.ResolveAsync(
                It.IsAny<IReadOnlyList<ValidatedSkillMention>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("taxonomy unavailable"));

        await context.Processor.ProcessAsync(context.RunId);

        context.Repository.Verify(repository => repository.TryCompleteReadyAsync(
            context.RunId,
            It.Is<JobAnalysisCompletion>(completion =>
                completion.Quality == quality &&
                completion.RawAnalysisJson == "provider-json" &&
                completion.EffectiveAnalysisJson == "effective-json" &&
                completion.Decisions.Count == 0 &&
                completion.AnalysisDiagnosticsJson != null &&
                completion.AnalysisDiagnosticsJson.Contains("SKILL_RESOLUTION_UNAVAILABLE", StringComparison.Ordinal)),
            It.IsAny<CancellationToken>()), Times.Once);
        context.Repository.Verify(repository => repository.MarkFailedAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        context.Extraction.Verify(service => service.SerializeEffectiveAnalysis(
            It.Is<ValidatedJobAnalysis>(analysis =>
                analysis.Diagnostics.Any(diagnostic => diagnostic.Code == "SKILL_RESOLUTION_UNAVAILABLE"))), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_InvalidExtraction_PersistsRawFallbackAndDoesNotInvokeResolver()
    {
        var context = CreateContext(JdAnalysisQuality.INVALID);

        await context.Processor.ProcessAsync(context.RunId);

        context.Resolver.Verify(resolver => resolver.ResolveAsync(
            It.IsAny<IReadOnlyList<ValidatedSkillMention>>(), It.IsAny<CancellationToken>()), Times.Never);
        context.Extraction.Verify(service => service.SerializeEffectiveAnalysis(
            It.IsAny<ValidatedJobAnalysis>()), Times.Never);
        context.Repository.Verify(repository => repository.TryCompleteReadyAsync(
            context.RunId,
            It.Is<JobAnalysisCompletion>(completion =>
                completion.Quality == JdAnalysisQuality.INVALID &&
                completion.RawAnalysisJson == null &&
                completion.EffectiveAnalysisJson == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_ResolvedDecision_DoesNotPersistConfidence()
    {
        var context = CreateContext(JdAnalysisQuality.COMPLETE);
        context.Resolver.Setup(resolver => resolver.ResolveAsync(
                It.IsAny<IReadOnlyList<ValidatedSkillMention>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new SkillResolution
                {
                    RawMention = "Java",
                    NormalizedMention = "java",
                    Category = "tech_skill",
                    Importance = "must_have",
                    SourceSection = "requirements",
                    EvidenceText = "Java is required",
                    ResolutionStatus = SkillResolutionStatus.EXACT_CANONICAL,
                    Confidence = 0.99m,
                    ResolvedSkillId = 10,
                    SuggestedSkillId = 10
                }
            });

        await context.Processor.ProcessAsync(context.RunId);

        context.Repository.Verify(repository => repository.TryCompleteReadyAsync(
            context.RunId,
            It.Is<JobAnalysisCompletion>(completion =>
                completion.Decisions.Count == 1 && completion.Decisions[0].Confidence == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ProcessorContext CreateContext(JdAnalysisQuality quality)
    {
        var runId = Guid.NewGuid();
        var run = new JobAnalysisRuns
        {
            Id = runId,
            Status = JobAnalysisStatus.PROCESSING,
            InputRevision = 1,
            SystemPromptVersionId = Guid.NewGuid(),
            UserPromptVersionId = Guid.NewGuid(),
            RawInputSnapshot = "{}"
        };
        var repository = new Mock<IJobAnalysisRepository>();
        repository.Setup(candidate => candidate.GetRunAsync(runId, It.IsAny<CancellationToken>())).ReturnsAsync(run);
        repository.Setup(candidate => candidate.TryMarkProviderCallStartedAsync(runId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(candidate => candidate.TryCompleteReadyAsync(
                runId, It.IsAny<JobAnalysisCompletion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var prompts = new Mock<IPromptManagementService>();
        prompts.Setup(candidate => candidate.GetPromptSnapshotByVersionIdAsync(
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PromptSnapshotDto { Content = "prompt" });

        var analysis = new ValidatedJobAnalysis
        {
            SchemaVersion = "jd-analysis/v5",
            Quality = quality,
            Coverage = quality == JdAnalysisQuality.PARTIAL
                ? new JdAnalysisCoverage(2, 1, 1, 2, 1, 1, false)
                : new JdAnalysisCoverage(1, 1, 0, 1, 1, 0, true),
            Diagnostics = quality == JdAnalysisQuality.PARTIAL
                ? new List<JdAnalysisDiagnostic> { new("OUTPUT_TRUNCATED", "$") }
                : new List<JdAnalysisDiagnostic>(),
            SkillsNormalized = new List<ValidatedSkillMention>
            {
                new() { Name = "Java", RawMention = "Java", Category = "tech_skill" }
            }
        };
        var extraction = new Mock<IJobAnalysisExtractionService>();
        extraction.Setup(candidate => candidate.ExtractAsync(
                It.IsAny<JobAnalysisInputSnapshot>(), "prompt", "prompt", It.IsAny<CancellationToken>()))
            .ReturnsAsync(quality == JdAnalysisQuality.INVALID
                ? new JobAnalysisExtractionResult
                {
                    Quality = JdAnalysisQuality.INVALID,
                    RawTextFallback = "raw JD",
                    UsesRawTextFallback = true,
                    Diagnostics = new[] { new JdAnalysisDiagnostic("INVALID_JSON_FORMAT", "$") },
                    Validation = new ValidationResult<ValidatedJobAnalysis>
                    {
                        IsValid = false,
                        Quality = JdAnalysisQuality.INVALID,
                        FailureCode = "INVALID_JSON_FORMAT"
                    }
                }
                : new JobAnalysisExtractionResult
                {
                    ProviderName = "test",
                    RawJson = "provider-json",
                    PersistableAnalysisJson = "provider-json",
                    Quality = quality,
                    Coverage = analysis.Coverage,
                    Diagnostics = analysis.Diagnostics,
                    Validation = new ValidationResult<ValidatedJobAnalysis>
                    {
                        IsValid = quality == JdAnalysisQuality.COMPLETE,
                        Quality = quality,
                        Data = analysis
                    }
                });
        extraction.Setup(candidate => candidate.SerializeEffectiveAnalysis(It.IsAny<ValidatedJobAnalysis>()))
            .Returns("effective-json");

        var resolver = new Mock<ISkillResolver>();
        resolver.Setup(candidate => candidate.ResolveAsync(
                It.IsAny<IReadOnlyList<ValidatedSkillMention>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SkillResolution>());

        var processor = new JobAnalysisProcessor(
            repository.Object,
            prompts.Object,
            extraction.Object,
            resolver.Object,
            NullLogger<JobAnalysisProcessor>.Instance);
        return new ProcessorContext(runId, repository, extraction, resolver, processor);
    }

    private sealed record ProcessorContext(
        Guid RunId,
        Mock<IJobAnalysisRepository> Repository,
        Mock<IJobAnalysisExtractionService> Extraction,
        Mock<ISkillResolver> Resolver,
        JobAnalysisProcessor Processor);
}
