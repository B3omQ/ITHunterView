using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.DTOs.FeatureUsage;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.Service.Matching;
using ITHunterview.Service.UseCase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ITHunterview.Service.Tests.Matching;

public sealed class MatchingConsumerIsolationTests
{
    // =========================================================================
    // Scope Assignment Lifecycle Tests (R-01, R-04)
    // =========================================================================

    [Fact]
    public async Task SubmitAsync_NewOneToOneParent_SetsCandidateOneToOneScope()
    {
        var userId = Guid.NewGuid();
        var preflight = new Mock<IMatchingInputPreflightUseCase>();
        preflight.Setup(x => x.PrepareAsync(userId, It.IsAny<MatchingRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RawPreparedRequest("test cv content", "test jd content"));

        await using var context = CreateContext();
        var featureUsage = CreateFeatureUsageMock();
        var submission = CreateSubmission(context, preflight.Object, featureUsage.Object);

        var result = await submission.SubmitAsync(userId, RawRequest("test cv content", "test jd content"), "idemp-submit-1");

        result.IsExisting.Should().BeFalse();
        var job = await context.CvJobMatchScores.SingleAsync(j => j.Id == result.JobId);
        job.ProductScope.Should().Be(CvJobMatchProductScope.CandidateOneToOne);
    }

    [Fact]
    public async Task AutomaticRetry_ReusesRowAndPreservesCandidateOneToOneScope()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var existing = new CvJobMatchScores
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CvFileName = "cv.pdf",
            JdTitle = "Software Engineer",
            Status = "Pending",
            ProcessingStage = MatchingProcessingStages.Queued,
            MatchType = "AI",
            InputSnapshotJson = "{}",
            InputHash = "hash",
            IdempotencyKey = "key-1",
            IdempotencyRequestHash = "req-hash-1",
            AttemptCount = 0,
            MaxAttempts = 3,
            CreatedAt = now,
            UpdatedAt = now,
            NextAttemptAt = now,
            ProductScope = CvJobMatchProductScope.CandidateOneToOne
        };

        context.CvJobMatchScores.Add(existing);
        await context.SaveChangesAsync();

