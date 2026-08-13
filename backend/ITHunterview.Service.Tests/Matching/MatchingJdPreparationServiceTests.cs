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
    public async Task PrepareAsync_PublishedEditHasUnappliedRevision_DoesNotReuseOrPersistOldAnalysis()
    {
        var extractor = new Mock<IJobAnalysisExtractionService>(MockBehavior.Strict);
        var validated = new ValidatedJobAnalysis { Quality = JdAnalysisQuality.COMPLETE };
        extractor.Setup(service => service.ExtractWithActivePromptsAsync(
                It.IsAny<JobAnalysisInputSnapshot>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JobAnalysisExtractionResult
            {
                Quality = JdAnalysisQuality.COMPLETE,
                Validation = new ValidationResult<ValidatedJobAnalysis>
                {
                    IsValid = true,
                    Quality = JdAnalysisQuality.COMPLETE,
                    Data = validated
                },
                Coverage = new JdAnalysisCoverage(1, 1, 0, 1, 1, 0, true)
            });
        extractor.Setup(service => service.SerializeEffectiveAnalysis(validated))
            .Returns(EffectiveJson("COMPLETE"));
        var service = new MatchingJdPreparationService(
            extractor.Object,
            new JobAnalysisInputBuilder(),
            new JdRequirementProjector());
        var snapshot = SavedSnapshot(EffectiveJson("COMPLETE"), "STALE", 8, 7);

        var prepared = Assert.IsType<PreparedStructuredJdMatchingInput>(await service.PrepareAsync(snapshot));

        Assert.Null(prepared.PersistenceIntent);
        extractor.VerifyAll();
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

    [Theory]
    [InlineData("INVALID_JSON_FORMAT")]
    [InlineData("NO_USABLE_REQUIREMENT_GROUPS")]
    public async Task PrepareAsync_InvalidStoredAnalysis_ExtractsThenReturnsRawFallbackAndPersistenceIntent(
        string failureCode)
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
                Diagnostics = new[] { new JdAnalysisDiagnostic(failureCode, "$") },
                UsesRawTextFallback = true,
                Validation = new ValidationResult<ValidatedJobAnalysis>
                {
                    IsValid = false,
                    Quality = JdAnalysisQuality.INVALID,
                    FailureCode = failureCode
                }
            });
        var service = new MatchingJdPreparationService(
            extractor.Object,
            inputBuilder,
            new JdRequirementProjector());
        var snapshot = SavedSnapshot("{not-json", "SUCCESS", 7, 7);

        var prepared = await service.PrepareAsync(snapshot);

        var raw = Assert.IsType<PreparedRawJdMatchingInput>(prepared);
        Assert.Equal("React developer required.", raw.RawText);
        Assert.Equal(JdAnalysisQuality.INVALID, raw.Quality);
        Assert.NotNull(raw.PersistenceIntent);
        Assert.Null(raw.PersistenceIntent!.CanonicalJson);
        Assert.Equal(failureCode, raw.PersistenceIntent.FailureCode);
        extractor.VerifyAll();
    }

    [Fact]
    public async Task PrepareAsync_SavedV3ReparseUsesSeparatedCanonicalInput()
    {
        JobAnalysisInputSnapshot? observedInput = null;
        var extractor = InvalidExtractor(input => observedInput = input);
        var service = new MatchingJdPreparationService(
            extractor.Object,
            new JobAnalysisInputBuilder(),
            new JdRequirementProjector());
        var canonical = "{\"title\":\"Backend Engineer\",\"description\":\"Design APIs.\\nImprove performance.\",\"requirements\":\"C# required.\\nPostgreSQL required.\"}";
        var snapshot = SavedSnapshot(null, "FAILED", 7, 7, "matching-context/v3", canonical);

        await service.PrepareAsync(snapshot);

        Assert.NotNull(observedInput);
        Assert.Equal("Design APIs.\nImprove performance.", observedInput!.Description);
        Assert.Equal("C# required.\nPostgreSQL required.", observedInput.Requirements);
        extractor.VerifyAll();
    }

    [Fact]
    public async Task PrepareAsync_SavedV2ReparseUsesDeterministicLabeledCompatibilityParser()
    {
        JobAnalysisInputSnapshot? observedInput = null;
        var extractor = InvalidExtractor(input => observedInput = input);
        var service = new MatchingJdPreparationService(
            extractor.Object,
            new JobAnalysisInputBuilder(),
            new JdRequirementProjector());
        var snapshot = SavedSnapshot(null, "FAILED", 7, 7) with
        {
            Jd = SavedSnapshot(null, "FAILED", 7, 7).Jd with
            {
                OriginalText = "Title: Backend Engineer\nDescription: Design APIs.\nImprove performance.\nRequirements: C# required.\nPostgreSQL required.\nBenefits: Insurance."
            }
        };

        await service.PrepareAsync(snapshot);

        Assert.NotNull(observedInput);
        Assert.Equal("Design APIs.\nImprove performance.", observedInput!.Description);
        Assert.Equal("C# required.\nPostgreSQL required.", observedInput.Requirements);
        extractor.VerifyAll();
    }

    [Fact]
    public async Task PrepareAsync_MalformedV3CanonicalInputFallsBackAndRecordsDiagnostic()
    {
        JobAnalysisInputSnapshot? observedInput = null;
        var extractor = InvalidExtractor(input => observedInput = input);
        var service = new MatchingJdPreparationService(
            extractor.Object,
            new JobAnalysisInputBuilder(),
            new JdRequirementProjector());
        var snapshot = SavedSnapshot(null, "FAILED", 7, 7, "matching-context/v3", "{not-json") with
        {
            Jd = SavedSnapshot(null, "FAILED", 7, 7, "matching-context/v3", "{not-json").Jd with
            {
                OriginalText = "Title: Backend Engineer\nDescription: Design APIs.\nRequirements: C# required."
            }
        };

        var prepared = await service.PrepareAsync(snapshot);

        Assert.Equal("Design APIs.", observedInput!.Description);
        Assert.Equal("C# required.", observedInput.Requirements);
        Assert.Contains(prepared.Diagnostics, diagnostic =>
            diagnostic.Code == "SNAPSHOT_CANONICAL_INPUT_INVALID" &&
            diagnostic.JsonPath == "$.jd.analysisInputJson");
    }

    [Fact]
    public async Task PrepareAsync_RawV3UsesCanonicalSplitAndKeepsOriginalFallback()
    {
        JobAnalysisInputSnapshot? observedInput = null;
        var extractor = InvalidExtractor(input => observedInput = input);
        var service = new MatchingJdPreparationService(
            extractor.Object,
            new JobAnalysisInputBuilder(),
            new JdRequirementProjector());
        const string raw = "Mô tả công việc\nDesign APIs.\n\nYêu cầu ứng viên\nC# required.";
        var snapshot = new MatchingInputSnapshotV1(
            "matching-context/v3",
            MatchingMode.JdFit,
            new MatchingCvSnapshot("raw_cv", null, null, "Candidate", null, null),
            new MatchingJdSnapshot(
                "raw_jd",
                null,
                "Backend Engineer",
                raw,
                null,
                null,
                AnalysisInputJson: "{\"title\":\"Backend Engineer\",\"description\":\"Mô tả công việc\\nDesign APIs.\",\"requirements\":\"Yêu cầu ứng viên\\nC# required.\"}"),
            DateTime.UtcNow);

        var prepared = await service.PrepareAsync(snapshot);

        Assert.Equal("Mô tả công việc\nDesign APIs.", observedInput!.Description);
        Assert.Equal("Yêu cầu ứng viên\nC# required.", observedInput.Requirements);
        Assert.Equal(raw, Assert.IsType<PreparedRawJdMatchingInput>(prepared).RawText);
    }

    [Fact]
    public async Task PrepareAsync_SuccessfulSavedV3ReparseKeepsGuardedPersistenceIntent()
    {
        var extractor = new Mock<IJobAnalysisExtractionService>(MockBehavior.Strict);
        var validated = new ValidatedJobAnalysis { Quality = JdAnalysisQuality.COMPLETE };
        var validation = new ValidationResult<ValidatedJobAnalysis>
        {
            IsValid = true,
            Quality = JdAnalysisQuality.COMPLETE,
            Data = validated
        };
        var effectiveJson = EffectiveJson("COMPLETE");
        extractor.Setup(service => service.ExtractWithActivePromptsAsync(
                It.IsAny<JobAnalysisInputSnapshot>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JobAnalysisExtractionResult
            {
                Quality = JdAnalysisQuality.COMPLETE,
                Validation = validation,
                Coverage = new JdAnalysisCoverage(1, 1, 0, 1, 1, 0, true)
            });
        extractor.Setup(service => service.SerializeEffectiveAnalysis(validated)).Returns(effectiveJson);
        var service = new MatchingJdPreparationService(
            extractor.Object,
            new JobAnalysisInputBuilder(),
            new JdRequirementProjector());
        var snapshot = SavedSnapshot(
            null,
            "FAILED",
            7,
            7,
            "matching-context/v3",
            "{\"title\":\"React Developer\",\"description\":\"\",\"requirements\":\"React developer required.\"}");

        var prepared = Assert.IsType<PreparedStructuredJdMatchingInput>(await service.PrepareAsync(snapshot));

        Assert.NotNull(prepared.PersistenceIntent);
        Assert.Equal(effectiveJson, prepared.PersistenceIntent!.CanonicalJson);
        Assert.Equal(snapshot.Jd.SourceContentHash, prepared.PersistenceIntent.ExpectedSourceHash);
        Assert.Equal(snapshot.Jd.SourceAnalysisHash, prepared.PersistenceIntent.ExpectedAnalysisHash);
        Assert.Equal(snapshot.Jd.SourceAnalysisRevision, prepared.PersistenceIntent.ExpectedRevision);
        extractor.VerifyAll();
    }

    private static MatchingInputSnapshotV1 SavedSnapshot(
        string? analysisJson,
        string parseStatus,
        int analysisRevision,
        int effectiveAnalysisRevision,
        string schemaVersion = "matching-context/v2",
        string? analysisInputJson = null) =>
        new(
            schemaVersion,
            MatchingMode.JdFit,
            new MatchingCvSnapshot("raw_cv", null, null, "Candidate", null, null),
            new MatchingJdSnapshot(
                "saved_jd",
                Guid.NewGuid(),
                "React Developer",
                "React developer required.",
                analysisJson,
                "jd-analysis-effective/v1",
                "source-hash",
                "analysis-hash",
                analysisRevision,
                effectiveAnalysisRevision,
                parseStatus,
                analysisInputJson),
            DateTime.UtcNow);

    private static Mock<IJobAnalysisExtractionService> InvalidExtractor(Action<JobAnalysisInputSnapshot> capture)
    {
        var extractor = new Mock<IJobAnalysisExtractionService>(MockBehavior.Strict);
        extractor.Setup(service => service.ExtractWithActivePromptsAsync(
                It.IsAny<JobAnalysisInputSnapshot>(),
                It.IsAny<CancellationToken>()))
            .Callback<JobAnalysisInputSnapshot, CancellationToken>((input, _) => capture(input))
            .ReturnsAsync(new JobAnalysisExtractionResult
            {
                Quality = JdAnalysisQuality.INVALID,
                RawTextFallback = string.Empty,
                Diagnostics = Array.Empty<JdAnalysisDiagnostic>(),
                UsesRawTextFallback = true
            });
        return extractor;
    }

    private static string EffectiveJson(string quality) => $$"""
        {
          "schema_version":"jd-analysis-effective/v1",
          "analysis_quality":"{{quality}}",
          "analysis_coverage":{"input_group_count":1,"accepted_group_count":1,"discarded_group_count":0,"input_item_count":1,"accepted_item_count":1,"discarded_item_count":0,"requirement_set_complete":true},
          "matching_metrics":{
            "requirement_groups":[
              {
                "group_id":"grp-001",
                "source_requirement_id":"req-001",
                "intent":"qualification",
                "operator":"all_of",
                "min_satisfied":1,
                "importance":"must_have",
                "source_section":"requirements",
                "requirement_verbatim":"React developer required.",
                "items":[{"item_id":"grp-001:item-001","category":"tech_skill","skill_name":"react","raw_mention":"React"}]
              }
            ]
          }
        }
        """;
}
