using System.Text.Json;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.DTOs.FeatureUsage;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.Service.Matching;
using ITHunterview.Service.UseCase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ITHunterview.Service.Tests.Matching;

public sealed class MatchingProductBoundaryCharacterizationTests
{
    public static IEnumerable<object[]> CurrentHardcodeGoldenCases()
    {
        var fixture = LoadFixture();
        foreach (var testCase in fixture.Cases)
        {
            yield return new object[] { testCase };
        }
    }

    [Theory]
    [Trait("Requirement", "R-04")]
    [MemberData(nameof(CurrentHardcodeGoldenCases))]
    public async Task CurrentHardcodePath_FixedInputs_PersistsExpectedScoreAndCanonicalDetails(
        HardcodeGoldenCase testCase)
    {
        var fixture = LoadFixture();
        var expectedCv = fixture.ExpectedCvAnalyses[testCase.CvAnalysis];
        await using var context = CreateContext();
        var userId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var cv = CreateCv(userId, fixture.CvAnalyses[testCase.CvAnalysis]);
        var job = CreateJob(testCase.Ordinal, testCase.JobAnalysis);
        context.Cvs.Add(cv);
        context.JobPostings.Add(job);
        await context.SaveChangesAsync();

        await CreateHardcodeUseCase(context).MatchCvWithAllJobsHardcodeAsync(cv.Id, userId);

        var score = await context.CvJobMatchScores.SingleAsync();
        score.MatchScore.Should().Be(testCase.ExpectedMatchScore);
        score.Status.Should().Be(testCase.ExpectedStatus);
        score.MatchType.Should().Be(testCase.ExpectedMatchType);
        score.ErrorCode.Should().BeNull();
        score.ErrorMessage.Should().BeNull();
        AssertCvAnalysis(score, cv, expectedCv);
        AssertMatchDetails(score.MatchDetails, testCase.ExpectedMatchDetails);
    }

    [Fact]
    [Trait("Requirement", "R-02")]
    public async Task CandidatePreparation_OneCvAcrossManyJobs_ExtractsAndCanonicalizesOnceAndPersistsMetadata()
    {
        var fixture = LoadFixture();
        var rawCv = fixture.CvAnalyses["complete"];
        var expectedCv = fixture.ExpectedCvAnalyses["complete"];
        await using var context = CreateContext();
        var userId = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var cv = CreateCv(userId, default);
        cv.ParsedData = string.Empty;
        cv.ParseStatus = "PENDING";
        context.Cvs.Add(cv);
        context.JobPostings.AddRange(fixture.Cases.Take(3).Select(testCase =>
            CreateJob(testCase.Ordinal + 20, testCase.JobAnalysis)));
        await context.SaveChangesAsync();

        var extractor = new Mock<ICvTextExtractorService>(MockBehavior.Strict);
        extractor.Setup(service => service.ExtractParsedDataFromUrlAsync(cv.FileUrl, cv.RawText!))
            .ReturnsAsync(rawCv.GetRawText());
        var realValidator = new CvAnalysisResponseValidator();
        var validator = new Mock<ICvAnalysisResponseValidator>(MockBehavior.Strict);
        validator.Setup(service => service.ValidateAndCanonicalize(rawCv.GetRawText()))
            .Returns(realValidator.ValidateAndCanonicalize(rawCv.GetRawText()));

        await CreateHardcodeUseCase(context, extractor.Object, validator.Object)
            .MatchCvWithAllJobsHardcodeAsync(cv.Id, userId);

        var scores = await context.CvJobMatchScores.OrderBy(value => value.JobId).ToListAsync();
        scores.Should().HaveCount(3);
        cv.ParseStatus.Should().Be("SUCCESS");
        AssertCanonicalCv(cv, expectedCv);
        scores.Should().OnlyContain(score =>
            score.CvAnalysisQuality == cv.AnalysisQuality &&
            score.CvAnalysisCoverageJson == cv.AnalysisCoverageJson &&
            score.CvAnalysisDiagnosticsJson == cv.AnalysisDiagnosticsJson);
        extractor.Verify(service => service.ExtractParsedDataFromUrlAsync(cv.FileUrl, cv.RawText!), Times.Once);
        validator.Verify(service => service.ValidateAndCanonicalize(rawCv.GetRawText()), Times.Once);
    }

