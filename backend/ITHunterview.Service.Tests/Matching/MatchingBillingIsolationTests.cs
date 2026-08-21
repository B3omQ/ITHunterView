using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.FeatureUsage;
using ITHunterview.Service.DTOs.Subscription;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.UseCase;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PayOS;
using Xunit;

namespace ITHunterview.Service.Tests.Matching;

public sealed class MatchingBillingIsolationTests
{
    private sealed class BillingTestContext : ITHunterviewContext
    {
        public BillingTestContext(DbContextOptions<ITHunterviewContext> options) : base(options) { }

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

    private static BillingTestContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ITHunterviewContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BillingTestContext(options);
    }

    private static (CandidateFeatureUsageUseCase UsageUseCase, WalletUseCase WalletUseCase, IFeatureUsageReservationRepository ReservationRepo) CreateServices(BillingTestContext context)
    {
        var reservationRepo = new FeatureUsageReservationRepository(context);
        var configRepoMock = new Mock<ISystemConfigRepository>();
        var configMock = new Mock<IConfiguration>();
        var hubContextMock = new Mock<IHubContext<ITHunterview.Service.Hubs.NotificationHub>>();
        var payOS = new PayOSClient("clientId", "apiKey", "checksumKey");

        var usageUseCase = new CandidateFeatureUsageUseCase(context, configRepoMock.Object, reservationRepo);
        var walletUseCase = new WalletUseCase(
            context,
            payOS,
            configMock.Object,
            hubContextMock.Object,
            NullLogger<WalletUseCase>.Instance);

        return (usageUseCase, walletUseCase, reservationRepo);
    }

    // =========================================================================
    // Task 15 Step 1: Truth-Table Tests for CvJdMatching Usage
    // =========================================================================

    [Theory]
    [InlineData("one_to_one_new", 1)]
    [InlineData("one_to_one_automatic_retry", 1)]
    [InlineData("one_to_one_duplicate_submission", 1)]
    [InlineData("one_to_one_manual_retry", 2)]
    [InlineData("candidate_hardcode_scan", 0)]
    [InlineData("recruiter_hardcode_scan", 0)]
    [InlineData("legacy_hardcode_row", 0)]
    [InlineData("legacy_vector_row", 0)]
    public async Task CvJdMatchingUsage_CountsOnlyOneToOneEntitlement(string scenario, int expected)
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var start = now.AddDays(-1);
        var end = now.AddDays(29);

