using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.UseCase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ITHunterview.Service.Tests.UseCase;

public sealed class RecruiterCvUnlockUseCaseTests
{
    private static ITHunterviewContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ITHunterviewContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new UnlockTestContext(options);
    }

    private sealed class UnlockTestContext : ITHunterviewContext
    {
        private static readonly HashSet<Type> AllowedTypes =
        [
            typeof(User),
            typeof(CandidateProfiles),
            typeof(Cvs),
            typeof(RecruiterCvScanRun),
            typeof(RecruiterCvScanResult),
            typeof(RecruiterUnlockedCvs),
            typeof(Subscriptions),
            typeof(UserSubscriptions),
            typeof(UserWallets),
            typeof(CreditTransactions)
        ];

        public UnlockTestContext(DbContextOptions<ITHunterviewContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                         .Where(type => !AllowedTypes.Contains(type.ClrType))
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
        }
    }

    private static (RecruiterCvUnlockUseCase Sut, ITHunterviewContext Context, Mock<IRecruiterUnlockedCvSnapshotStore> SnapshotStore, IRecruiterCvUnlockRepository UnlockRepo) CreateSut(
        ITHunterviewContext? context = null,
        Mock<IRecruiterUnlockedCvSnapshotStore>? snapshotStoreMock = null)
    {
        var ctx = context ?? CreateContext();
        var store = snapshotStoreMock ?? new Mock<IRecruiterUnlockedCvSnapshotStore>();
        if (snapshotStoreMock == null)
        {
            store.Setup(s => s.CaptureAsync(It.IsAny<Guid>(), It.IsAny<Cvs>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid unlockId, Cvs cv, CancellationToken ct) =>
                    new RetainedCvSnapshot($"retained-unlocks/{unlockId}", cv.FileName ?? "cv.pdf", "DUMMYHASH", DateTime.UtcNow));
            store.Setup(s => s.CreateAuthorizedReadUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string key, CancellationToken ct) => $"https://signed.storage/{key}");
        }

        var unlockRepo = new RecruiterCvUnlockRepository(ctx);
        var sut = new RecruiterCvUnlockUseCase(ctx, unlockRepo, store.Object, NullLogger<RecruiterCvUnlockUseCase>.Instance);
        return (sut, ctx, store, unlockRepo);
    }

    private static async Task<(Guid RecruiterUserId, Guid CandidateUserId, Guid CvId, Guid RunId, Guid ScanResultId)> SeedMatchingScanGraphAsync(
        ITHunterviewContext context,
        bool isCvVisible = true,
        bool isCvPrimary = true,
        bool isCvDeleted = false,
        Guid? existingRecruiterId = null,
        Guid? existingRunId = null)
    {
        var recruiterId = existingRecruiterId ?? Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var cvId = Guid.NewGuid();
        var runId = existingRunId ?? Guid.NewGuid();
        var scanResultId = Guid.NewGuid();
        var jobId = Guid.NewGuid();

        var candidateUser = new User
        {
            Id = candidateId,
            Email = "candidate@test.com",
            Status = UserStatus.ACTIVE
        };
        var candidateProfile = new CandidateProfiles
        {
            Id = Guid.NewGuid(),
            UserId = candidateId,
            FirstName = "John",
            LastName = "Doe",
            Phone = "+84900000001",
            IsVisibleToRecruiters = isCvVisible
        };
        var cv = new Cvs
        {
            Id = cvId,
            UserId = candidateId,
            FileName = "johndoe_cv.pdf",
            FileUrl = "https://public.storage/cvs/johndoe_cv.pdf",
            FileType = "application/pdf",
            ParsedData = "{}",
            IsPrimary = isCvPrimary,
            DeletedAt = isCvDeleted ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow
        };
        if (existingRunId == null)
        {
            var run = new RecruiterCvScanRun
            {
                Id = runId,
                RecruiterUserId = recruiterId,
                CompanyId = Guid.NewGuid(),
                JobId = jobId,
                Status = MatchingScanRunStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };
            await context.RecruiterCvScanRuns.AddAsync(run);
        }

        var result = new RecruiterCvScanResult
        {
            Id = scanResultId,
            RunId = runId,
            CvId = cvId,
            CandidateUserId = candidateId,
            MatchScore = 85,
            MatchDetails = "{}",
            Rank = 1
        };

        await context.Users.AddAsync(candidateUser);
        await context.CandidateProfiles.AddAsync(candidateProfile);
        await context.Cvs.AddAsync(cv);
        await context.RecruiterCvScanResults.AddAsync(result);
        await context.SaveChangesAsync();

        return (recruiterId, candidateId, cvId, runId, scanResultId);
    }

    [Fact]
    [Trait("Requirement", "R-09")]
    [Trait("Requirement", "R-10")]
    public async Task UnlockAsync_OwnLockedScanResult_ConsumesExactlyOneCurrentEntitlement()
    {
        var (sut, ctx, _, _) = CreateSut();
        var (recruiterId, _, _, _, scanResultId) = await SeedMatchingScanGraphAsync(ctx);

        var wallet = new UserWallets
        {
            Id = Guid.NewGuid(),
            UserId = recruiterId,
            Balance = 100,
            UpdatedAt = DateTime.UtcNow
        };
        await ctx.UserWallets.AddAsync(wallet);
        await ctx.SaveChangesAsync();

        var response = await sut.UnlockAsync(recruiterId, scanResultId);

        using (new AssertionScope())
        {
            response.Should().NotBeNull();
            response.CoinsSpent.Should().Be(50);
            response.UnlockedVia.Should().Be("COINS");
            response.IsRetainedCopy.Should().BeTrue();
            response.FileUrl.Should().Contain("retained-unlocks");

            var updatedWallet = await ctx.UserWallets.AsNoTracking().SingleAsync(w => w.UserId == recruiterId);
            updatedWallet.Balance.Should().Be(50);

            var tx = await ctx.CreditTransactions.AsNoTracking().SingleAsync(t => t.WalletId == wallet.Id);
            tx.Amount.Should().Be(-50);
        }
    }

    [Fact]
    [Trait("Requirement", "R-10")]
    public async Task UnlockAsync_AlreadyCompleted_ReturnsRetainedSnapshotWithoutCharge()
    {
        var (sut, ctx, snapshotStore, _) = CreateSut();
        var (recruiterId, _, cvId, _, scanResultId) = await SeedMatchingScanGraphAsync(ctx);

        var unlockId = Guid.NewGuid();
        var existingUnlock = new RecruiterUnlockedCvs
        {
            Id = unlockId,
            RecruiterId = recruiterId,
            CvId = cvId,
            SourceScanResultId = scanResultId,
            Status = RecruiterCvUnlockStatus.Completed,
            SnapshotStorageKey = $"retained-unlocks/{unlockId}",
            SnapshotFileName = "johndoe_cv.pdf",
            SnapshotContentHash = "HASH123",
            SnapshotCreatedAt = DateTime.UtcNow.AddDays(-1),
            UnlockedVia = "COINS",
            CoinsSpent = 50,
            UnlockedAt = DateTime.UtcNow.AddDays(-1)
        };
        var wallet = new UserWallets { Id = Guid.NewGuid(), UserId = recruiterId, Balance = 100 };
        await ctx.RecruiterUnlockedCvs.AddAsync(existingUnlock);
        await ctx.UserWallets.AddAsync(wallet);
        await ctx.SaveChangesAsync();

        var response = await sut.UnlockAsync(recruiterId, scanResultId);

        using (new AssertionScope())
        {
            response.UnlockId.Should().Be(unlockId);
            response.CoinsSpent.Should().Be(50);
            response.IsRetainedCopy.Should().BeTrue();
            snapshotStore.Verify(s => s.CaptureAsync(It.IsAny<Guid>(), It.IsAny<Cvs>(), It.IsAny<CancellationToken>()), Times.Never);

            var updatedWallet = await ctx.UserWallets.AsNoTracking().SingleAsync(w => w.UserId == recruiterId);
            updatedWallet.Balance.Should().Be(100);
        }
    }

    [Fact]
    [Trait("Requirement", "R-10")]
    public async Task UnlockAsync_LegacyCompletedLedger_ReturnsAuthorizedLiveCompatibilityWithoutRecharge()
    {
        var (sut, ctx, _, _) = CreateSut();
        var (recruiterId, _, cvId, _, scanResultId) = await SeedMatchingScanGraphAsync(ctx);

        var unlockId = Guid.NewGuid();
        var legacyUnlock = new RecruiterUnlockedCvs
        {
            Id = unlockId,
            RecruiterId = recruiterId,
            CvId = cvId,
            Status = RecruiterCvUnlockStatus.Completed,
            SnapshotStorageKey = null, // legacy unlock
            SnapshotFileName = null,
            UnlockedVia = "SUBSCRIPTION",
            CoinsSpent = 0,
            UnlockedAt = DateTime.UtcNow.AddDays(-2)
        };
        await ctx.RecruiterUnlockedCvs.AddAsync(legacyUnlock);
        await ctx.SaveChangesAsync();

        var response = await sut.UnlockAsync(recruiterId, scanResultId);

        using (new AssertionScope())
        {
            response.UnlockId.Should().Be(unlockId);
            response.IsRetainedCopy.Should().BeFalse();
            response.FileUrl.Should().Be("https://public.storage/cvs/johndoe_cv.pdf");
        }
    }

    [Fact]
    [Trait("Requirement", "R-09")]
    public async Task UnlockAsync_OtherRecruiterResult_RejectsWithoutIdentityOrCharge()
    {
        var (sut, ctx, _, _) = CreateSut();
        var (_, _, _, _, scanResultId) = await SeedMatchingScanGraphAsync(ctx);
        var otherRecruiterId = Guid.NewGuid();

        var action = () => sut.UnlockAsync(otherRecruiterId, scanResultId);

        await action.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    [Trait("Requirement", "R-09")]
    public async Task UnlockAsync_ResultFromRunCallerDoesNotOwn_RejectsWithoutCharge()
    {
        var (sut, ctx, _, _) = CreateSut();
        var (recruiterId, _, _, _, _) = await SeedMatchingScanGraphAsync(ctx);
        var unownedResultId = Guid.NewGuid();

        var action = () => sut.UnlockAsync(recruiterId, unownedResultId);

        await action.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    [Trait("Requirement", "R-09")]
    public async Task UnlockAsync_RawCvIdWithoutOwnedScanResult_IsNotAuthority()
    {
        var (sut, ctx, _, _) = CreateSut();
        var (recruiterId, _, cvId, _, _) = await SeedMatchingScanGraphAsync(ctx);

        var action = () => sut.UnlockAsync(recruiterId, cvId); // Passing cvId instead of scanResultId

        await action.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    [Trait("Requirement", "R-08")]
    [Trait("Requirement", "R-10")]
    public async Task UnlockAsync_VisibilityTurnedOffAfterScan_Succeeds()
    {
        var (sut, ctx, _, _) = CreateSut();
        var (recruiterId, candidateId, _, _, scanResultId) = await SeedMatchingScanGraphAsync(ctx);

        // Turn off visibility after scan
        var profile = await ctx.CandidateProfiles.SingleAsync(p => p.UserId == candidateId);
        profile.IsVisibleToRecruiters = false;
        var wallet = new UserWallets { Id = Guid.NewGuid(), UserId = recruiterId, Balance = 100 };
        await ctx.UserWallets.AddAsync(wallet);
        await ctx.SaveChangesAsync();

        var response = await sut.UnlockAsync(recruiterId, scanResultId);

        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
    }

    [Fact]
    [Trait("Requirement", "R-08")]
    [Trait("Requirement", "R-10")]
    public async Task UnlockAsync_PrimaryChangedAfterScan_Succeeds()
    {
        var (sut, ctx, _, _) = CreateSut();
        var (recruiterId, _, cvId, _, scanResultId) = await SeedMatchingScanGraphAsync(ctx);

        // Remove primary status after scan
        var cv = await ctx.Cvs.SingleAsync(c => c.Id == cvId);
        cv.IsPrimary = false;
        var wallet = new UserWallets { Id = Guid.NewGuid(), UserId = recruiterId, Balance = 100 };
        await ctx.UserWallets.AddAsync(wallet);
        await ctx.SaveChangesAsync();

        var response = await sut.UnlockAsync(recruiterId, scanResultId);

        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
    }

    [Fact]
    [Trait("Requirement", "R-10")]
    public async Task UnlockAsync_CvDeletedBeforeFirstUnlock_FailsWithoutCaptureOrCharge()
    {
        var (sut, ctx, snapshotStore, _) = CreateSut();
        var (recruiterId, _, cvId, _, scanResultId) = await SeedMatchingScanGraphAsync(ctx, isCvDeleted: true);

        var wallet = new UserWallets { Id = Guid.NewGuid(), UserId = recruiterId, Balance = 100 };
        await ctx.UserWallets.AddAsync(wallet);
        await ctx.SaveChangesAsync();

        var action = () => sut.UnlockAsync(recruiterId, scanResultId);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*CV_NOT_FOUND_OR_DELETED*");

        snapshotStore.Verify(s => s.CaptureAsync(It.IsAny<Guid>(), It.IsAny<Cvs>(), It.IsAny<CancellationToken>()), Times.Never);
        var updatedWallet = await ctx.UserWallets.AsNoTracking().SingleAsync(w => w.UserId == recruiterId);
        updatedWallet.Balance.Should().Be(100);
    }

    [Fact]
    [Trait("Requirement", "R-10")]
    public async Task UnlockAsync_CvDeletedAfterCompleted_ReturnsRetainedCopy()
    {
        var (sut, ctx, _, _) = CreateSut();
        var (recruiterId, _, cvId, _, scanResultId) = await SeedMatchingScanGraphAsync(ctx);

        var unlockId = Guid.NewGuid();
        var completedUnlock = new RecruiterUnlockedCvs
        {
            Id = unlockId,
            RecruiterId = recruiterId,
            CvId = cvId,
            SourceScanResultId = scanResultId,
            Status = RecruiterCvUnlockStatus.Completed,
            SnapshotStorageKey = $"retained-unlocks/{unlockId}",
            SnapshotFileName = "deleted_cv.pdf",
            SnapshotCreatedAt = DateTime.UtcNow.AddDays(-1),
            UnlockedVia = "COINS",
            CoinsSpent = 50,
            UnlockedAt = DateTime.UtcNow.AddDays(-1)
        };
        await ctx.RecruiterUnlockedCvs.AddAsync(completedUnlock);

        // Delete CV after completed unlock
        var cv = await ctx.Cvs.SingleAsync(c => c.Id == cvId);
        cv.DeletedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();

        var response = await sut.UnlockAsync(recruiterId, scanResultId);

        using (new AssertionScope())
        {
            response.UnlockId.Should().Be(unlockId);
            response.IsRetainedCopy.Should().BeTrue();
            response.FileUrl.Should().Contain("retained-unlocks");
        }
    }

    [Fact]
    [Trait("Requirement", "R-10")]
    public async Task UnlockAsync_DeleteRacingFirstUnlock_HasOneLinearizedChargedOrUnchargedOutcome()
    {
        var (sut, ctx, _, _) = CreateSut();
        var (recruiterId, _, cvId, _, scanResultId) = await SeedMatchingScanGraphAsync(ctx);

        var wallet = new UserWallets { Id = Guid.NewGuid(), UserId = recruiterId, Balance = 100 };
        await ctx.UserWallets.AddAsync(wallet);
        await ctx.SaveChangesAsync();

        // Simulate racing delete before unlock execution starts
        var cv = await ctx.Cvs.SingleAsync(c => c.Id == cvId);
        cv.DeletedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();

        var action = () => sut.UnlockAsync(recruiterId, scanResultId);
        await action.Should().ThrowAsync<InvalidOperationException>();

        var updatedWallet = await ctx.UserWallets.AsNoTracking().SingleAsync(w => w.UserId == recruiterId);
        updatedWallet.Balance.Should().Be(100);
    }

    [Fact]
    [Trait("Requirement", "R-10")]
    [Trait("Requirement", "R-11")]
    public async Task UnlockAsync_CaptureFailure_LeavesWalletQuotaAndLedgerUnconsumed()
    {
        var storeMock = new Mock<IRecruiterUnlockedCvSnapshotStore>();
        storeMock.Setup(s => s.CaptureAsync(It.IsAny<Guid>(), It.IsAny<Cvs>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("RETAINED_CV_CAPTURE_FAILED: Cloudinary unavailable"));

        var (sut, ctx, _, _) = CreateSut(snapshotStoreMock: storeMock);
        var (recruiterId, _, cvId, _, scanResultId) = await SeedMatchingScanGraphAsync(ctx);

        var wallet = new UserWallets { Id = Guid.NewGuid(), UserId = recruiterId, Balance = 100 };
        await ctx.UserWallets.AddAsync(wallet);
        await ctx.SaveChangesAsync();

        var action = () => sut.UnlockAsync(recruiterId, scanResultId);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*RETAINED_CV_CAPTURE_FAILED*");

        var updatedWallet = await ctx.UserWallets.AsNoTracking().SingleAsync(w => w.UserId == recruiterId);
        updatedWallet.Balance.Should().Be(100);

        var ledger = await ctx.RecruiterUnlockedCvs.AsNoTracking().SingleAsync(u => u.RecruiterId == recruiterId && u.CvId == cvId);
        ledger.Status.Should().Be(RecruiterCvUnlockStatus.Failed);
    }

    [Fact]
    [Trait("Requirement", "R-10")]
    public async Task UnlockAsync_NoQuotaAndInsufficientCoins_FailsWithoutCompletedLedgerOrDebit()
    {
        var (sut, ctx, _, _) = CreateSut();
        var (recruiterId, _, cvId, _, scanResultId) = await SeedMatchingScanGraphAsync(ctx);

        var wallet = new UserWallets { Id = Guid.NewGuid(), UserId = recruiterId, Balance = 20 }; // Needs 50
        await ctx.UserWallets.AddAsync(wallet);
        await ctx.SaveChangesAsync();

        var action = () => sut.UnlockAsync(recruiterId, scanResultId);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*INSUFFICIENT_FUNDS_OR_QUOTA*");

        var updatedWallet = await ctx.UserWallets.AsNoTracking().SingleAsync(w => w.UserId == recruiterId);
        updatedWallet.Balance.Should().Be(20);

        var ledger = await ctx.RecruiterUnlockedCvs.AsNoTracking().SingleAsync(u => u.RecruiterId == recruiterId && u.CvId == cvId);
        ledger.Status.Should().Be(RecruiterCvUnlockStatus.Failed);
    }

    [Fact]
    [Trait("Requirement", "R-10")]
    public async Task UnlockAsync_TwoConcurrentRequests_OneLedgerOneChargeBothIdempotentSuccess()
    {
        var (sut, ctx, _, _) = CreateSut();
        var (recruiterId, _, cvId, _, scanResultId) = await SeedMatchingScanGraphAsync(ctx);

        var wallet = new UserWallets { Id = Guid.NewGuid(), UserId = recruiterId, Balance = 100 };
        await ctx.UserWallets.AddAsync(wallet);
        await ctx.SaveChangesAsync();

        var task1 = sut.UnlockAsync(recruiterId, scanResultId);
        var task2 = sut.UnlockAsync(recruiterId, scanResultId);

        var results = await Task.WhenAll(task1, task2);

        using (new AssertionScope())
        {
            results[0].UnlockId.Should().Be(results[1].UnlockId);
            (await ctx.RecruiterUnlockedCvs.CountAsync(u => u.RecruiterId == recruiterId && u.CvId == cvId)).Should().Be(1);

            var updatedWallet = await ctx.UserWallets.AsNoTracking().SingleAsync(w => w.UserId == recruiterId);
            updatedWallet.Balance.Should().Be(50);
        }
    }

    [Fact]
    [Trait("Requirement", "R-10")]
    public async Task UnlockAsync_TwoDifferentCvsCompeteForLastQuota_NeverOverConsumesQuota()
    {
        var (sut, ctx, _, _) = CreateSut();
        var (recruiterId, _, _, runId, scanResult1) = await SeedMatchingScanGraphAsync(ctx);
        var (_, _, _, _, scanResult2) = await SeedMatchingScanGraphAsync(ctx, existingRecruiterId: recruiterId, existingRunId: runId);

        // Assign subscription with unlockCvLimit = 1
        const int subId = 1;
        var sub = new Subscriptions
        {
            Id = subId,
            Name = "Starter Recruiter",
            FeaturesConfig = "{\"unlockCvLimit\":1}"
        };
        var userSub = new UserSubscriptions
        {
            Id = Guid.NewGuid(),
            UserId = recruiterId,
            SubId = subId,
            Status = UserSubscriptionStatus.ACTIVE,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(30)
        };
        var wallet = new UserWallets { Id = Guid.NewGuid(), UserId = recruiterId, Balance = 50 };
        await ctx.Subscriptions.AddAsync(sub);
        await ctx.UserSubscriptions.AddAsync(userSub);
        await ctx.UserWallets.AddAsync(wallet);
        await ctx.SaveChangesAsync();

        var res1 = await sut.UnlockAsync(recruiterId, scanResult1);
        var res2 = await sut.UnlockAsync(recruiterId, scanResult2);

        using (new AssertionScope())
        {
            res1.UnlockedVia.Should().Be("SUBSCRIPTION");
            res1.CoinsSpent.Should().Be(0);

            res2.UnlockedVia.Should().Be("COINS");
            res2.CoinsSpent.Should().Be(50);

            var updatedWallet = await ctx.UserWallets.AsNoTracking().SingleAsync(w => w.UserId == recruiterId);
            updatedWallet.Balance.Should().Be(0);
        }
    }

    [Fact]
    [Trait("Requirement", "R-10")]
    public async Task UnlockAsync_TwoDifferentCvsCompeteForCoins_NeverOverdrawsWallet()
    {
        var (sut, ctx, _, _) = CreateSut();
        var (recruiterId, _, _, runId, scanResult1) = await SeedMatchingScanGraphAsync(ctx);
        var (_, _, _, _, scanResult2) = await SeedMatchingScanGraphAsync(ctx, existingRecruiterId: recruiterId, existingRunId: runId);

        // Wallet has only 50 coins (enough for 1 CV)
        var wallet = new UserWallets { Id = Guid.NewGuid(), UserId = recruiterId, Balance = 50 };
        await ctx.UserWallets.AddAsync(wallet);
        await ctx.SaveChangesAsync();

        var res1 = await sut.UnlockAsync(recruiterId, scanResult1);
        var action2 = () => sut.UnlockAsync(recruiterId, scanResult2);

        res1.CoinsSpent.Should().Be(50);
        await action2.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*INSUFFICIENT_FUNDS_OR_QUOTA*");

        var updatedWallet = await ctx.UserWallets.AsNoTracking().SingleAsync(w => w.UserId == recruiterId);
        updatedWallet.Balance.Should().Be(0);
    }

    [Fact]
    [Trait("Requirement", "R-10")]
    public async Task UnlockAsync_SameRecruiterSameCvFromLaterOwnedJobOrRun_IsFree()
    {
        var (sut, ctx, _, _) = CreateSut();
        var (recruiterId, candidateId, cvId, _, scanResult1) = await SeedMatchingScanGraphAsync(ctx);

        // Create second run and second scan result with same CV
        var run2 = new RecruiterCvScanRun
        {
            Id = Guid.NewGuid(),
            RecruiterUserId = recruiterId,
            CompanyId = Guid.NewGuid(),
            JobId = Guid.NewGuid(),
            Status = MatchingScanRunStatus.Completed,
            CreatedAt = DateTime.UtcNow
        };
        var scanResult2 = new RecruiterCvScanResult
        {
            Id = Guid.NewGuid(),
            RunId = run2.Id,
            CvId = cvId,
            CandidateUserId = candidateId,
            MatchScore = 90,
            MatchDetails = "{}",
            Rank = 1
        };
        var wallet = new UserWallets { Id = Guid.NewGuid(), UserId = recruiterId, Balance = 100 };
        await ctx.RecruiterCvScanRuns.AddAsync(run2);
        await ctx.RecruiterCvScanResults.AddAsync(scanResult2);
        await ctx.UserWallets.AddAsync(wallet);
        await ctx.SaveChangesAsync();

        var res1 = await sut.UnlockAsync(recruiterId, scanResult1);
        var res2 = await sut.UnlockAsync(recruiterId, scanResult2.Id);

        using (new AssertionScope())
        {
            res1.CoinsSpent.Should().Be(50);
            res2.CoinsSpent.Should().Be(50); // Same original unlock record returned
            res1.UnlockId.Should().Be(res2.UnlockId);

            var updatedWallet = await ctx.UserWallets.AsNoTracking().SingleAsync(w => w.UserId == recruiterId);
            updatedWallet.Balance.Should().Be(50); // Charged only once!
        }
    }

    [Fact]
    [Trait("Requirement", "R-10")]
    public async Task UnlockAsync_DifferentRecruiterSameCv_RequiresIndependentUnlock()
    {
        var (sut, ctx, _, _) = CreateSut();
        var (recruiter1, candidateId, cvId, _, scanResult1) = await SeedMatchingScanGraphAsync(ctx);
        var recruiter2 = Guid.NewGuid();

        var run2 = new RecruiterCvScanRun
        {
            Id = Guid.NewGuid(),
            RecruiterUserId = recruiter2,
            CompanyId = Guid.NewGuid(),
            JobId = Guid.NewGuid(),
            Status = MatchingScanRunStatus.Completed,
            CreatedAt = DateTime.UtcNow
        };
        var scanResult2 = new RecruiterCvScanResult
        {
            Id = Guid.NewGuid(),
            RunId = run2.Id,
            CvId = cvId,
            CandidateUserId = candidateId,
            MatchScore = 90,
            MatchDetails = "{}",
            Rank = 1
        };
        var wallet1 = new UserWallets { Id = Guid.NewGuid(), UserId = recruiter1, Balance = 100 };
        var wallet2 = new UserWallets { Id = Guid.NewGuid(), UserId = recruiter2, Balance = 100 };
        await ctx.RecruiterCvScanRuns.AddAsync(run2);
        await ctx.RecruiterCvScanResults.AddAsync(scanResult2);
        await ctx.UserWallets.AddRangeAsync(wallet1, wallet2);
        await ctx.SaveChangesAsync();

        var res1 = await sut.UnlockAsync(recruiter1, scanResult1);
        var res2 = await sut.UnlockAsync(recruiter2, scanResult2.Id);

        using (new AssertionScope())
        {
            res1.UnlockId.Should().NotBe(res2.UnlockId);
            (await ctx.UserWallets.AsNoTracking().SingleAsync(w => w.UserId == recruiter1)).Balance.Should().Be(50);
            (await ctx.UserWallets.AsNoTracking().SingleAsync(w => w.UserId == recruiter2)).Balance.Should().Be(50);
        }
    }

    [Fact]
    [Trait("Requirement", "R-10")]
    public async Task UnlockAsync_SameRecruiterReplacementCv_RequiresIndependentUnlock()
    {
        var (sut, ctx, _, _) = CreateSut();
        var (recruiterId, candidateId, cv1, _, scanResult1) = await SeedMatchingScanGraphAsync(ctx);

        var cv2 = new Cvs
        {
            Id = Guid.NewGuid(),
            UserId = candidateId,
            FileName = "johndoe_updated_cv.pdf",
            FileUrl = "https://public.storage/cvs/johndoe_updated_cv.pdf",
            FileType = "application/pdf",
            ParsedData = "{}",
            IsPrimary = true,
            CreatedAt = DateTime.UtcNow
        };
        var run2 = new RecruiterCvScanRun
        {
            Id = Guid.NewGuid(),
            RecruiterUserId = recruiterId,
            CompanyId = Guid.NewGuid(),
            JobId = Guid.NewGuid(),
            Status = MatchingScanRunStatus.Completed,
            CreatedAt = DateTime.UtcNow
        };
        var scanResult2 = new RecruiterCvScanResult
        {
            Id = Guid.NewGuid(),
            RunId = run2.Id,
            CvId = cv2.Id,
            CandidateUserId = candidateId,
            MatchScore = 95,
            MatchDetails = "{}",
            Rank = 1
        };
        var wallet = new UserWallets { Id = Guid.NewGuid(), UserId = recruiterId, Balance = 100 };
        await ctx.Cvs.AddAsync(cv2);
        await ctx.RecruiterCvScanRuns.AddAsync(run2);
        await ctx.RecruiterCvScanResults.AddAsync(scanResult2);
        await ctx.UserWallets.AddAsync(wallet);
        await ctx.SaveChangesAsync();

        var res1 = await sut.UnlockAsync(recruiterId, scanResult1);
        var res2 = await sut.UnlockAsync(recruiterId, scanResult2.Id);

        using (new AssertionScope())
        {
            res1.UnlockId.Should().NotBe(res2.UnlockId);
            res1.CvId.Should().Be(cv1);
            res2.CvId.Should().Be(cv2.Id);
            (await ctx.UserWallets.AsNoTracking().SingleAsync(w => w.UserId == recruiterId)).Balance.Should().Be(0);
        }
    }
}