        // Simulate automatic retry by worker incrementing attempt and updating NextAttemptAt
        existing.AttemptCount++;
        existing.NextAttemptAt = DateTime.UtcNow.AddMinutes(2);
        existing.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        var reloaded = await context.CvJobMatchScores.SingleAsync(j => j.Id == existing.Id);
        reloaded.ProductScope.Should().Be(CvJobMatchProductScope.CandidateOneToOne);
        reloaded.AttemptCount.Should().Be(1);
    }

    [Fact]
    public async Task ManualRetry_NewChild_SetsCandidateOneToOneScope()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var failedJob = new CvJobMatchScores
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CvFileName = "cv.pdf",
            JdTitle = "Software Engineer",
            Status = "Failed",
            ProcessingStage = MatchingProcessingStages.Failed,
            ErrorCode = "AI_PROVIDER_TIMEOUT",
            ErrorMessage = "Timeout calling model",
            MatchType = "AI",
            InputSnapshotJson = "{\"snapshot\":\"data\"}",
            InputHash = "hash-123",
            IdempotencyKey = "key-parent",
            IdempotencyRequestHash = "req-parent",
            AttemptCount = 3,
            MaxAttempts = 3,
            CreatedAt = now,
            UpdatedAt = now,
            NextAttemptAt = now,
            ManualRetryUsed = false,
            ProductScope = CvJobMatchProductScope.CandidateOneToOne
        };

        context.CvJobMatchScores.Add(failedJob);
        await context.SaveChangesAsync();

        var jobRepo = new CvJdMatchingJobRepository(context);
        var featureUsage = CreateFeatureUsageMock();
        var retryUseCase = new CvJdMatchingRetryUseCase(context, jobRepo, featureUsage.Object);

        var retryResult = await retryUseCase.RetryAsync(userId, failedJob.Id, "key-retry-child-1");

        retryResult.IsExisting.Should().BeFalse();
        var childJob = await context.CvJobMatchScores.SingleAsync(j => j.Id == retryResult.JobId);
        childJob.ProductScope.Should().Be(CvJobMatchProductScope.CandidateOneToOne);
        childJob.RetryOfJobId.Should().Be(failedJob.Id);

        var reloadedFailedJob = await context.CvJobMatchScores.SingleAsync(j => j.Id == failedJob.Id);
        reloadedFailedJob.ManualRetryUsed.Should().BeTrue();
    }

    [Fact]
    public async Task TerminalRefundOrSoftHide_DoesNotClearCandidateOneToOneScope()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var job = new CvJobMatchScores
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CvFileName = "cv.pdf",
            JdTitle = "Software Engineer",
            Status = "Completed",
            ProcessingStage = MatchingProcessingStages.Completed,
            MatchScore = 88,
            MatchDetails = "{\"score\": 88}",
            MatchType = "AI",
            InputSnapshotJson = "{}",
            InputHash = "hash",
            IdempotencyKey = "key-1",
            IdempotencyRequestHash = "req-1",
            AttemptCount = 1,
            MaxAttempts = 3,
            CreatedAt = now,
            UpdatedAt = now,
            NextAttemptAt = now,
            ProductScope = CvJobMatchProductScope.CandidateOneToOne
        };

        context.CvJobMatchScores.Add(job);
        await context.SaveChangesAsync();

        // Simulate soft-hide
        job.HistoryHiddenAt = DateTime.UtcNow;
        job.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        var reloaded = await context.CvJobMatchScores.SingleAsync(j => j.Id == job.Id);
        reloaded.ProductScope.Should().Be(CvJobMatchProductScope.CandidateOneToOne);
        reloaded.HistoryHiddenAt.Should().NotBeNull();
    }

    // =========================================================================
    // Candidate History Isolation Tests (R-01, R-04, R-09, R-12)
    // =========================================================================

    [Fact]
    public async Task GetMatchHistoryAsync_ContainsOnlyOwnedOneToOneAiResults()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var ownedOneToOne = new CvJobMatchScores
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CvFileName = "my_cv.pdf",
            JdTitle = "Senior Dev",
            Status = "Completed",
            MatchScore = 90,
            MatchDetails = "{\"scorePercent\": 90}",
            MatchType = "AI",
            ProductScope = CvJobMatchProductScope.CandidateOneToOne,
            UpdatedAt = now
        };

        var otherUserJob = new CvJobMatchScores
        {
            Id = Guid.NewGuid(),
            UserId = otherUserId,
            CvFileName = "other_cv.pdf",
            JdTitle = "Senior Dev",
            Status = "Completed",
            MatchScore = 95,
            MatchDetails = "{\"scorePercent\": 95}",
            MatchType = "AI",
            ProductScope = CvJobMatchProductScope.CandidateOneToOne,
            UpdatedAt = now
        };

        context.CvJobMatchScores.AddRange(ownedOneToOne, otherUserJob);
        await context.SaveChangesAsync();

        var matchingUseCase = CreateMatchingUseCase(context);
        var history = await matchingUseCase.GetMatchHistoryAsync(userId, 1, 20);

        history.Items.Should().HaveCount(1);
        history.Items.Single().JobId.Should().Be(ownedOneToOne.Id);
    }

    [Fact]
    public async Task GetMatchHistoryAsync_HardcodeAndVectorLegacyRows_AreExcluded()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var ownedOneToOne = new CvJobMatchScores
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CvFileName = "my_cv.pdf",
            JdTitle = "Senior Dev",
            Status = "Completed",
            MatchScore = 90,
            MatchDetails = "{\"scorePercent\": 90}",
            MatchType = "AI",
            ProductScope = CvJobMatchProductScope.CandidateOneToOne,
            UpdatedAt = now
        };

        var hardcodeLegacy = new CvJobMatchScores
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CvFileName = "my_cv.pdf",
            JdTitle = "Senior Dev",
            Status = "Completed",
            MatchScore = 60,
            MatchDetails = "{\"scorePercent\": 60}",
            MatchType = "Hardcode",
            ProductScope = null,
            UpdatedAt = now.AddMinutes(-5)
        };

        var vectorLegacy = new CvJobMatchScores
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CvFileName = "my_cv.pdf",
            JdTitle = "Senior Dev",
            Status = "Completed",
            MatchScore = 75,
            MatchDetails = "{\"scorePercent\": 75}",
            MatchType = "Vector",
            ProductScope = null,
            UpdatedAt = now.AddMinutes(-10)
        };

        context.CvJobMatchScores.AddRange(ownedOneToOne, hardcodeLegacy, vectorLegacy);
        await context.SaveChangesAsync();

        var matchingUseCase = CreateMatchingUseCase(context);
        var history = await matchingUseCase.GetMatchHistoryAsync(userId, 1, 20);

        history.Items.Should().HaveCount(1);
        history.Items.Single().JobId.Should().Be(ownedOneToOne.Id);
    }

    [Fact]
    public async Task GetMatchHistoryAsync_NullScopeAiLegacyRow_IsExcludedWithoutInference()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var unclassifiedAiLegacy = new CvJobMatchScores
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CvFileName = "my_cv.pdf",
            JdTitle = "Senior Dev",
            Status = "Completed",
            MatchScore = 80,
            MatchDetails = "{\"scorePercent\": 80}",
            MatchType = "AI",
            ProductScope = null, // Null scope without reservation
            UpdatedAt = now
        };

        context.CvJobMatchScores.Add(unclassifiedAiLegacy);
        await context.SaveChangesAsync();

        var matchingUseCase = CreateMatchingUseCase(context);
        var history = await matchingUseCase.GetMatchHistoryAsync(userId, 1, 20);

        history.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMatchHistoryAsync_NullScopeWithValidOneToOneReservation_IsIncludedAsCompatibility()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var compatibleLegacyAi = new CvJobMatchScores
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CvFileName = "my_cv.pdf",
            JdTitle = "Senior Dev",
            Status = "Completed",
            MatchScore = 85,
            MatchDetails = "{\"scorePercent\": 85}",
            MatchType = "AI",
            ProductScope = null,
            UpdatedAt = now
        };

        var reservation = new FeatureUsageReservations
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FeatureKey = "CvJdMatching",
            ReferenceId = compatibleLegacyAi.Id,
            Source = "Coin",
            Status = "Captured",
            CreatedAt = now
        };

        context.CvJobMatchScores.Add(compatibleLegacyAi);
        context.FeatureUsageReservations.Add(reservation);
        await context.SaveChangesAsync();

        var matchingUseCase = CreateMatchingUseCase(context);
        var history = await matchingUseCase.GetMatchHistoryAsync(userId, 1, 20);

        history.Items.Should().HaveCount(1);
        history.Items.Single().JobId.Should().Be(compatibleLegacyAi.Id);
    }

    [Fact]
    public async Task GetMatchHistoryAsync_RecruiterScanForCandidate_IsExcluded()
    {
        await using var context = CreateContext();
        var candidateUserId = Guid.NewGuid();
        var recruiterUserId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var recruiterScanRun = new RecruiterCvScanRun
        {
            Id = Guid.NewGuid(),
            RecruiterUserId = recruiterUserId,
            JobId = Guid.NewGuid(),
            Status = MatchingScanRunStatus.Completed,
            CreatedAt = now,
            CompletedAt = now
        };

        var recruiterScanResult = new RecruiterCvScanResult
        {
            Id = Guid.NewGuid(),
            RunId = recruiterScanRun.Id,
            CvId = Guid.NewGuid(),
            CandidateUserId = candidateUserId,
            Rank = 1,
            MatchScore = 92
        };

        context.RecruiterCvScanRuns.Add(recruiterScanRun);
        context.RecruiterCvScanResults.Add(recruiterScanResult);
        await context.SaveChangesAsync();

        var matchingUseCase = CreateMatchingUseCase(context);
        var history = await matchingUseCase.GetMatchHistoryAsync(candidateUserId, 1, 20);

        // Recruiter scans are NOT in CvJobMatchScores and never appear in Candidate one-to-one history
        history.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task DashboardHistory_SourceCannotContainCandidateBulkOrRecruiterBulk()
    {
        await using var context = CreateContext();
        var candidateUserId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var candidateJobScanRun = new CandidateJobScanRun
        {
            Id = Guid.NewGuid(),
            CandidateUserId = candidateUserId,
            CvId = Guid.NewGuid(),
            Status = MatchingScanRunStatus.Completed,
            CreatedAt = now,
            CompletedAt = now
        };

        var candidateJobScanResult = new CandidateJobScanResult
        {
            Id = Guid.NewGuid(),
            RunId = candidateJobScanRun.Id,
            JobId = Guid.NewGuid(),
            Rank = 1,
            MatchScore = 88
        };

        context.CandidateJobScanRuns.Add(candidateJobScanRun);
        context.CandidateJobScanResults.Add(candidateJobScanResult);
        await context.SaveChangesAsync();

        var matchingUseCase = CreateMatchingUseCase(context);
        var dashboardHistory = await matchingUseCase.GetMatchHistoryAsync(candidateUserId, 1, 20);

        dashboardHistory.Items.Should().BeEmpty();
    }

    // =========================================================================
    // Learning Path Consumer Isolation Tests (R-01, R-12)
    // =========================================================================

    [Fact]
    public async Task ExtractFromCvJdAsync_CompletedOwnedHistoryVisibleOneToOne_Succeeds()
    {
        await using var context = CreateContext();
        var candidateId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var matchScore = new CvJobMatchScores
        {
            Id = Guid.NewGuid(),
            UserId = candidateId,
            CvFileName = "my_cv.pdf",
            JdTitle = "Fullstack .NET & React Engineer",
            Status = "Completed",
            MatchScore = 85,
            MatchDetails = "{\"scorePercent\": 85.0, \"reportKind\": \"structured\", \"matchMethod\": \"ai\", \"narrative\": \"Strong match\", \"requirements\": [], \"criticalGaps\": []}",
            MatchType = "AI",
            ProductScope = CvJobMatchProductScope.CandidateOneToOne,
            UpdatedAt = now
        };

        context.CvJobMatchScores.Add(matchScore);
        await context.SaveChangesAsync();

        var aiMock = new Mock<IAiService>();
        aiMock.Setup(x => x.GenerateTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("{\"customRoleName\": \".NET Dev\", \"customRoleDescription\": \"Backend role\", \"skills\": []}");

        var learningPathUseCase = CreateLearningPathUseCase(context, aiMock.Object);
        var profile = await learningPathUseCase.ExtractFromCvJdAsync(candidateId, matchScore.Id);

        profile.Should().NotBeNull();
        profile.CustomRoleName.Should().Be(".NET Dev");
    }

    [Fact]
    public async Task ExtractFromCvJdAsync_OwnCvHiddenFromRecruiters_StillSucceeds()
    {
        await using var context = CreateContext();
        var candidateId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var cv = new Cvs
        {
            Id = Guid.NewGuid(),
            UserId = candidateId,
            FileName = "hidden_cv.pdf",
            FileUrl = "https://storage.test/hidden_cv.pdf",
            FileType = "application/pdf",
            ParsedData = "CV text",
            IsPrimary = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        var matchScore = new CvJobMatchScores
        {
            Id = Guid.NewGuid(),
            UserId = candidateId,
            CvId = cv.Id,
            CvFileName = cv.FileName,
            JdTitle = "Backend Engineer",
            Status = "Completed",
            MatchScore = 80,
            MatchDetails = "{\"scorePercent\": 80.0, \"reportKind\": \"raw_text\", \"matchMethod\": \"ai\", \"narrative\": \"Good fit\"}",
            MatchType = "AI",
            ProductScope = CvJobMatchProductScope.CandidateOneToOne,
            UpdatedAt = now
        };

        context.Cvs.Add(cv);
        context.CvJobMatchScores.Add(matchScore);
        await context.SaveChangesAsync();

        var aiMock = new Mock<IAiService>();
        aiMock.Setup(x => x.GenerateTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("{\"customRoleName\": \"Backend Engineer\", \"customRoleDescription\": \"Backend role\", \"skills\": []}");

        var learningPathUseCase = CreateLearningPathUseCase(context, aiMock.Object);
        var profile = await learningPathUseCase.ExtractFromCvJdAsync(candidateId, matchScore.Id);

        profile.Should().NotBeNull();
        profile.CustomRoleName.Should().Be("Backend Engineer");
    }

    [Theory]
    [InlineData("foreign_candidate")]
    [InlineData("failed_status")]
    [InlineData("history_hidden")]
    [InlineData("null_scope_without_reservation")]
    public async Task ExtractFromCvJdAsync_BulkFailedHiddenOrForeignResult_Rejects(string reason)
    {
        await using var context = CreateContext();
        var candidateId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var matchScore = new CvJobMatchScores
        {
            Id = Guid.NewGuid(),
            UserId = reason == "foreign_candidate" ? otherUserId : candidateId,
            CvFileName = "cv.pdf",
            JdTitle = "Job",
            Status = reason == "failed_status" ? "Failed" : "Completed",
            MatchScore = 80,
            MatchDetails = "{\"scorePercent\": 80.0, \"reportKind\": \"raw_text\", \"matchMethod\": \"ai\"}",
            MatchType = "AI",
            HistoryHiddenAt = reason == "history_hidden" ? now : null,
            ProductScope = reason == "null_scope_without_reservation" ? null : CvJobMatchProductScope.CandidateOneToOne,
            UpdatedAt = now
        };

        context.CvJobMatchScores.Add(matchScore);
        await context.SaveChangesAsync();

        var aiMock = new Mock<IAiService>();
        var learningPathUseCase = CreateLearningPathUseCase(context, aiMock.Object);

        var action = () => learningPathUseCase.ExtractFromCvJdAsync(candidateId, matchScore.Id);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Chưa có dữ liệu matching CV-JD.");
    }

    // =========================================================================
    // Helpers & Context
    // =========================================================================

    private static CvJdMatchingSubmissionUseCase CreateSubmission(
        ITHunterviewContext context,
        IMatchingInputPreflightUseCase preflight,
        ICandidateFeatureUsageUseCase featureUsage)
    {
        var sourceRepository = new Mock<IMatchingSourceRepository>(MockBehavior.Strict);
        return new CvJdMatchingSubmissionUseCase(
            context,
            new MatchingRequestValidator(),
            preflight,
            new MatchingInputSnapshotBuilder(sourceRepository.Object),
            new CvJdMatchingJobRepository(context),
            featureUsage);
    }

    private static CvJobMatchingUseCase CreateMatchingUseCase(ITHunterviewContext context)
    {
        var aiEmbedding = new Mock<IAiEmbeddingService>();
        var extractor = new Mock<ICvTextExtractorService>();
        var prompt = new Mock<IPromptManagementService>();
        var textAi = new Mock<IAiService>();
        var featureUsage = new Mock<ICandidateFeatureUsageUseCase>();
        var preflight = new Mock<IMatchingInputPreflightUseCase>();
        var sourceRepo = new Mock<IMatchingSourceRepository>();
        var validator = new Mock<ICvAnalysisResponseValidator>();

        return new CvJobMatchingUseCase(
            context,
            aiEmbedding.Object,
            extractor.Object,
            NullLogger<CvJobMatchingUseCase>.Instance,
            prompt.Object,
            textAi.Object,
            featureUsage.Object,
            preflight.Object,
            sourceRepo.Object,
            validator.Object);
    }

    private static LearningPathUseCase CreateLearningPathUseCase(ITHunterviewContext context, IAiService aiService)
    {
        var lpRepo = new Mock<ILearningPathRepository>();
        var answerRepo = new Mock<IInterviewAnswerRepository>();
        var sessionRepo = new Mock<IInterviewSessionRepository>();

        return new LearningPathUseCase(
            lpRepo.Object,
            answerRepo.Object,
            sessionRepo.Object,
            aiService,
            context);
    }

    private static Mock<ICandidateFeatureUsageUseCase> CreateFeatureUsageMock()
    {
        var mock = new Mock<ICandidateFeatureUsageUseCase>();
        mock.Setup(x => x.ReserveFeatureAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid userId, string featureKey, Guid referenceId, CancellationToken _) =>
                new FeatureReservationResult(Guid.NewGuid(), referenceId, featureKey, "Coin", "Reserved", 1000, null));
        return mock;
    }

    private static MatchingRequestDto RawRequest(string cvText, string jdText)
        => new() { CvText = cvText.PadRight(100, 'c'), RawJdText = jdText.PadRight(100, 'j') };

    private static PreparedMatchingRequest RawPreparedRequest(string cvText, string jdText)
        => new(
            new PreparedRawCvSource(cvText.PadRight(100, 'c'), "cv.pdf"),
            new PreparedRawJdSource(jdText.PadRight(100, 'j'), "JD"),
            MatchingMode.JdFit);

    private static ITHunterviewContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ITHunterviewContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ConsumerIsolationTestContext(options);
    }

    private sealed class ConsumerIsolationTestContext : ITHunterviewContext
    {
        public ConsumerIsolationTestContext(DbContextOptions<ITHunterviewContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Cvs>().Ignore(c => c.TitleEmbedding);
            modelBuilder.Entity<Cvs>().Ignore(c => c.SkillsEmbedding);
            modelBuilder.Entity<Cvs>().Ignore(c => c.ExperienceEmbedding);
            modelBuilder.Entity<Cvs>().Ignore(c => c.DomainEmbedding);
            modelBuilder.Entity<JobPostings>().Ignore(j => j.TitleEmbedding);
            modelBuilder.Entity<JobPostings>().Ignore(j => j.SkillsEmbedding);
            modelBuilder.Entity<JobPostings>().Ignore(j => j.ExperienceEmbedding);
            modelBuilder.Entity<JobPostings>().Ignore(j => j.DomainEmbedding);
        }
    }
}
