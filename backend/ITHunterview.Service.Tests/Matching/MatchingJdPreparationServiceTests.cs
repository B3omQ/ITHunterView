using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.DTOs.JobAnalysis;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Service;
using ITHunterview.Service.Service.Matching;
using ITHunterview.Service.Utils;
using Moq;

namespace ITHunterview.Service.Tests.Matching;

public sealed class MatchingJdPreparationServiceTests
{
    [Fact]
    public async Task PrepareAsync_UsableCurrentSavedAnalysis_ReusesStructuredDataWithoutExtraction()
    {
        var extractor = new Mock<IJobAnalysisExtractionService>(MockBehavior.Strict);
        var service = new MatchingJdPreparationService(
            extractor.Object,
            new JobAnalysisInputBuilder(),
            new JdRequirementProjector());
        var snapshot = SavedSnapshot(EffectiveJson("COMPLETE"), "SUCCESS", 7, 7);

        var prepared = await service.PrepareAsync(snapshot);

        var structured = Assert.IsType<PreparedStructuredJdMatchingInput>(prepared);
        Assert.Equal(JdAnalysisQuality.COMPLETE, structured.Quality);
        Assert.Single(structured.Projection.Groups);
        Assert.Null(structured.PersistenceIntent);
        extractor.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PrepareAsync_CurrentSavedRawFallback_ReturnsRawInputWithoutExtraction()
    {
        var extractor = new Mock<IJobAnalysisExtractionService>(MockBehavior.Strict);
        var service = new MatchingJdPreparationService(
            extractor.Object,
            new JobAnalysisInputBuilder(),
            new JdRequirementProjector());
        var snapshot = SavedSnapshot(null, "RAW_FALLBACK", 7, 7);

        var prepared = await service.PrepareAsync(snapshot);

        var raw = Assert.IsType<PreparedRawJdMatchingInput>(prepared);
        Assert.Equal("React developer required.", raw.RawText);
        Assert.Null(raw.PersistenceIntent);
        extractor.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PrepareAsync_InvalidStoredAnalysis_ExtractsThenReturnsRawFallbackAndPersistenceIntent()
    {
        var inputBuilder = new JobAnalysisInputBuilder();
        var extractor = new Mock<IJobAnalysisExtractionService>(MockBehavior.Strict);
        extractor
            .Setup(service => service.ExtractWithActivePromptsAsync(
                It.IsAny<JobAnalysisInputSnapshot>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JobAnalysisExtractionResult
            {
                Quality = JdAnalysisQuality.INVALID,
                RawTextFallback = "React developer required.",
                Diagnostics = new[] { new JdAnalysisDiagnostic("INVALID_JSON_FORMAT", "$") },
                UsesRawTextFallback = true
            });
        var service = new MatchingJdPreparationService(
            extractor.Object,
            inputBuilder,
            new JdRequirementProjector());
        var snapshot = SavedSnapshot("{not-json", "SUCCESS", 7, 7);

        var prepared = await service.PrepareAsync(snapshot);

        var raw = Assert.IsType<PreparedRawJdMatchingInput>(prepared);
        Assert.Equal("React developer required.", raw.RawText);
        Assert.NotNull(raw.PersistenceIntent);
        Assert.Equal("INVALID_JSON_FORMAT", raw.PersistenceIntent!.FailureCode);
        extractor.VerifyAll();
    }

    private static MatchingInputSnapshotV1 SavedSnapshot(
        string? analysisJson,
        string parseStatus,
        int analysisRevision,
        int effectiveAnalysisRevision) =>
        new(
            "matching-context/v2",
            MatchingMode.JdFit,
            new MatchingCvSnapshot("raw_cv", null, null, "Candidate", null, null),
            new MatchingJdSnapshot(
                "saved_jd",
                Guid.NewGuid(),
                "React Developer",
                "React developer required.",
                analysisJson,
                "jd-analysis/v3",
                "source-hash",
                "analysis-hash",
                analysisRevision,
                effectiveAnalysisRevision,
                parseStatus),
            DateTime.UtcNow);

    private static string EffectiveJson(string quality) => $$"""
        {
          "schema_version":"jd-analysis/v3",
          "analysis_quality":"{{quality}}",
          "matching_metrics":{
            "requirement_groups":[
              {
                "group_id":"grp-001",
                "operator":"all_of",
                "min_satisfied":1,
                "importance":"must_have",
                "items":[{"category":"tech_skill","skill_name":"react"}]
              }
            ]
          }
        }
        """;
}
