using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Tests.Persistence;

public sealed class RecruiterCvUnlockRepositoryContractTests
{
    [Fact]
    [Trait("Requirement", "R-10")]
    public async Task AcquirePendingAsync_FirstCaller_CreatesPendingLedger()
    {
        await using var context = MatchingScanInMemoryContextFactory.Create();
        var repository = new RecruiterCvUnlockRepository(context);
        var recruiterUserId = Guid.NewGuid();
        var cvId = Guid.NewGuid();
        var scanResultId = Guid.NewGuid();
        var jobId = Guid.NewGuid();

        var (ledger, isOwner) = await repository.AcquirePendingAsync(
            recruiterUserId, cvId, scanResultId, jobId, CancellationToken.None);

        isOwner.Should().BeTrue();
        ledger.Status.Should().Be(RecruiterCvUnlockStatus.Pending);
        ledger.RecruiterId.Should().Be(recruiterUserId);
        ledger.CvId.Should().Be(cvId);
        ledger.SourceScanResultId.Should().Be(scanResultId);
        ledger.JobId.Should().Be(jobId);

        var persisted = await context.RecruiterUnlockedCvs.SingleAsync(u => u.Id == ledger.Id);
        persisted.Status.Should().Be(RecruiterCvUnlockStatus.Pending);
    }

