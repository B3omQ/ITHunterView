using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.UseCase;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Tests.Matching;

public sealed class CvJdMatchingHistoryUseCaseTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 2, 8, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("Pending")]
    [InlineData("RetryScheduled")]
    [InlineData("Processing")]
    public async Task HideAsync_ActiveJob_ReturnsActiveJobWithoutMutation(string status)
    {
        await using var context = CreateContext();
        var job = CreateJob(status, Guid.NewGuid());
        context.CvJobMatchScores.Add(job);
        await context.SaveChangesAsync();

        var result = await CreateUseCase(context).HideAsync(job.Id, job.UserId, UtcNow, CancellationToken.None);

        result.Should().Be(HideMatchHistoryResult.ActiveJob);
        job.HistoryHiddenAt.Should().BeNull();
        context.CvJobMatchScores.Should().ContainSingle(x => x.Id == job.Id);
    }

    [Theory]
    [InlineData("Completed")]
    [InlineData("Failed")]
    public async Task HideAsync_TerminalJob_SoftHidesWithoutDeletingAudit(string status)
    {
        await using var context = CreateContext();
        var job = CreateJob(status, Guid.NewGuid());
        job.BillingReservationId = Guid.NewGuid();
        context.CvJobMatchScores.Add(job);
        await context.SaveChangesAsync();

        var result = await CreateUseCase(context).HideAsync(job.Id, job.UserId, UtcNow, CancellationToken.None);

        result.Should().Be(HideMatchHistoryResult.Hidden);
        var persisted = await context.CvJobMatchScores.SingleAsync(x => x.Id == job.Id);
        persisted.HistoryHiddenAt.Should().Be(UtcNow);
        persisted.BillingReservationId.Should().NotBeNull();
    }

    [Fact]
    public async Task HideAsync_DifferentUser_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var job = CreateJob("Completed", Guid.NewGuid());
        context.CvJobMatchScores.Add(job);
        await context.SaveChangesAsync();

        var result = await CreateUseCase(context).HideAsync(job.Id, Guid.NewGuid(), UtcNow, CancellationToken.None);

        result.Should().Be(HideMatchHistoryResult.NotFound);
        job.HistoryHiddenAt.Should().BeNull();
    }

    [Fact]
    public async Task HideAsync_ParentWithRetryChild_PreservesRetryLineage()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var parent = CreateJob("Failed", userId);
        var child = CreateJob("Pending", userId);
        child.RetryOfJobId = parent.Id;
        context.CvJobMatchScores.AddRange(parent, child);
        await context.SaveChangesAsync();

        var result = await CreateUseCase(context).HideAsync(parent.Id, userId, UtcNow, CancellationToken.None);

        result.Should().Be(HideMatchHistoryResult.Hidden);
        var persistedChild = await context.CvJobMatchScores.SingleAsync(x => x.Id == child.Id);
        persistedChild.RetryOfJobId.Should().Be(parent.Id);
    }

    [Fact]
    public async Task HideAsync_RepeatedCall_IsIdempotent()
    {
        await using var context = CreateContext();
        var job = CreateJob("Completed", Guid.NewGuid());
        context.CvJobMatchScores.Add(job);
        await context.SaveChangesAsync();

        var useCase = CreateUseCase(context);
        var first = await useCase.HideAsync(job.Id, job.UserId, UtcNow, CancellationToken.None);
        var second = await useCase.HideAsync(job.Id, job.UserId, UtcNow.AddMinutes(1), CancellationToken.None);

        first.Should().Be(HideMatchHistoryResult.Hidden);
        second.Should().Be(HideMatchHistoryResult.Hidden);
        context.CvJobMatchScores.Single().HistoryHiddenAt.Should().Be(UtcNow);
    }

    private static CvJdMatchingHistoryUseCase CreateUseCase(ITHunterviewContext context)
        => new(new CvJdMatchingHistoryRepository(context));

    private static CvJobMatchScores CreateJob(string status, Guid userId)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MatchType = "AI",
            Status = status,
            MatchDetails = "details",
            CreatedAt = UtcNow,
            UpdatedAt = UtcNow
        };

    private static ITHunterviewContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ITHunterviewContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new HistoryTestContext(options);
    }

    private sealed class HistoryTestContext : ITHunterviewContext
    {
        public HistoryTestContext(DbContextOptions<ITHunterviewContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                         .Where(type => type.ClrType != typeof(CvJobMatchScores))
                         .Select(type => type.ClrType)
                         .Distinct()
                         .ToList())
            {
                modelBuilder.Ignore(entityType);
            }
        }
    }
}