    [Fact]
    [Trait("Requirement", "R-03")]
    public async Task RecruiterPreparation_AlreadyUsableCvs_AreNotReparsedAndMetadataIsPersisted()
    {
        var fixture = LoadFixture();
        await using var context = CreateContext();
        var completeCv = CreateCvWithVisibleCandidate(
            context,
            Guid.Parse("10000000-0000-0000-0000-000000000003"),
            fixture.CvAnalyses["complete"],
            31);
        var partialCv = CreateCvWithVisibleCandidate(
            context,
            Guid.Parse("10000000-0000-0000-0000-000000000004"),
            fixture.CvAnalyses["partial"],
            32);
        var legacyCase = fixture.Cases.Single(value => value.Name == "legacy_metrics_partial_cv");
        var job = CreateJob(31, legacyCase.JobAnalysis);
        context.JobPostings.Add(job);
        await context.SaveChangesAsync();

        var extractor = new Mock<ICvTextExtractorService>(MockBehavior.Strict);
        await CreateHardcodeUseCase(context, extractor.Object, new CvAnalysisResponseValidator())
            .MatchJobWithAllCvsHardcodeAsync(job.Id, job.RecruiterId);

        var scores = await context.CvJobMatchScores.OrderBy(value => value.CvId).ToListAsync();
        scores.Should().HaveCount(2);
        AssertCvAnalysis(
            scores.Single(value => value.CvId == completeCv.Id),
            completeCv,
            fixture.ExpectedCvAnalyses["complete"]);
        AssertCvAnalysis(
            scores.Single(value => value.CvId == partialCv.Id),
            partialCv,
            fixture.ExpectedCvAnalyses["partial"]);
        extractor.Verify(service => service.ExtractParsedDataFromUrlAsync(
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    [Trait("Requirement", "R-01")]
    public async Task OneToOneSubmission_AutomaticRetry_PreservesSingleReservationAndOutputContract()
    {
        await using var context = CreateContext();
        var userId = Guid.Parse("10000000-0000-0000-0000-000000000005");
        var reservationId = Guid.Parse("60000000-0000-0000-0000-000000000001");
        var featureUsage = CreateFeatureUsageMock(reservationId);
        var preflight = new Mock<IMatchingInputPreflightUseCase>(MockBehavior.Strict);
        preflight.Setup(service => service.PrepareAsync(
                userId,
                It.IsAny<MatchingRequestDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RawPreparedRequest());
        var sourceRepository = new Mock<IMatchingSourceRepository>(MockBehavior.Strict);
        var repository = new CvJdMatchingJobRepository(context);
        var submission = new CvJdMatchingSubmissionUseCase(
            context,
            new MatchingRequestValidator(),
            preflight.Object,
            new MatchingInputSnapshotBuilder(sourceRepository.Object),
            repository,
            featureUsage.Object);

        var submitted = await submission.SubmitAsync(userId, RawRequest(), "characterization-retry-1");
        var submittedJob = await context.CvJobMatchScores.SingleAsync();
        var initiallyPersistedSnapshotJson = submittedJob.InputSnapshotJson!;
        var initiallyPersistedInputHash = submittedJob.InputHash!;
        var initiallyPersistedSnapshot = MatchingInputSnapshotIntegrity.Deserialize(initiallyPersistedSnapshotJson);
        submitted.IsExisting.Should().BeFalse();
        submittedJob.BillingReservationId.Should().Be(reservationId);

        const string expectedDetails = """
            {"mode":"jd_fit","contract":"jd-matching/v5","scoreAvailable":true,"completionDisposition":"scored_billable","jdFit":{"scorePercent":82,"requirementGroups":[],"criticalGaps":[]}}
            """;
        var attemptedSnapshots = new List<MatchingInputSnapshotV1>();
        var processor = new Mock<ICvJdOneToOneMatchingProcessor>(MockBehavior.Strict);
        processor.Setup(service => service.ExecuteAsync(
                submitted.JobId,
                It.IsAny<MatchingInputSnapshotV1>(),
                It.IsAny<CancellationToken>()))
            .Returns((Guid _, MatchingInputSnapshotV1 snapshot, CancellationToken _) =>
            {
                attemptedSnapshots.Add(snapshot);
                return attemptedSnapshots.Count == 1
                    ? Task.FromException<CvJdMatchingExecutionResult>(
                        new TimeoutException("Synthetic transient provider timeout."))
                    : Task.FromResult(new CvJdMatchingExecutionResult(82m, expectedDetails, null));
            });
        var worker = new CvJdMatchingWorkerUseCase(
            context,
            repository,
            processor.Object,
            featureUsage.Object,
            NullLogger<CvJdMatchingWorkerUseCase>.Instance);

        var firstClaim = (await repository.ClaimRunnableJobsAsync(
            1,
            "characterization-worker",
            DateTime.UtcNow.AddMinutes(1),
            CvJdMatchingWorkerUseCase.LeaseDuration)).Single();
        await worker.ProcessClaimedJobAsync(submitted.JobId, "characterization-worker", firstClaim.LeaseToken);
        submittedJob.Status.Should().Be("RetryScheduled");
        submittedJob.AttemptCount.Should().Be(1);
        submittedJob.BillingReservationId.Should().Be(reservationId);

        var secondClaim = (await repository.ClaimRunnableJobsAsync(
            1,
            "characterization-worker",
            DateTime.UtcNow.AddDays(1),
            CvJdMatchingWorkerUseCase.LeaseDuration)).Single();
        await worker.ProcessClaimedJobAsync(submitted.JobId, "characterization-worker", secondClaim.LeaseToken);

        var completed = await context.CvJobMatchScores.SingleAsync();
        completed.Id.Should().Be(submitted.JobId);
        completed.BillingReservationId.Should().Be(reservationId);
        completed.AttemptCount.Should().Be(2);
        completed.Status.Should().Be("Completed");
        completed.ProcessingStage.Should().Be(MatchingProcessingStages.Completed);
        completed.MatchType.Should().Be("AI");
        completed.MatchScore.Should().Be(82m);
        completed.ErrorCode.Should().BeNull();
        attemptedSnapshots.Should().HaveCount(2);
        attemptedSnapshots[0].Should().Be(initiallyPersistedSnapshot);
        attemptedSnapshots[1].Should().Be(initiallyPersistedSnapshot);
        attemptedSnapshots[1].Should().Be(attemptedSnapshots[0]);
        MatchingInputSnapshotIntegrity.IsValid(attemptedSnapshots[0], initiallyPersistedInputHash).Should().BeTrue();
        MatchingInputSnapshotIntegrity.IsValid(attemptedSnapshots[1], initiallyPersistedInputHash).Should().BeTrue();
        completed.InputSnapshotJson.Should().Be(initiallyPersistedSnapshotJson);
        completed.InputHash.Should().Be(initiallyPersistedInputHash);
        AssertJsonEquivalent(completed.MatchDetails, expectedDetails, "one-to-one terminal output contract");
        featureUsage.Verify(service => service.ReserveFeatureAsync(
            userId,
            CvJdMatchingSubmissionUseCase.FeatureKey,
            submitted.JobId,
            It.IsAny<CancellationToken>()), Times.Once);
        featureUsage.Verify(service => service.CaptureFeatureReservationAsync(
            reservationId,
            It.IsAny<CancellationToken>()), Times.Once);
        featureUsage.Verify(service => service.RefundFeatureReservationAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static void AssertMatchDetails(string actualJson, JsonElement expected)
    {
        AssertJsonEquivalent(actualJson, expected.GetRawText(), "hardcode match details");
        using var actualDocument = JsonDocument.Parse(actualJson);
        var actual = actualDocument.RootElement;
        actual.GetProperty("ScoreBasis").GetString()
            .Should().Be(expected.GetProperty("ScoreBasis").GetString());
        AssertOptionalProperty(actual, expected, "AvailableDimensions");
        AssertOptionalProperty(actual, expected, "Weights");
        AssertOptionalProperty(actual, expected, "ResultCode");
        AssertOptionalProperty(actual, expected, "InternalReasonCode");
        AssertOptionalProperty(actual, expected, "CvAnalysisQuality");
        AssertOptionalProperty(actual, expected, "JdAnalysisQuality");
    }

    private static void AssertOptionalProperty(JsonElement actual, JsonElement expected, string propertyName)
    {
        var expectedHasProperty = expected.TryGetProperty(propertyName, out var expectedValue);
        actual.TryGetProperty(propertyName, out var actualValue).Should().Be(expectedHasProperty);
        if (expectedHasProperty)
        {
            JsonElement.DeepEquals(actualValue, expectedValue).Should().BeTrue(
                $"{propertyName} must match the observed protected-engine value");
        }
    }

    private static void AssertCvAnalysis(
        CvJobMatchScores score,
        Cvs cv,
        ExpectedCvAnalysis expected)
    {
        score.CvAnalysisQuality?.ToString().Should().Be(expected.Quality);
        score.CvAnalysisQuality.Should().Be(cv.AnalysisQuality);
        score.CvAnalysisCoverageJson.Should().Be(cv.AnalysisCoverageJson);
        score.CvAnalysisDiagnosticsJson.Should().Be(cv.AnalysisDiagnosticsJson);
        AssertJsonEquivalent(score.CvAnalysisCoverageJson!, expected.Coverage.GetRawText(), "persisted CV coverage");
        AssertJsonEquivalent(score.CvAnalysisDiagnosticsJson!, expected.Diagnostics.GetRawText(), "persisted CV diagnostics");
        AssertCanonicalCv(cv, expected);
    }

    private static void AssertCanonicalCv(Cvs cv, ExpectedCvAnalysis expected)
    {
        AssertJsonEquivalent(cv.ParsedData!, expected.Canonical.GetRawText(), "canonical saved CV analysis");
        cv.AnalysisQuality?.ToString().Should().Be(expected.Quality);
        AssertJsonEquivalent(cv.AnalysisCoverageJson!, expected.Coverage.GetRawText(), "saved CV coverage");
        AssertJsonEquivalent(cv.AnalysisDiagnosticsJson!, expected.Diagnostics.GetRawText(), "saved CV diagnostics");
    }

    private static void AssertJsonEquivalent(string actualJson, string expectedJson, string because)
    {
        using var actual = JsonDocument.Parse(actualJson);
        using var expected = JsonDocument.Parse(expectedJson);
        JsonElement.DeepEquals(actual.RootElement, expected.RootElement).Should().BeTrue(
            $"{because} must be semantically identical; actual={actualJson}; expected={expectedJson}");
    }

    private static Cvs CreateCv(Guid userId, JsonElement analysis, int ordinal = 1) => new()
    {
        Id = Guid.Parse($"20000000-0000-0000-0000-{ordinal:000000000000}"),
        UserId = userId,
        FileUrl = "https://synthetic.example.test/candidate.pdf",
        FileName = "synthetic-candidate.pdf",
        FileType = "application/pdf",
        ParsedData = analysis.ValueKind == JsonValueKind.Undefined ? string.Empty : analysis.GetRawText(),
        ParseStatus = analysis.ValueKind == JsonValueKind.Undefined ? "PENDING" : "SUCCESS",
        RawText = "Synthetic saved CV source.",
        CreatedAt = FixedUtc,
        UpdatedAt = FixedUtc,
        IsPrimary = true
    };

    private static Cvs CreateCvWithVisibleCandidate(
        CharacterizationTestContext context,
        Guid userId,
        JsonElement analysis,
        int ordinal)
    {
        var cv = CreateCv(userId, analysis, ordinal);
        var user = new User
        {
            Id = userId,
            Email = $"synthetic-{ordinal}@example.test",
            Status = UserStatus.ACTIVE
        };
        var profile = new CandidateProfiles
        {
            UserId = userId,
            IsVisibleToRecruiters = true,
            User = user
        };
        user.CandidateProfile = profile;
        user.Cvs.Add(cv);
        cv.User = user;
        context.Users.Add(user);
        context.CandidateProfiles.Add(profile);
        context.Cvs.Add(cv);
        return cv;
    }

    private static JobPostings CreateJob(int ordinal, JsonElement analysis) => new()
    {
        Id = Guid.Parse($"30000000-0000-0000-0000-{ordinal + 1:000000000000}"),
        JobCode = $"SYNTHETIC-{ordinal + 1:000}",
        RecruiterId = Guid.Parse("40000000-0000-0000-0000-000000000001"),
        CompanyId = Guid.Parse("50000000-0000-0000-0000-000000000001"),
        Title = "Backend Engineer",
        Description = "Build synthetic APIs.",
        Requirements = "Synthetic requirements only.",
        Benefits = string.Empty,
        Currency = "VND",
        Location = "Remote",
        Status = JobStatus.PUBLISHED,
        ParseStatus = "SUCCESS",
        ParsedData = analysis.GetRawText(),
        CreatedAt = FixedUtc,
        UpdatedAt = FixedUtc
    };

    private static HardcodeCvJobMatchingUseCase CreateHardcodeUseCase(ITHunterviewContext context) =>
        CreateHardcodeUseCase(
            context,
            new Mock<ICvTextExtractorService>(MockBehavior.Strict).Object,
            new CvAnalysisResponseValidator());

    private static HardcodeCvJobMatchingUseCase CreateHardcodeUseCase(
        ITHunterviewContext context,
        ICvTextExtractorService extractor,
        ICvAnalysisResponseValidator validator) => new(
            context,
            extractor,
            NullLogger<HardcodeCvJobMatchingUseCase>.Instance,
            new HardcodeJdRequirementScoringService(new JdRequirementProjector(), new JdHardcodeRequirementEvaluator()),
            validator);

    private static Mock<ICandidateFeatureUsageUseCase> CreateFeatureUsageMock(Guid reservationId)
    {
        var mock = new Mock<ICandidateFeatureUsageUseCase>(MockBehavior.Strict);
        mock.Setup(service => service.AcquireFeatureSubmissionLockAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mock.Setup(service => service.ReserveFeatureAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, string feature, Guid reference, CancellationToken _) =>
                new FeatureReservationResult(reservationId, reference, feature, "Coin", "Reserved", 1000, null));
        mock.Setup(service => service.CaptureFeatureReservationAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mock.Setup(service => service.RefundFeatureReservationAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    private static MatchingRequestDto RawRequest() => new()
    {
        CvText = "Synthetic raw CV text.".PadRight(100, 'c'),
        RawJdText = "Synthetic raw JD text.".PadRight(100, 'j')
    };

    private static PreparedMatchingRequest RawPreparedRequest() => new(
        new PreparedRawCvSource("Synthetic raw CV text.".PadRight(100, 'c'), "synthetic-cv.pdf"),
        new PreparedRawJdSource("Synthetic raw JD text.".PadRight(100, 'j'), "Synthetic JD"),
        MatchingMode.JdFit);

    private static CharacterizationFixture LoadFixture()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Matching", "Fixtures", "hardcode-characterization-cases.json");
        return JsonSerializer.Deserialize<CharacterizationFixture>(File.ReadAllText(path), JsonOptions)
               ?? throw new InvalidOperationException("Characterization fixture could not be loaded.");
    }

    private static CharacterizationTestContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ITHunterviewContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new CharacterizationTestContext(options);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly DateTime FixedUtc = new(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc);

    private sealed record CharacterizationFixture(
        Dictionary<string, JsonElement> CvAnalyses,
        Dictionary<string, ExpectedCvAnalysis> ExpectedCvAnalyses,
        IReadOnlyList<HardcodeGoldenCase> Cases);

    private sealed record ExpectedCvAnalysis(
        string Quality,
        JsonElement Coverage,
        JsonElement Diagnostics,
        JsonElement Canonical);

    public sealed record HardcodeGoldenCase(
        string Name,
        string CvAnalysis,
        decimal? ExpectedMatchScore,
        string ExpectedStatus,
        string ExpectedMatchType,
        JsonElement ExpectedMatchDetails,
        JsonElement JobAnalysis)
    {
        public int Ordinal => Name switch
        {
            "structured_all_dimensions" => 0,
            "legacy_metrics_partial_cv" => 1,
            "partial_requirement_set_unscored" => 2,
            "no_safe_dimensions_unscored" => 3,
            _ => throw new InvalidOperationException($"Unknown characterization case '{Name}'.")
        };

        public override string ToString() => Name;
    }

    private sealed class CharacterizationTestContext : ITHunterviewContext
    {
        public CharacterizationTestContext(DbContextOptions<ITHunterviewContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                         .Where(type => type.ClrType != typeof(Cvs)
                                        && type.ClrType != typeof(JobPostings)
                                        && type.ClrType != typeof(CvJobMatchScores)
                                        && type.ClrType != typeof(JobSkillRequirements)
                                        && type.ClrType != typeof(Skills)
                                        && type.ClrType != typeof(User)
                                        && type.ClrType != typeof(CandidateProfiles)
                                        && type.ClrType != typeof(FeatureUsageReservations))
                         .Select(type => type.ClrType)
                         .Distinct()
                         .ToList())
            {
                modelBuilder.Ignore(entityType);
            }

            modelBuilder.Entity<Cvs>().Ignore(value => value.TitleEmbedding);
            modelBuilder.Entity<Cvs>().Ignore(value => value.SkillsEmbedding);
            modelBuilder.Entity<Cvs>().Ignore(value => value.ExperienceEmbedding);
            modelBuilder.Entity<Cvs>().Ignore(value => value.DomainEmbedding);
            modelBuilder.Entity<JobPostings>().Ignore(value => value.TitleEmbedding);
            modelBuilder.Entity<JobPostings>().Ignore(value => value.SkillsEmbedding);
            modelBuilder.Entity<JobPostings>().Ignore(value => value.ExperienceEmbedding);
            modelBuilder.Entity<JobPostings>().Ignore(value => value.DomainEmbedding);
            modelBuilder.Entity<Skills>().Ignore(value => value.Category);
            modelBuilder.Entity<Skills>().Ignore(value => value.Aliases);
        }
    }
}
