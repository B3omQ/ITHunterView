using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.FeatureUsage;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.UseCase;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ITHunterview.Service.Tests.Matching;

public sealed class CvJdMatchingRetryUseCaseTests
{
    [Fact]
    public async Task RetryAsync_CreatesSnapshotBackedChildAndCapturesNewReservation()
    {
        await using var context = CreateContext();
        var original = CreateFailedJob("AI_PROVIDER_TIMEOUT");
        context.CvJobMatchScores.Add(original);
        await context.SaveChangesAsync();
        var featureUsage = CreateFeatureUsageMock();
        var retry = CreateRetry(context, featureUsage.Object);

        var result = await retry.RetryAsync(original.UserId, original.Id, "retry-1");
        var child = await context.CvJobMatchScores.SingleAsync(x => x.Id == result.JobId);

        result.IsExisting.Should().BeFalse();
        child.Status.Should().Be("Pending");
        child.RetryOfJobId.Should().Be(original.Id);
        child.InputSnapshotJson.Should().Be(original.InputSnapshotJson);
        child.InputHash.Should().Be(original.InputHash);
        child.BillingReservationId.Should().NotBeNull();
        original.ManualRetryUsed.Should().BeTrue();
        featureUsage.Verify(x => x.ReserveFeatureAsync(
            original.UserId,
            CvJdMatchingSubmissionUseCase.FeatureKey,
            child.Id,
            It.IsAny<CancellationToken>()), Times.Once);
        featureUsage.Verify(x => x.CaptureFeatureReservationAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RetryAsync_IsIdempotentForSameRetryKey()
    {
        await using var context = CreateContext();
        var original = CreateFailedJob("AI_PROVIDER_INVALID_JSON");
        context.CvJobMatchScores.Add(original);
        await context.SaveChangesAsync();
        var featureUsage = CreateFeatureUsageMock();
        var retry = CreateRetry(context, featureUsage.Object);

        var first = await retry.RetryAsync(original.UserId, original.Id, "retry-2");
        var second = await retry.RetryAsync(original.UserId, original.Id, "retry-2");

        second.IsExisting.Should().BeTrue();
        second.JobId.Should().Be(first.JobId);
        context.CvJobMatchScores.Count().Should().Be(2);
        featureUsage.Verify(x => x.ReserveFeatureAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RetryAsync_RejectsNonRetryableFailure()
    {
        await using var context = CreateContext();
        var original = CreateFailedJob("MATCHING_INPUT_INVALID");
        context.CvJobMatchScores.Add(original);
        await context.SaveChangesAsync();
        var retry = CreateRetry(context, CreateFeatureUsageMock().Object);

        var action = () => retry.RetryAsync(original.UserId, original.Id, "retry-3");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("MATCHING_RETRY_NOT_ALLOWED");
        context.CvJobMatchScores.Count().Should().Be(1);
    }

    private static CvJdMatchingRetryUseCase CreateRetry(
        ITHunterviewContext context,
        ICandidateFeatureUsageUseCase featureUsage)
        => new(context, new CvJdMatchingJobRepository(context), featureUsage);

    private static Mock<ICandidateFeatureUsageUseCase> CreateFeatureUsageMock()
    {
        var mock = new Mock<ICandidateFeatureUsageUseCase>();
        mock.Setup(x => x.ReserveFeatureAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, string feature, Guid reference, CancellationToken _) =>
                new FeatureReservationResult(Guid.NewGuid(), reference, feature, "Coin", "Reserved", 1000, null));
        mock.Setup(x => x.CaptureFeatureReservationAsync(
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    private static CvJobMatchScores CreateFailedJob(string errorCode)
    {
        var now = new DateTime(2026, 8, 2, 8, 0, 0, DateTimeKind.Utc);
        return new CvJobMatchScores
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CvId = Guid.NewGuid(),
            JobId = Guid.NewGuid(),
            CvFileName = "cv.pdf",
            JdTitle = "Engineer",
            RawJdText = "Job text",
            MatchType = "AI",
            Status = "Failed",
            MatchDetails = string.Empty,
            ErrorCode = errorCode,
            ErrorMessage = errorCode,
            InputSnapshotJson = "{\"schemaVersion\":\"matching-context/v1\"}",
            InputHash = "abc123",
            IdempotencyKey = "original-key",
            IdempotencyRequestHash = "request-hash",
            MaxAttempts = 3,
            CreatedAt = now,
            UpdatedAt = now,
            CompletedAt = now
        };
    }

    private static ITHunterviewContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ITHunterviewContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new RetryTestContext(options);
    }

    private sealed class RetryTestContext : ITHunterviewContext
    {
        public RetryTestContext(DbContextOptions<ITHunterviewContext> options)
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