    [Fact]
    [Trait("Requirement", "R-10")]
    public async Task AcquirePendingAsync_AlreadyCompleted_ReturnsExistingWithoutCaptureOwnership()
    {
        await using var context = MatchingScanInMemoryContextFactory.Create();
        var recruiterUserId = Guid.NewGuid();
        var cvId = Guid.NewGuid();
        var existing = new RecruiterUnlockedCvs
        {
            Id = Guid.NewGuid(),
            RecruiterId = recruiterUserId,
            CvId = cvId,
            Status = RecruiterCvUnlockStatus.Completed,
            SnapshotStorageKey = "retained-unlocks/existing",
            SnapshotFileName = "cv.pdf",
            SnapshotContentHash = "HASH",
            SnapshotCreatedAt = DateTime.UtcNow,
            UnlockedVia = "SUBSCRIPTION",
            CoinsSpent = 0,
            UnlockedAt = DateTime.UtcNow
        };
        await context.RecruiterUnlockedCvs.AddAsync(existing);
        await context.SaveChangesAsync();

        var repository = new RecruiterCvUnlockRepository(context);
        var (ledger, isOwner) = await repository.AcquirePendingAsync(
            recruiterUserId, cvId, Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        isOwner.Should().BeFalse();
        ledger.Id.Should().Be(existing.Id);
        ledger.Status.Should().Be(RecruiterCvUnlockStatus.Completed);
    }

    [Fact]
    [Trait("Requirement", "R-10")]
    [Trait("Requirement", "R-11")]
    public async Task CompleteAsync_TransitionsPendingToCompletedWithSnapshot()
    {
        await using var context = MatchingScanInMemoryContextFactory.Create();
        var unlockId = Guid.NewGuid();
        var recruiterUserId = Guid.NewGuid();
        var cvId = Guid.NewGuid();
        var pending = new RecruiterUnlockedCvs
        {
            Id = unlockId,
            RecruiterId = recruiterUserId,
            CvId = cvId,
            Status = RecruiterCvUnlockStatus.Pending,
            UnlockedVia = "PENDING",
            CoinsSpent = 0,
            UnlockedAt = DateTime.UtcNow
        };
        await context.RecruiterUnlockedCvs.AddAsync(pending);
        await context.SaveChangesAsync();

        var repository = new RecruiterCvUnlockRepository(context);
        var snapshot = new RetainedCvSnapshot(
            $"retained-unlocks/{unlockId}",
            "candidate.pdf",
            "SHA256HASH",
            DateTime.UtcNow);

        var completed = await repository.CompleteAsync(
            unlockId, snapshot, "COINS", 50, DateTime.UtcNow, CancellationToken.None);

        completed.Should().BeTrue();

        var persisted = await context.RecruiterUnlockedCvs.AsNoTracking().SingleAsync(u => u.Id == unlockId);
        persisted.Status.Should().Be(RecruiterCvUnlockStatus.Completed);
        persisted.SnapshotStorageKey.Should().Be(snapshot.StorageKey);
        persisted.SnapshotFileName.Should().Be(snapshot.FileName);
        persisted.SnapshotContentHash.Should().Be(snapshot.ContentHash);
        persisted.CoinsSpent.Should().Be(50);
        persisted.UnlockedVia.Should().Be("COINS");
    }

    [Fact]
    [Trait("Requirement", "R-10")]
    public async Task CompleteAsync_NotPending_ReturnsFalse()
    {
        await using var context = MatchingScanInMemoryContextFactory.Create();
        var unlockId = Guid.NewGuid();
        var completedRow = new RecruiterUnlockedCvs
        {
            Id = unlockId,
            RecruiterId = Guid.NewGuid(),
            CvId = Guid.NewGuid(),
            Status = RecruiterCvUnlockStatus.Completed,
            UnlockedVia = "SUBSCRIPTION",
            CoinsSpent = 0,
            UnlockedAt = DateTime.UtcNow
        };
        await context.RecruiterUnlockedCvs.AddAsync(completedRow);
        await context.SaveChangesAsync();

        var repository = new RecruiterCvUnlockRepository(context);
        var snapshot = new RetainedCvSnapshot(
            $"retained-unlocks/{unlockId}",
            "candidate.pdf",
            "SHA256HASH",
            DateTime.UtcNow);

        var result = await repository.CompleteAsync(
            unlockId, snapshot, "COINS", 50, DateTime.UtcNow, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    [Trait("Requirement", "R-11")]
    public async Task FailAsync_TransitionsPendingToFailedWithBoundedCode()
    {
        await using var context = MatchingScanInMemoryContextFactory.Create();
        var unlockId = Guid.NewGuid();
        var pending = new RecruiterUnlockedCvs
        {
            Id = unlockId,
            RecruiterId = Guid.NewGuid(),
            CvId = Guid.NewGuid(),
            Status = RecruiterCvUnlockStatus.Pending,
            UnlockedVia = "PENDING",
            CoinsSpent = 0,
            UnlockedAt = DateTime.UtcNow
        };
        await context.RecruiterUnlockedCvs.AddAsync(pending);
        await context.SaveChangesAsync();

        var repository = new RecruiterCvUnlockRepository(context);
        await repository.FailAsync(unlockId, "CV_DELETED_BEFORE_CAPTURE", CancellationToken.None);

        var persisted = await context.RecruiterUnlockedCvs.AsNoTracking().SingleAsync(u => u.Id == unlockId);
        persisted.Status.Should().Be(RecruiterCvUnlockStatus.Failed);
        persisted.FailureCode.Should().Be("CV_DELETED_BEFORE_CAPTURE");
    }

    [Fact]
    [Trait("Requirement", "R-10")]
    public async Task FailedCapture_RetryReusesLedgerIdentityWithoutPriorCharge()
    {
        await using var context = MatchingScanInMemoryContextFactory.Create();
        var unlockId = Guid.NewGuid();
        var recruiterUserId = Guid.NewGuid();
        var cvId = Guid.NewGuid();
        var failed = new RecruiterUnlockedCvs
        {
            Id = unlockId,
            RecruiterId = recruiterUserId,
            CvId = cvId,
            Status = RecruiterCvUnlockStatus.Failed,
            FailureCode = "TEMPORARY_NETWORK_FAILURE",
            UnlockedVia = "PENDING",
            CoinsSpent = 0,
            UnlockedAt = DateTime.UtcNow.AddMinutes(-5)
        };
        await context.RecruiterUnlockedCvs.AddAsync(failed);
        await context.SaveChangesAsync();

        var repository = new RecruiterCvUnlockRepository(context);
        var (ledger, isOwner) = await repository.AcquirePendingAsync(
            recruiterUserId, cvId, Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        isOwner.Should().BeTrue();
        ledger.Id.Should().Be(unlockId);
        ledger.Status.Should().Be(RecruiterCvUnlockStatus.Pending);
        ledger.FailureCode.Should().BeNull();
    }

    [Fact]
    [Trait("Requirement", "R-10")]
    public async Task GetUnlockedCvIdsAsync_ReturnsOnlyCompletedUnlockedCvs()
    {
        await using var context = MatchingScanInMemoryContextFactory.Create();
        var recruiterUserId = Guid.NewGuid();
        var cv1 = Guid.NewGuid();
        var cv2 = Guid.NewGuid();
        var cv3 = Guid.NewGuid();

        await context.RecruiterUnlockedCvs.AddRangeAsync(
            new RecruiterUnlockedCvs
            {
                Id = Guid.NewGuid(),
                RecruiterId = recruiterUserId,
                CvId = cv1,
                Status = RecruiterCvUnlockStatus.Completed,
                UnlockedVia = "COINS",
                CoinsSpent = 50,
                UnlockedAt = DateTime.UtcNow
            },
            new RecruiterUnlockedCvs
            {
                Id = Guid.NewGuid(),
                RecruiterId = recruiterUserId,
                CvId = cv2,
                Status = RecruiterCvUnlockStatus.Pending,
                UnlockedVia = "PENDING",
                CoinsSpent = 0,
                UnlockedAt = DateTime.UtcNow
            },
            new RecruiterUnlockedCvs
            {
                Id = Guid.NewGuid(),
                RecruiterId = Guid.NewGuid(), // other recruiter
                CvId = cv3,
                Status = RecruiterCvUnlockStatus.Completed,
                UnlockedVia = "COINS",
                CoinsSpent = 50,
                UnlockedAt = DateTime.UtcNow
            });
        await context.SaveChangesAsync();

        var repository = new RecruiterCvUnlockRepository(context);
        var unlocked = await repository.GetUnlockedCvIdsAsync(
            recruiterUserId, new[] { cv1, cv2, cv3 }, CancellationToken.None);

        unlocked.Should().ContainSingle(id => id == cv1);
    }
}

[Collection(MatchingScanPostgresCollection.Name)]
public sealed class RecruiterCvUnlockRepositoryPostgresTests
{
    private readonly MatchingScanPostgresFixture _fixture;

    public RecruiterCvUnlockRepositoryPostgresTests(MatchingScanPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Task6PostgresFact]
    [Trait("Requirement", "R-10")]
    public async Task AcquireAsync_TwoConcurrentRequests_ReturnSameLedgerAndOneCaptureOwner()
    {
        var seed = await _fixture.SeedGraphAsync();
        var updateBarrier = new MatchingScanUpdateBarrier("recruiter_unlocked_cvs");
        await using var firstContext = _fixture.CreateContext(updateBarrier.CreateParticipant());
        await using var secondContext = _fixture.CreateContext(updateBarrier.CreateParticipant());
        var firstRepository = new RecruiterCvUnlockRepository(firstContext);
        var secondRepository = new RecruiterCvUnlockRepository(secondContext);

        var first = Task.Run(() => firstRepository.AcquirePendingAsync(
            seed.RecruiterUserId, seed.CvId, null, seed.JobId, CancellationToken.None));
        var second = Task.Run(() => secondRepository.AcquirePendingAsync(
            seed.RecruiterUserId, seed.CvId, null, seed.JobId, CancellationToken.None));

        var results = await Task.WhenAll(first, second);
        results[0].Ledger.Id.Should().Be(results[1].Ledger.Id);
        (results[0].IsCaptureOwner ^ results[1].IsCaptureOwner).Should().BeTrue("Exactly one concurrent caller must win capture ownership.");
    }

    [Task6PostgresFact]
    [Trait("Requirement", "R-10")]
    public async Task CompleteAsync_TwoFinalizers_OnlyOneCanConsumeBilling()
    {
        var seed = await _fixture.SeedGraphAsync();
        Guid unlockId;
        await using (var context = _fixture.CreateContext())
        {
            var repo = new RecruiterCvUnlockRepository(context);
            var (ledger, _) = await repo.AcquirePendingAsync(
                seed.RecruiterUserId, seed.CvId, null, seed.JobId, CancellationToken.None);
            unlockId = ledger.Id;
        }

        var updateBarrier = new MatchingScanUpdateBarrier("recruiter_unlocked_cvs");
        await using var firstContext = _fixture.CreateContext(updateBarrier.CreateParticipant());
        await using var secondContext = _fixture.CreateContext(updateBarrier.CreateParticipant());
        var firstRepo = new RecruiterCvUnlockRepository(firstContext);
        var secondRepo = new RecruiterCvUnlockRepository(secondContext);

        var snapshot = new RetainedCvSnapshot($"retained-unlocks/{unlockId}", "cv.pdf", "HASH", DateTime.UtcNow);
        var first = Task.Run(() => firstRepo.CompleteAsync(unlockId, snapshot, "COINS", 50, DateTime.UtcNow, CancellationToken.None));
        var second = Task.Run(() => secondRepo.CompleteAsync(unlockId, snapshot, "COINS", 50, DateTime.UtcNow, CancellationToken.None));

        var results = await Task.WhenAll(first, second);
        (results[0] ^ results[1]).Should().BeTrue("Exactly one finalizer must successfully transition from Pending to Completed.");
    }

    [Task6PostgresFact]
    [Trait("Requirement", "R-10")]
    public async Task FailedCapture_RetryReusesLedgerIdentityWithoutPriorCharge()
    {
        var seed = await _fixture.SeedGraphAsync();
        Guid unlockId;
        await using (var context = _fixture.CreateContext())
        {
            var repo = new RecruiterCvUnlockRepository(context);
            var (ledger, _) = await repo.AcquirePendingAsync(
                seed.RecruiterUserId, seed.CvId, null, seed.JobId, CancellationToken.None);
            unlockId = ledger.Id;
            await repo.FailAsync(unlockId, "NETWORK_TIMEOUT", CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var repo = new RecruiterCvUnlockRepository(context);
            var (retryLedger, isOwner) = await repo.AcquirePendingAsync(
                seed.RecruiterUserId, seed.CvId, null, seed.JobId, CancellationToken.None);

            retryLedger.Id.Should().Be(unlockId);
            retryLedger.Status.Should().Be(RecruiterCvUnlockStatus.Pending);
            isOwner.Should().BeTrue();
        }
    }
}

