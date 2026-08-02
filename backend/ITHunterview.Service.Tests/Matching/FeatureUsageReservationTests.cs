using System.Text.Json;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.UseCase;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ITHunterview.Service.Tests.Matching;

public sealed class FeatureUsageReservationTests
{
    [Fact]
    public async Task CoinReservation_IsIdempotentCaptureAndRefundAreExactlyOnce()
    {
        var userId = Guid.NewGuid();
        var referenceId = Guid.NewGuid();
        await using var context = CreateContext();
        context.UserWallets.Add(new UserWallets
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Balance = 1_500,
            UpdatedAt = DateTime.UtcNow
        });
        context.CoinFeatures.Add(new CoinFeatures
        {
            FeatureKey = "CvJdMatching",
            CoinCost = 1_000,
            Description = "matching"
        });
        await context.SaveChangesAsync();
        var useCase = CreateUseCase(context);

        var first = await useCase.ReserveFeatureAsync(userId, "CvJdMatching", referenceId);
        var duplicate = await useCase.ReserveFeatureAsync(userId, "CvJdMatching", referenceId);

        duplicate.ReservationId.Should().Be(first.ReservationId);
        duplicate.Status.Should().Be("Reserved");
        context.FeatureUsageReservations.Count().Should().Be(1);

        await useCase.CaptureFeatureReservationAsync(first.ReservationId);
        await useCase.CaptureFeatureReservationAsync(first.ReservationId);

        context.UserWallets.Single(x => x.UserId == userId).Balance.Should().Be(500);
        context.CreditTransactions.Count(x => x.TransactionType == CreditTransactionType.DEDUCT).Should().Be(1);
        context.FeatureUsageReservations.Single().Status.Should().Be("Captured");

        await useCase.RefundFeatureReservationAsync(userId, referenceId, "technical_failure");
        await useCase.RefundFeatureReservationAsync(userId, referenceId, "technical_failure");

        context.UserWallets.Single(x => x.UserId == userId).Balance.Should().Be(1_500);
        context.CreditTransactions.Count(x => x.TransactionType == CreditTransactionType.REFUND).Should().Be(1);
        context.FeatureUsageReservations.Single().Status.Should().Be("Refunded");
    }

    [Fact]
    public async Task SubscriptionReservation_CountsOnlyActiveReservationsAndReleasesQuota()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();
        var now = DateTime.UtcNow;
        context.UserWallets.Add(new UserWallets
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Balance = 0,
            UpdatedAt = now
        });
        context.Subscriptions.Add(new Subscriptions
        {
            Id = 7,
            Name = "Candidate",
            Status = SubscriptionStatus.ACTIVE,
            FeaturesConfig = JsonSerializer.Serialize(new
            {
                Role = "CANDIDATE",
                CvMatchLimit = 1,
                MockInterviewLimit = 1,
                LearningPathLimit = 1,
                LearningPathSlotLimit = 1,
                CoinCredit = 0
            })
        });
        context.UserSubscriptions.Add(new UserSubscriptions
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SubId = 7,
            Status = UserSubscriptionStatus.ACTIVE,
            StartDate = now.AddDays(-1),
            EndDate = now.AddDays(1)
        });
        await context.SaveChangesAsync();
        var useCase = CreateUseCase(context);

        var firstReference = Guid.NewGuid();
        var first = await useCase.ReserveFeatureAsync(userId, "CvJdMatching", firstReference);
        first.Source.Should().Be("Subscription");

        var second = () => useCase.ReserveFeatureAsync(userId, "CvJdMatching", Guid.NewGuid());
        await second.Should().ThrowAsync<InvalidOperationException>();

        await useCase.RefundFeatureReservationAsync(userId, firstReference, "technical_failure");
        var afterRelease = await useCase.ReserveFeatureAsync(userId, "CvJdMatching", Guid.NewGuid());
        afterRelease.Source.Should().Be("Subscription");
    }

    [Fact]
    public async Task Reserve_WithSameReferenceForAnotherUser_IsRejected()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var referenceId = Guid.NewGuid();
        await using var context = CreateContext();
        context.UserWallets.AddRange(
            new UserWallets { Id = Guid.NewGuid(), UserId = ownerId, Balance = 1_000, UpdatedAt = DateTime.UtcNow },
            new UserWallets { Id = Guid.NewGuid(), UserId = otherUserId, Balance = 1_000, UpdatedAt = DateTime.UtcNow });
        context.CoinFeatures.Add(new CoinFeatures
        {
            FeatureKey = "CvJdMatching",
            CoinCost = 1_000,
            Description = "matching"
        });
        await context.SaveChangesAsync();
        var useCase = CreateUseCase(context);
        await useCase.ReserveFeatureAsync(ownerId, "CvJdMatching", referenceId);

        var action = () => useCase.ReserveFeatureAsync(otherUserId, "CvJdMatching", referenceId);
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    private static CandidateFeatureUsageUseCase CreateUseCase(ITHunterviewContext context)
    {
        var config = new Mock<ISystemConfigRepository>();
        var repository = new FeatureUsageReservationRepository(context);
        return new CandidateFeatureUsageUseCase(context, config.Object, repository);
    }

    private static ITHunterviewContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ITHunterviewContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new BillingTestContext(options);
    }

    private sealed class BillingTestContext : ITHunterviewContext
    {
        public BillingTestContext(DbContextOptions<ITHunterviewContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            var keep = new[]
            {
                typeof(CvJobMatchScores),
                typeof(FeatureUsageReservations),
                typeof(UserWallets),
                typeof(UserSubscriptions),
                typeof(Subscriptions),
                typeof(CoinFeatures),
                typeof(CreditTransactions)
            };

            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                         .Where(type => !keep.Contains(type.ClrType))
                         .Select(type => type.ClrType)
                         .Distinct()
                         .ToList())
            {
                modelBuilder.Ignore(entityType);
            }
        }
    }
}