        // Active subscription with quota limit of 5
        var sub = new Subscriptions
        {
            Id = 1,
            Name = "Pro Candidate",
            Price = 100000,
            Status = SubscriptionStatus.ACTIVE,
            FeaturesConfig = "{\"Role\":\"CANDIDATE\",\"CvMatchLimit\":5}"
        };
        var userSub = new UserSubscriptions
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SubId = 1,
            StartDate = start,
            EndDate = end,
            Status = UserSubscriptionStatus.ACTIVE
        };
        var wallet = new UserWallets
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Balance = 500,
            UpdatedAt = now
        };

        context.Subscriptions.Add(sub);
        context.UserSubscriptions.Add(userSub);
        context.UserWallets.Add(wallet);

        // Seed based on scenario
        switch (scenario)
        {
            case "one_to_one_new":
            {
                var matchId = Guid.NewGuid();
                context.FeatureUsageReservations.Add(new FeatureUsageReservations
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    FeatureKey = "CvJdMatching",
                    ReferenceId = matchId,
                    Source = "Subscription",
                    Status = "Captured",
                    CreatedAt = now
                });
                context.CvJobMatchScores.Add(new CvJobMatchScores
                {
                    Id = matchId,
                    UserId = userId,
                    Status = "Completed",
                    MatchType = "AI",
                    ProductScope = CvJobMatchProductScope.CandidateOneToOne,
                    BillingReservationId = Guid.NewGuid(),
                    UpdatedAt = now
                });
                break;
            }
            case "one_to_one_automatic_retry":
            {
                // Automatic retry reuses same reservation & same match row -> exactly 1 usage
                var matchId = Guid.NewGuid();
                context.FeatureUsageReservations.Add(new FeatureUsageReservations
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    FeatureKey = "CvJdMatching",
                    ReferenceId = matchId,
                    Source = "Subscription",
                    Status = "Captured",
                    CreatedAt = now
                });
                context.CvJobMatchScores.Add(new CvJobMatchScores
                {
                    Id = matchId,
                    UserId = userId,
                    Status = "Completed",
                    MatchType = "AI",
                    ProductScope = CvJobMatchProductScope.CandidateOneToOne,
                    BillingReservationId = Guid.NewGuid(),
                    AttemptCount = 1,
                    UpdatedAt = now
                });
                break;
            }
            case "one_to_one_duplicate_submission":
            {
                // Idempotent duplicate returns existing job with existing reservation -> exactly 1 usage
                var matchId = Guid.NewGuid();
                context.FeatureUsageReservations.Add(new FeatureUsageReservations
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    FeatureKey = "CvJdMatching",
                    ReferenceId = matchId,
                    Source = "Subscription",
                    Status = "Captured",
                    CreatedAt = now
                });
                context.CvJobMatchScores.Add(new CvJobMatchScores
                {
                    Id = matchId,
                    UserId = userId,
                    Status = "Completed",
                    MatchType = "AI",
                    ProductScope = CvJobMatchProductScope.CandidateOneToOne,
                    BillingReservationId = Guid.NewGuid(),
                    IdempotencyKey = "idemp-key-dup",
                    UpdatedAt = now
                });
                break;
            }
            case "one_to_one_manual_retry":
            {
                // Manual retry creates child row with second reservation -> 2 usages
                var parentId = Guid.NewGuid();
                var childId = Guid.NewGuid();
                var res1Id = Guid.NewGuid();
                var res2Id = Guid.NewGuid();

                context.FeatureUsageReservations.Add(new FeatureUsageReservations
                {
                    Id = res1Id,
                    UserId = userId,
                    FeatureKey = "CvJdMatching",
                    ReferenceId = parentId,
                    Source = "Subscription",
                    Status = "Captured",
                    CreatedAt = now.AddMinutes(-10)
                });
                context.CvJobMatchScores.Add(new CvJobMatchScores
                {
                    Id = parentId,
                    UserId = userId,
                    Status = "Failed",
                    ErrorCode = "AI_PROVIDER_TIMEOUT",
                    MatchType = "AI",
                    ProductScope = CvJobMatchProductScope.CandidateOneToOne,
                    BillingReservationId = res1Id,
                    UpdatedAt = now.AddMinutes(-10)
                });

                context.FeatureUsageReservations.Add(new FeatureUsageReservations
                {
                    Id = res2Id,
                    UserId = userId,
                    FeatureKey = "CvJdMatching",
                    ReferenceId = childId,
                    Source = "Subscription",
                    Status = "Captured",
                    CreatedAt = now
                });
                context.CvJobMatchScores.Add(new CvJobMatchScores
                {
                    Id = childId,
                    UserId = userId,
                    RetryOfJobId = parentId,
                    Status = "Completed",
                    MatchType = "AI",
                    ProductScope = CvJobMatchProductScope.CandidateOneToOne,
                    BillingReservationId = res2Id,
                    UpdatedAt = now
                });
                break;
            }
            case "candidate_hardcode_scan":
            {
                var run = new CandidateJobScanRun
                {
                    Id = Guid.NewGuid(),
                    CandidateUserId = userId,
                    CvId = Guid.NewGuid(),
                    Status = MatchingScanRunStatus.Completed,
                    CreatedAt = now,
                    CompletedAt = now
                };
                context.CandidateJobScanRuns.Add(run);
                context.CandidateJobScanResults.Add(new CandidateJobScanResult
                {
                    Id = Guid.NewGuid(),
                    RunId = run.Id,
                    JobId = Guid.NewGuid(),
                    Rank = 1,
                    MatchScore = 85
                });
                break;
            }
            case "recruiter_hardcode_scan":
            {
                var run = new RecruiterCvScanRun
                {
                    Id = Guid.NewGuid(),
                    RecruiterUserId = Guid.NewGuid(),
                    JobId = Guid.NewGuid(),
                    Status = MatchingScanRunStatus.Completed,
                    CreatedAt = now,
                    CompletedAt = now
                };
                context.RecruiterCvScanRuns.Add(run);
                context.RecruiterCvScanResults.Add(new RecruiterCvScanResult
                {
                    Id = Guid.NewGuid(),
                    RunId = run.Id,
                    CvId = Guid.NewGuid(),
                    CandidateUserId = userId,
                    Rank = 1,
                    MatchScore = 90
                });
                break;
            }
            case "legacy_hardcode_row":
            {
                // Unscoped legacy hardcode row in CvJobMatchScores -> counts 0
                context.CvJobMatchScores.Add(new CvJobMatchScores
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Status = "Completed",
                    MatchType = "Hardcode",
                    ProductScope = null,
                    BillingReservationId = null,
                    UpdatedAt = now
                });
                break;
            }
            case "legacy_vector_row":
            {
                // Unscoped legacy vector row in CvJobMatchScores -> counts 0
                context.CvJobMatchScores.Add(new CvJobMatchScores
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Status = "Completed",
                    MatchType = "Vector",
                    ProductScope = null,
                    BillingReservationId = null,
                    UpdatedAt = now
                });
                break;
            }
        }

        await context.SaveChangesAsync();

        var (usageUseCase, walletUseCase, _) = CreateServices(context);

        // 1. Check Wallet balance response
        var balanceResponse = await walletUseCase.GetWalletBalanceAsync(userId);
        balanceResponse.Data.Should().NotBeNull();
        (balanceResponse.Data!.CvMatchUsed ?? 0).Should().Be(expected);

        // 2. Check ReserveFeatureAsync admission behavior
        var nextRefId = Guid.NewGuid();
        var reservation = await usageUseCase.ReserveFeatureAsync(userId, "CvJdMatching", nextRefId);

        // Limit is 5. If expected < 5, source must be Subscription.
        if (expected < 5)
        {
            reservation.Source.Should().Be("Subscription");
            reservation.CoinAmount.Should().Be(0);
        }
    }

    [Fact]
    public async Task ReserveFeatureAsync_NullScopeAiLegacyRow_DoesNotCountTowardQuota()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var start = now.AddDays(-1);
        var end = now.AddDays(29);

        var sub = new Subscriptions
        {
            Id = 1,
            Name = "Pro Candidate",
            Price = 100000,
            Status = SubscriptionStatus.ACTIVE,
            FeaturesConfig = "{\"Role\":\"CANDIDATE\",\"CvMatchLimit\":1}"
        };
        var userSub = new UserSubscriptions
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SubId = 1,
            StartDate = start,
            EndDate = end,
            Status = UserSubscriptionStatus.ACTIVE
        };
        var wallet = new UserWallets
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Balance = 500,
            UpdatedAt = now
        };

        // Null-scope AI row without reservation (legacy row)
        context.CvJobMatchScores.Add(new CvJobMatchScores
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = "Completed",
            MatchType = "AI",
            ProductScope = null,
            BillingReservationId = null,
            UpdatedAt = now
        });

        context.Subscriptions.Add(sub);
        context.UserSubscriptions.Add(userSub);
        context.UserWallets.Add(wallet);
        await context.SaveChangesAsync();

        var (usageUseCase, walletUseCase, _) = CreateServices(context);

        // Legacy null-scope row counts 0 towards current quota
        var balance = await walletUseCase.GetWalletBalanceAsync(userId);
        (balance.Data!.CvMatchUsed ?? 0).Should().Be(0);

        // Candidate with limit=1 should still be allowed to reserve using Subscription
        var reservation = await usageUseCase.ReserveFeatureAsync(userId, "CvJdMatching", Guid.NewGuid());
        reservation.Source.Should().Be("Subscription");
    }

    [Fact]
    public async Task ReserveFeatureAsync_ExplicitCandidateOneToOneScope_CountsTowardQuota()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var start = now.AddDays(-1);
        var end = now.AddDays(29);

        var sub = new Subscriptions
        {
            Id = 1,
            Name = "Pro Candidate",
            Price = 100000,
            Status = SubscriptionStatus.ACTIVE,
            FeaturesConfig = "{\"Role\":\"CANDIDATE\",\"CvMatchLimit\":1}"
        };
        var userSub = new UserSubscriptions
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SubId = 1,
            StartDate = start,
            EndDate = end,
            Status = UserSubscriptionStatus.ACTIVE
        };
        var wallet = new UserWallets
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Balance = 2000, // Enough for 1000 coin fallback
            UpdatedAt = now
        };

        // Legacy compatibility row with explicit CandidateOneToOne scope
        context.CvJobMatchScores.Add(new CvJobMatchScores
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = "Completed",
            MatchType = "AI",
            ProductScope = CvJobMatchProductScope.CandidateOneToOne,
            BillingReservationId = null,
            UpdatedAt = now
        });

        context.Subscriptions.Add(sub);
        context.UserSubscriptions.Add(userSub);
        context.UserWallets.Add(wallet);
        await context.SaveChangesAsync();

        var (usageUseCase, walletUseCase, _) = CreateServices(context);

        // Should count 1
        var balance = await walletUseCase.GetWalletBalanceAsync(userId);
        (balance.Data!.CvMatchUsed ?? 0).Should().Be(1);

        // Limit was 1, so next reservation falls back to Coin
        var reservation = await usageUseCase.ReserveFeatureAsync(userId, "CvJdMatching", Guid.NewGuid());
        reservation.Source.Should().Be("Coin");
        reservation.CoinAmount.Should().Be(1000);
    }
}
