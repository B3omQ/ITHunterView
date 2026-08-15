using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.DTOs.FeatureUsage;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.Service.Matching;
using ITHunterview.Service.UseCase;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ITHunterview.Service.Tests.Matching;

public sealed class CvJdMatchingSubmissionUseCaseTests
{
    [Fact]
    public async Task SubmitAsync_SameUserAndKeyReturnsExistingJobWithoutSecondCharge()
    {
        var userId = Guid.NewGuid();
        var preflight = new Mock<IMatchingInputPreflightUseCase>();
        var prepared = RawPreparedRequest("first CV text", "first JD text");
        preflight.Setup(x => x.PrepareAsync(userId, It.IsAny<MatchingRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(prepared);
        await using var context = CreateContext();
        var featureUsage = CreateFeatureUsageMock();
        var submission = CreateSubmission(context, preflight.Object, featureUsage.Object);
        var request = RawRequest("first CV text", "first JD text");

        var first = await submission.SubmitAsync(userId, request, "same-key");
        var second = await submission.SubmitAsync(userId, request, "same-key");

        first.IsExisting.Should().BeFalse();
        second.IsExisting.Should().BeTrue();
        second.JobId.Should().Be(first.JobId);
        context.CvJobMatchScores.Count().Should().Be(1);
        context.CvJobMatchScores.Single().ProcessingStage.Should().Be("queued");
        featureUsage.Verify(x => x.ReserveFeatureAsync(
            userId,
            CvJdMatchingSubmissionUseCase.FeatureKey,
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Once);
        featureUsage.Verify(x => x.CaptureFeatureReservationAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_ReusingKeyWithDifferentFingerprintIsRejectedWithoutNewJob()
    {
        var userId = Guid.NewGuid();
        var preflight = new Mock<IMatchingInputPreflightUseCase>();
        preflight.Setup(x => x.PrepareAsync(userId, It.IsAny<MatchingRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, MatchingRequestDto request, CancellationToken _) =>
                RawPreparedRequest(request.CvText!, request.RawJdText!));
        await using var context = CreateContext();
        var featureUsage = CreateFeatureUsageMock();
        var submission = CreateSubmission(context, preflight.Object, featureUsage.Object);

        await submission.SubmitAsync(userId, RawRequest("first CV text", "first JD text"), "same-key");
        var action = () => submission.SubmitAsync(userId, RawRequest("different CV text", "first JD text"), "same-key");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("IDEMPOTENCY_KEY_REUSED");
        context.CvJobMatchScores.Count().Should().Be(1);
        featureUsage.Verify(x => x.ReserveFeatureAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_RejectsMissingOrMalformedIdempotencyKeyBeforeDatabaseMutation()
    {
        var preflight = new Mock<IMatchingInputPreflightUseCase>();
        await using var context = CreateContext();
        var featureUsage = CreateFeatureUsageMock();
        var submission = CreateSubmission(context, preflight.Object, featureUsage.Object);

        var action = () => submission.SubmitAsync(
            Guid.NewGuid(),
            RawRequest("first CV text", "first JD text"),
            "bad key");

        await action.Should().ThrowAsync<ArgumentException>().WithMessage("IDEMPOTENCY_KEY_INVALID*");
        context.CvJobMatchScores.Should().BeEmpty();
        preflight.Verify(x => x.PrepareAsync(It.IsAny<Guid>(), It.IsAny<MatchingRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

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
        return new SubmissionTestContext(options);
    }

    private sealed class SubmissionTestContext : ITHunterviewContext
    {
        public SubmissionTestContext(DbContextOptions<ITHunterviewContext> options)
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
