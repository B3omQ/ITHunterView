using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Exceptions;
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

public sealed class CvJdMatchingWorkerUseCaseTests
{
    [Fact]
    public async Task ClaimRunnableJobs_ClaimsPendingJobWithLeaseAndAttempt()
    {
        await using var context = CreateContext();
        var job = CreateJob();
        context.CvJobMatchScores.Add(job);
        await context.SaveChangesAsync();

        var repository = new CvJdMatchingJobRepository(context);
        var claimed = await repository.ClaimRunnableJobsAsync(
            10,
            "worker-a",
            UtcNow,
            CvJdMatchingWorkerUseCase.LeaseDuration);

        claimed.Should().ContainSingle().Which.JobId.Should().Be(job.Id);
        job.Status.Should().Be("Processing");
        job.AttemptCount.Should().Be(1);
        job.LeaseOwner.Should().Be("worker-a");
        job.LeaseToken.Should().Be(claimed[0].LeaseToken);
        job.LeaseExpiresAt.Should().Be(UtcNow.Add(CvJdMatchingWorkerUseCase.LeaseDuration));
    }

    [Fact]
    public async Task ProcessClaimedJob_CompletesOnlyWhenLeaseStillBelongsToWorker()
    {
        await using var context = CreateContext();
        var job = CreateJob();
        context.CvJobMatchScores.Add(job);
        await context.SaveChangesAsync();

        var processor = new Mock<ICvJdOneToOneMatchingProcessor>(MockBehavior.Strict);
        var coverage = new CvAnalysisCoverage(
            2, 1, 1,
            4, 3, 1,
            2, 2, 0,
            true, true, true, false);
        var diagnostics = new[] { new CvAnalysisDiagnostic("DOMAIN_METRIC_MISSING", "$.matching_metrics.domains") };
        processor.Setup(x => x.ExecuteAsync(job.Id, It.IsAny<MatchingInputSnapshotV1>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CvJdMatchingExecutionResult(
                0.82m,
                "details",
                "sfia",
                CvAnalysisQuality.PARTIAL,
                coverage,
                diagnostics,
                JdAnalysisQuality: JdAnalysisQuality.INVALID,
                JdAnalysisDiagnostics: new[] { new ITHunterview.Service.DTOs.JobAnalysis.JdAnalysisDiagnostic("INVALID_JSON_FORMAT", "$") }));
        var featureUsage = CreateFeatureUsageMock();
        var worker = CreateWorker(context, processor.Object, featureUsage.Object);
        var repository = new CvJdMatchingJobRepository(context);
        var claimed = (await repository.ClaimRunnableJobsAsync(1, "worker-a", UtcNow, CvJdMatchingWorkerUseCase.LeaseDuration))[0];

        await worker.ProcessClaimedJobAsync(job.Id, "worker-a", claimed.LeaseToken);

        job.Status.Should().Be("Completed");
        job.MatchScore.Should().Be(0.82m);
        job.MatchDetails.Should().Be("details");
        job.SfiaExtractResult.Should().Be("sfia");
        job.CvAnalysisQuality.Should().Be(CvAnalysisQuality.PARTIAL);
        job.CvAnalysisCoverageJson.Should().Contain("accepted_experience_entry_count");
        job.CvAnalysisDiagnosticsJson.Should().Contain("DOMAIN_METRIC_MISSING");
        job.JdAnalysisQuality.Should().Be(JdAnalysisQuality.INVALID);
        job.JdAnalysisDiagnosticsJson.Should().Contain("INVALID_JSON_FORMAT");
        job.LeaseToken.Should().BeNull();
        featureUsage.Verify(x => x.RefundFeatureReservationAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessClaimedJob_SourceAnalysisPersistenceFailure_DoesNotUndoCompletedMatch()
    {
        await using var context = CreateContext();
        var job = CreateJob();
        context.CvJobMatchScores.Add(job);
        await context.SaveChangesAsync();

        var processor = new Mock<ICvJdOneToOneMatchingProcessor>(MockBehavior.Strict);
        processor.Setup(x => x.ExecuteAsync(job.Id, It.IsAny<MatchingInputSnapshotV1>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CvJdMatchingExecutionResult(
                0.82m,
                "details",
                null,
                CvPersistenceIntent: new CvAnalysisPersistenceIntent(
                    Guid.NewGuid(), job.UserId, "source", "analysis", "{}",
                    CvAnalysisQuality.COMPLETE, null, null)));
        var sourcePersistence = new Mock<IMatchingSourceAnalysisPersistence>(MockBehavior.Strict);
        sourcePersistence.Setup(service => service.TryPersistCvAsync(
                It.IsAny<CvAnalysisPersistenceIntent>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("CACHE_WRITE_FAILED"));
        var featureUsage = CreateFeatureUsageMock();
        var worker = CreateWorker(context, processor.Object, featureUsage.Object, sourcePersistence.Object);
        var repository = new CvJdMatchingJobRepository(context);
        var claimed = (await repository.ClaimRunnableJobsAsync(1, "worker-a", UtcNow, CvJdMatchingWorkerUseCase.LeaseDuration))[0];

        await worker.ProcessClaimedJobAsync(job.Id, "worker-a", claimed.LeaseToken);

        job.Status.Should().Be("Completed");
        featureUsage.Verify(service => service.RefundFeatureReservationAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        sourcePersistence.VerifyAll();
    }

    [Fact]
    public async Task ProcessClaimedJob_RetryableTimeoutSchedulesRetryWithoutRefund()
    {
        await using var context = CreateContext();
        var job = CreateJob();
        context.CvJobMatchScores.Add(job);
        await context.SaveChangesAsync();

        var processor = new Mock<ICvJdOneToOneMatchingProcessor>(MockBehavior.Strict);
        processor.Setup(x => x.ExecuteAsync(job.Id, It.IsAny<MatchingInputSnapshotV1>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException());
        var featureUsage = CreateFeatureUsageMock();
        var worker = CreateWorker(context, processor.Object, featureUsage.Object);
        var repository = new CvJdMatchingJobRepository(context);
        var claimed = (await repository.ClaimRunnableJobsAsync(1, "worker-a", UtcNow, CvJdMatchingWorkerUseCase.LeaseDuration))[0];

        await worker.ProcessClaimedJobAsync(job.Id, "worker-a", claimed.LeaseToken);

        job.Status.Should().Be("RetryScheduled");
        job.ErrorCode.Should().Be("AI_PROVIDER_TIMEOUT");
        job.NextAttemptAt.Should().NotBeNull();
        featureUsage.Verify(x => x.RefundFeatureReservationAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessClaimedJob_ProviderUnauthorizedFailsAsConfigurationAndRefundsOnce()
    {
        await using var context = CreateContext();
        var job = CreateJob();
        context.CvJobMatchScores.Add(job);
        await context.SaveChangesAsync();

        var processor = new Mock<ICvJdOneToOneMatchingProcessor>(MockBehavior.Strict);
        processor.Setup(x => x.ExecuteAsync(job.Id, It.IsAny<MatchingInputSnapshotV1>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException(
                "Provider authentication failed.",
                inner: null,
                HttpStatusCode.Unauthorized));
        var featureUsage = CreateFeatureUsageMock();
        var worker = CreateWorker(context, processor.Object, featureUsage.Object);
        var repository = new CvJdMatchingJobRepository(context);
        var claimed = (await repository.ClaimRunnableJobsAsync(
            1,
            "worker-a",
            UtcNow,
            CvJdMatchingWorkerUseCase.LeaseDuration))[0];

        await worker.ProcessClaimedJobAsync(job.Id, "worker-a", claimed.LeaseToken);

        job.Status.Should().Be("Failed");
        job.ErrorCode.Should().Be("MATCHING_CONFIGURATION_INVALID");
        job.NextAttemptAt.Should().BeNull();
        featureUsage.Verify(x => x.RefundFeatureReservationAsync(
            job.UserId,
            job.Id,
            "matching_configuration_invalid",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ClaimRunnableJobs_DoesNotReprocessCompletedJob()
    {
        await using var context = CreateContext();
        var job = CreateJob();
        job.Status = "Completed";
        job.MatchScore = 84m;
        job.MatchDetails = "{\"contract\":\"jd-matching/v4\"}";
        context.CvJobMatchScores.Add(job);
        await context.SaveChangesAsync();

        var repository = new CvJdMatchingJobRepository(context);

        var claimed = await repository.ClaimRunnableJobsAsync(
            1,
            "worker-a",
            UtcNow,
            CvJdMatchingWorkerUseCase.LeaseDuration);

        claimed.Should().BeEmpty();
        job.Status.Should().Be("Completed");
        job.MatchScore.Should().Be(84m);
        job.MatchDetails.Should().Be("{\"contract\":\"jd-matching/v4\"}");
    }

    [Fact]
    public async Task ProcessClaimedJob_InvalidStageTwoContractFailsFirstAttemptAndRefunds()
    {
        await using var context = CreateContext();
        var job = CreateJob();
        context.CvJobMatchScores.Add(job);
        await context.SaveChangesAsync();

        var processor = new Mock<ICvJdOneToOneMatchingProcessor>(MockBehavior.Strict);
        processor.Setup(x => x.ExecuteAsync(job.Id, It.IsAny<MatchingInputSnapshotV1>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("MATCHING_STAGE2_OUTPUT_INVALID"));
        var featureUsage = CreateFeatureUsageMock();
        var worker = CreateWorker(context, processor.Object, featureUsage.Object);
        var repository = new CvJdMatchingJobRepository(context);
        var claimed = (await repository.ClaimRunnableJobsAsync(
            1,
            "worker-a",
            UtcNow,
            CvJdMatchingWorkerUseCase.LeaseDuration))[0];

        await worker.ProcessClaimedJobAsync(job.Id, "worker-a", claimed.LeaseToken);

        job.AttemptCount.Should().Be(1);
        job.Status.Should().Be("Failed");
        job.ErrorCode.Should().Be("AI_OUTPUT_INVALID");
        job.CvAnalysisQuality.Should().BeNull();
        job.NextAttemptAt.Should().BeNull();
        featureUsage.Verify(x => x.RefundFeatureReservationAsync(
            job.UserId,
            job.Id,
            "ai_output_invalid",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessClaimedJob_CvSchemaFailureFailsFirstAttemptAndRefunds()
    {
        await using var context = CreateContext();
        var job = CreateJob();
        context.CvJobMatchScores.Add(job);
        await context.SaveChangesAsync();

        var validationFailure = CvAnalysisValidationResult.Invalid(
            "CV_ANALYSIS_SCHEMA_INVALID",
            "TYPED_DESERIALIZATION_FAILED",
            "$.matching_metrics");
        var processor = new Mock<ICvJdOneToOneMatchingProcessor>(MockBehavior.Strict);
        processor.Setup(x => x.ExecuteAsync(job.Id, It.IsAny<MatchingInputSnapshotV1>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CvAnalysisValidationException(validationFailure));
        var featureUsage = CreateFeatureUsageMock();
        var worker = CreateWorker(context, processor.Object, featureUsage.Object);
        var repository = new CvJdMatchingJobRepository(context);
        var claimed = (await repository.ClaimRunnableJobsAsync(
            1,
            "worker-a",
            UtcNow,
            CvJdMatchingWorkerUseCase.LeaseDuration))[0];

        await worker.ProcessClaimedJobAsync(job.Id, "worker-a", claimed.LeaseToken);

        job.AttemptCount.Should().Be(1);
        job.Status.Should().Be("Failed");
        job.ErrorCode.Should().Be("AI_OUTPUT_INVALID");
        job.CvAnalysisQuality.Should().Be(CvAnalysisQuality.INVALID);
        job.NextAttemptAt.Should().BeNull();
        featureUsage.Verify(x => x.RefundFeatureReservationAsync(
            job.UserId,
            job.Id,
            "ai_output_invalid",
            It.IsAny<CancellationToken>()), Times.Once);

        var laterClaims = await repository.ClaimRunnableJobsAsync(
            1,
            "worker-b",
            UtcNow.AddHours(1),
            CvJdMatchingWorkerUseCase.LeaseDuration);
        laterClaims.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessClaimedJob_InvalidSnapshotFailsAndRefundsReservation()
    {
        await using var context = CreateContext();
        var job = CreateJob();
        job.InputSnapshotJson = "{\"schemaVersion\":\"not-supported\"}";
        context.CvJobMatchScores.Add(job);
        await context.SaveChangesAsync();

        var featureUsage = CreateFeatureUsageMock();
        var worker = CreateWorker(
            context,
            new Mock<ICvJdOneToOneMatchingProcessor>(MockBehavior.Strict).Object,
            featureUsage.Object);
        var repository = new CvJdMatchingJobRepository(context);
        var claimed = (await repository.ClaimRunnableJobsAsync(1, "worker-a", UtcNow, CvJdMatchingWorkerUseCase.LeaseDuration))[0];

        await worker.ProcessClaimedJobAsync(job.Id, "worker-a", claimed.LeaseToken);

        job.Status.Should().Be("Failed");
        job.ErrorCode.Should().Be("SNAPSHOT_INVALID");
        featureUsage.Verify(x => x.RefundFeatureReservationAsync(
            job.UserId,
            job.Id,
            "snapshot_invalid",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessClaimedJob_WhenSnapshotHashChanges_FailsWithIntegrityCode()
    {
        await using var context = CreateContext();
        var job = CreateJob();
        job.InputHash = new string('0', 64);
        context.CvJobMatchScores.Add(job);
        await context.SaveChangesAsync();

        var featureUsage = CreateFeatureUsageMock();
        var worker = CreateWorker(
            context,
            new Mock<ICvJdOneToOneMatchingProcessor>(MockBehavior.Strict).Object,
            featureUsage.Object);
        var repository = new CvJdMatchingJobRepository(context);
        var claimed = (await repository.ClaimRunnableJobsAsync(1, "worker-a", UtcNow, CvJdMatchingWorkerUseCase.LeaseDuration))[0];

        await worker.ProcessClaimedJobAsync(job.Id, "worker-a", claimed.LeaseToken);

        job.Status.Should().Be("Failed");
        job.ErrorCode.Should().Be("SNAPSHOT_HASH_MISMATCH");
        featureUsage.Verify(x => x.RefundFeatureReservationAsync(
            job.UserId,
            job.Id,
            "snapshot_hash_mismatch",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecoverExpiredLease_SchedulesRetryUntilMaxAttemptsThenFailsAndRefunds()
    {
        await using var context = CreateContext();
        var first = CreateJob();
        first.Status = "Processing";
        first.AttemptCount = 1;
        first.LeaseOwner = "dead-worker";
        first.LeaseToken = Guid.NewGuid();
        first.LeaseExpiresAt = UtcNow.AddMinutes(-1);
        var second = CreateJob();
        second.Status = "Processing";
        second.AttemptCount = 3;
        second.MaxAttempts = 3;
        second.LeaseOwner = "dead-worker";
        second.LeaseToken = Guid.NewGuid();
        second.LeaseExpiresAt = UtcNow.AddMinutes(-1);
        context.CvJobMatchScores.AddRange(first, second);
        await context.SaveChangesAsync();

        var featureUsage = CreateFeatureUsageMock();
        var worker = CreateWorker(
            context,
            new Mock<ICvJdOneToOneMatchingProcessor>(MockBehavior.Strict).Object,
            featureUsage.Object);

        await worker.RecoverExpiredLeasesAsync(UtcNow);

        first.Status.Should().Be("RetryScheduled");
        first.ErrorCode.Should().Be("LEASE_EXPIRED");
        second.Status.Should().Be("Failed");
        second.ErrorCode.Should().Be("LEASE_EXPIRED");
        featureUsage.Verify(x => x.RefundFeatureReservationAsync(
            second.UserId,
            second.Id,
            "lease_expired",
            It.IsAny<CancellationToken>()), Times.Once);
        featureUsage.Verify(x => x.RefundFeatureReservationAsync(
            first.UserId,
            first.Id,
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecoverExpiredLease_RecoversLegacyProcessingRowWithoutLease()
    {
        await using var context = CreateContext();
        var job = CreateJob();
        job.Status = "Processing";
        job.LeaseOwner = null;
        job.LeaseToken = null;
        job.LeaseExpiresAt = null;
        job.AttemptCount = 1;
        context.CvJobMatchScores.Add(job);
        await context.SaveChangesAsync();

        var featureUsage = CreateFeatureUsageMock();
        var worker = CreateWorker(
            context,
            new Mock<ICvJdOneToOneMatchingProcessor>(MockBehavior.Strict).Object,
            featureUsage.Object);

        await worker.RecoverExpiredLeasesAsync(UtcNow);

        job.Status.Should().Be("RetryScheduled");
        job.ErrorCode.Should().Be("LEASE_EXPIRED");
        job.LeaseOwner.Should().BeNull();
        featureUsage.Verify(x => x.RefundFeatureReservationAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteAsync_RejectsStaleLeaseToken()
    {
        await using var context = CreateContext();
        var job = CreateJob();
        job.Status = "Processing";
        job.LeaseOwner = "worker-a";
        job.LeaseToken = Guid.NewGuid();
        job.LeaseExpiresAt = UtcNow.AddMinutes(1);
        context.CvJobMatchScores.Add(job);
        await context.SaveChangesAsync();

        var repository = new CvJdMatchingJobRepository(context);
        var completed = await repository.CompleteAsync(
            job.Id,
            "worker-a",
            Guid.NewGuid(),
            0.5m,
            "should not write",
            null,
            null,
            null,
            null,
            UtcNow);

        completed.Should().BeFalse();
        job.Status.Should().Be("Processing");
        job.MatchScore.Should().BeNull();
    }

    private static CvJdMatchingWorkerUseCase CreateWorker(
        ITHunterviewContext context,
        ICvJdOneToOneMatchingProcessor processor,
        ICandidateFeatureUsageUseCase featureUsage,
        IMatchingSourceAnalysisPersistence? sourceAnalysisPersistence = null)
        => new(
            context,
            new CvJdMatchingJobRepository(context),
            processor,
            featureUsage,
            NullLogger<CvJdMatchingWorkerUseCase>.Instance,
            sourceAnalysisPersistence);

    private static Mock<ICandidateFeatureUsageUseCase> CreateFeatureUsageMock()
    {
        var mock = new Mock<ICandidateFeatureUsageUseCase>();
        mock.Setup(x => x.RefundFeatureReservationAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    private static CvJobMatchScores CreateJob()
    {
        var now = UtcNow;
        var userId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        return new CvJobMatchScores
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            JobId = jobId,
            MatchType = "AI",
            Status = "Pending",
            MatchDetails = string.Empty,
            CreatedAt = now,
            UpdatedAt = now,
            MaxAttempts = 3,
            InputSnapshotJson = SnapshotJson(),
            InputHash = MatchingInputSnapshotIntegrity.ComputeHash(CreateSnapshot())
        };
    }

    private static string SnapshotJson()
    {
        return JsonSerializer.Serialize(CreateSnapshot(), new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        });
    }

    private static MatchingInputSnapshotV1 CreateSnapshot()
    {
        return new MatchingInputSnapshotV1(
            MatchingInputSnapshotBuilder.SchemaVersion,
            MatchingMode.JdFit,
            new MatchingCvSnapshot("raw", null, "cv.pdf", "candidate text", null, null),
            new MatchingJdSnapshot("raw", null, "Engineer", "job text", null, null),
            UtcNow);
    }

    private static DateTime UtcNow => new(2026, 8, 2, 8, 0, 0, DateTimeKind.Utc);

    private static ITHunterviewContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ITHunterviewContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new WorkerTestContext(options);
    }

    private sealed class WorkerTestContext : ITHunterviewContext
    {
        public WorkerTestContext(DbContextOptions<ITHunterviewContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                         .Where(type => type.ClrType != typeof(CvJobMatchScores)
                                        && type.ClrType != typeof(FeatureUsageReservations))
                         .Select(type => type.ClrType)
                         .Distinct()
                         .ToList())
            {
                modelBuilder.Ignore(entityType);
            }
        }
    }
}
