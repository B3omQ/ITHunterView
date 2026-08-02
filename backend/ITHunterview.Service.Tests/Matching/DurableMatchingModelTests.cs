using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Tests.Matching;

public sealed class DurableMatchingModelTests
{
    [Fact]
    public void CvJobMatchScores_ContainsDurableRuntimeFields()
    {
        var expected = new Dictionary<string, Type>
        {
            [nameof(CvJobMatchScores.InputSnapshotJson)] = typeof(string),
            [nameof(CvJobMatchScores.InputHash)] = typeof(string),
            [nameof(CvJobMatchScores.IdempotencyKey)] = typeof(string),
            [nameof(CvJobMatchScores.IdempotencyRequestHash)] = typeof(string),
            [nameof(CvJobMatchScores.AttemptCount)] = typeof(int),
            [nameof(CvJobMatchScores.MaxAttempts)] = typeof(int),
            [nameof(CvJobMatchScores.CreatedAt)] = typeof(DateTime),
            [nameof(CvJobMatchScores.StartedAt)] = typeof(DateTime?),
            [nameof(CvJobMatchScores.CompletedAt)] = typeof(DateTime?),
            [nameof(CvJobMatchScores.NextAttemptAt)] = typeof(DateTime?),
            [nameof(CvJobMatchScores.LeaseOwner)] = typeof(string),
            [nameof(CvJobMatchScores.LeaseToken)] = typeof(Guid?),
            [nameof(CvJobMatchScores.LeaseExpiresAt)] = typeof(DateTime?),
            [nameof(CvJobMatchScores.LastHeartbeatAt)] = typeof(DateTime?),
            [nameof(CvJobMatchScores.BillingReservationId)] = typeof(Guid?),
            [nameof(CvJobMatchScores.ErrorCode)] = typeof(string),
            [nameof(CvJobMatchScores.ManualRetryUsed)] = typeof(bool),
            [nameof(CvJobMatchScores.RetryOfJobId)] = typeof(Guid?)
        };

        foreach (var (name, type) in expected)
        {
            typeof(CvJobMatchScores).GetProperty(name)?.PropertyType
                .Should().Be(type, because: $"{name} is part of the durable AI job contract");
        }
    }

    [Fact]
    public void FeatureUsageReservations_ContainsIdempotentBillingFields()
    {
        var expected = new Dictionary<string, Type>
        {
            [nameof(FeatureUsageReservations.Id)] = typeof(Guid),
            [nameof(FeatureUsageReservations.UserId)] = typeof(Guid),
            [nameof(FeatureUsageReservations.FeatureKey)] = typeof(string),
            [nameof(FeatureUsageReservations.ReferenceId)] = typeof(Guid),
            [nameof(FeatureUsageReservations.Source)] = typeof(string),
            [nameof(FeatureUsageReservations.Status)] = typeof(string),
            [nameof(FeatureUsageReservations.CoinAmount)] = typeof(int),
            [nameof(FeatureUsageReservations.DeductTransactionId)] = typeof(Guid?),
            [nameof(FeatureUsageReservations.RefundTransactionId)] = typeof(Guid?),
            [nameof(FeatureUsageReservations.CreatedAt)] = typeof(DateTime),
            [nameof(FeatureUsageReservations.CapturedAt)] = typeof(DateTime?),
            [nameof(FeatureUsageReservations.ReleasedAt)] = typeof(DateTime?),
            [nameof(FeatureUsageReservations.RefundedAt)] = typeof(DateTime?)
        };

        foreach (var (name, type) in expected)
        {
            typeof(FeatureUsageReservations).GetProperty(name)?.PropertyType
                .Should().Be(type, because: $"{name} is part of the billing reservation contract");
        }
    }

    [Fact]
    public void MatchingRuntimeFields_AreNullableForLegacyRows()
    {
        var options = new DbContextOptionsBuilder<ITHunterviewContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        using var context = new DurableTestContext(options);
        var entity = context.Model.FindEntityType(typeof(CvJobMatchScores));

        entity.Should().NotBeNull();
        entity!.FindProperty(nameof(CvJobMatchScores.InputSnapshotJson))!.IsNullable.Should().BeTrue();
        entity.FindProperty(nameof(CvJobMatchScores.InputHash))!.IsNullable.Should().BeTrue();
        entity.FindProperty(nameof(CvJobMatchScores.IdempotencyKey))!.IsNullable.Should().BeTrue();
        entity.FindProperty(nameof(CvJobMatchScores.IdempotencyRequestHash))!.IsNullable.Should().BeTrue();
        entity.FindProperty(nameof(CvJobMatchScores.LeaseToken))!.IsNullable.Should().BeTrue();
        entity.FindProperty(nameof(CvJobMatchScores.BillingReservationId))!.IsNullable.Should().BeTrue();
        entity.FindProperty(nameof(CvJobMatchScores.RetryOfJobId))!.IsNullable.Should().BeTrue();
    }

    private sealed class DurableTestContext : ITHunterviewContext
    {
        public DurableTestContext(DbContextOptions<ITHunterviewContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                         .Where(type => type.ClrType != typeof(CvJobMatchScores) &&
                                        type.ClrType != typeof(FeatureUsageReservations))
                         .Select(type => type.ClrType)
                         .Distinct()
                         .ToList())
            {
                modelBuilder.Ignore(entityType);
            }
        }
    }
}
