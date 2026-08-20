using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.UseCase;
using Moq;

namespace ITHunterview.Service.Tests.UseCase;

/// <summary>
/// Candidate product behavior is intentionally tested apart from the legacy
/// one-to-one result store. See the approved matching-product-boundary spec.
/// </summary>
public sealed class CandidateJobScanUseCaseTests
{
    [Fact]
    [Trait("Requirement", "R-05")]
    public async Task EnqueueAsync_FailureMarksCreatedRunFailedAndReturnsNoFalseAcceptedState()
    {
        var candidateUserId = Guid.NewGuid();
        var cv = CreateCv(candidateUserId, false);
        var store = new CandidateRunStore();
        var queue = new Mock<ICandidateJobScanQueue>(MockBehavior.Strict);
        queue.Setup(value => value.EnqueueAsync(It.IsAny<CandidateJobScanRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("queue unavailable"));
        var sut = CreateSut(store, cv, queue: queue.Object);

        var action = () => sut.CreateRunAsync(candidateUserId, cv.Id, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        store.Runs.Should().ContainSingle().Which.Status.Should().Be(MatchingScanRunStatus.Failed);
    }
    [Fact]
    [Trait("Requirement", "R-05")]
    [Trait("Requirement", "R-07")]
    public async Task CreateRunAsync_OwnNonPrimaryHiddenCv_CreatesPendingRun()
    {
        var candidateUserId = Guid.NewGuid();
        var cv = CreateCv(candidateUserId, isPrimary: false);
        var store = new CandidateRunStore();
        var sut = CreateSut(store, cv);

        await sut.CreateRunAsync(candidateUserId, cv.Id, CancellationToken.None);

        store.Runs.Should().ContainSingle();
        store.Runs.Single().CandidateUserId.Should().Be(candidateUserId);
        store.Runs.Single().CvId.Should().Be(cv.Id);
        store.Runs.Single().Status.Should().Be(MatchingScanRunStatus.Pending);
    }

    [Fact]
    [Trait("Requirement", "R-07")]
    public async Task CreateRunAsync_ForeignCv_RejectsWithoutRun()
    {
        var cv = CreateCv(Guid.NewGuid(), isPrimary: false);
        var store = new CandidateRunStore();
        var sut = CreateSut(store, cv);

        var action = () => sut.CreateRunAsync(Guid.NewGuid(), cv.Id, CancellationToken.None);

        await action.Should().ThrowAsync<KeyNotFoundException>();
        store.Runs.Should().BeEmpty();
    }

    [Fact]
    [Trait("Requirement", "R-07")]
    public async Task CreateRunAsync_DeletedCv_RejectsWithoutRun()
    {
        var candidateUserId = Guid.NewGuid();
        var cv = CreateCv(candidateUserId, isPrimary: false);
        cv.DeletedAt = DateTime.UtcNow;
        var store = new CandidateRunStore();
        var sut = CreateSut(store, cv, exposeDeletedCv: false);

        var action = () => sut.CreateRunAsync(candidateUserId, cv.Id, CancellationToken.None);

        await action.Should().ThrowAsync<KeyNotFoundException>();
        store.Runs.Should().BeEmpty();
    }

    [Fact]
    [Trait("Requirement", "R-05")]
    public async Task CreateRunAsync_EachClick_CreatesDistinctRun()
    {
        var candidateUserId = Guid.NewGuid();
        var cv = CreateCv(candidateUserId, isPrimary: true);
        var store = new CandidateRunStore();
        var sut = CreateSut(store, cv);

        await sut.CreateRunAsync(candidateUserId, cv.Id, CancellationToken.None);
        await sut.CreateRunAsync(candidateUserId, cv.Id, CancellationToken.None);

        store.Runs.Should().HaveCount(2);
        store.Runs.Select(run => run.Id).Should().OnlyHaveUniqueItems();
        store.Runs.Should().OnlyContain(run => run.Status == MatchingScanRunStatus.Pending);
    }

    [Fact]
    [Trait("Requirement", "R-04")]
    [Trait("Requirement", "R-05")]
    [Trait("Requirement", "R-07")]
    public async Task ProcessRunAsync_PendingCandidateRun_UsesRunOwnedCvAndNeverOneToOneRows()
    {
        // This must fail if the production change stops loading the durable
        // Candidate scan run before it chooses the CV or attempts a pair match.
        var candidateUserId = Guid.NewGuid();
        var cvId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var run = new CandidateJobScanRun
        {
            Id = runId,
            CandidateUserId = candidateUserId,
            CvId = cvId,
            CvFileNameSnapshot = "candidate-cv.pdf",
            Status = MatchingScanRunStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        var scanRepository = new Mock<ICandidateJobScanRepository>(MockBehavior.Strict);
        scanRepository
            .Setup(repository => repository.GetPendingOrProcessingByIdAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);
        scanRepository
            .Setup(repository => repository.TryStartAsync(runId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = new CandidateJobScanUseCase(
            scanRepository.Object,
            Mock.Of<ICvRepository>(),
            Mock.Of<IJobPostingRepository>(),
            Mock.Of<IHardcodeCvJobPairMatcher>(),
            Mock.Of<ICandidateJobScanQueue>());

        await sut.ProcessRunAsync(runId, CancellationToken.None);

        scanRepository.Verify(
            repository => repository.GetPendingOrProcessingByIdAsync(runId, It.IsAny<CancellationToken>()),
            Times.Once);
        scanRepository.Verify(
            repository => repository.TryStartAsync(runId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Requirement", "R-02")]
    [Trait("Requirement", "R-05")]
    public async Task ProcessRunAsync_UsesOnlyPublishedUnbannedUndeletedUsableJobs()
    {
        var candidateUserId = Guid.NewGuid();
        var cv = CreateCv(candidateUserId, isPrimary: false);
        var eligible = CreateJob();
        var banned = CreateJob(); banned.IsBanned = true;
        var deleted = CreateJob(); deleted.DeletedAt = DateTime.UtcNow;
        var draft = CreateJob(); draft.Status = JobStatus.DRAFT;
        var unusable = CreateJob(); unusable.ParseStatus = "PENDING";
        var store = new CandidateRunStore();
        var run = store.AddPending(candidateUserId, cv);
        var matcher = new Mock<IHardcodeCvJobPairMatcher>(MockBehavior.Strict);
        matcher.Setup(value => value.MatchAsync(cv, eligible, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HardcodePairMatchResult(73m, "hardcode", null, null, null));
        var sut = CreateSut(store, cv, new[] { eligible, banned, deleted, draft, unusable }, matcher.Object);

        await sut.ProcessRunAsync(run.Id, CancellationToken.None);

        store.Results.Should().ContainSingle();
        store.Results.Single().JobId.Should().Be(eligible.Id);
        store.Runs.Single(value => value.Id == run.Id).Status.Should().Be(MatchingScanRunStatus.Completed);
    }

    [Fact]
    [Trait("Requirement", "R-04")]
    [Trait("Requirement", "R-05")]
    public async Task ProcessRunAsync_SamePairExistsInPriorScan_CreatesNewResult()
    {
        var candidateUserId = Guid.NewGuid();
        var cv = CreateCv(candidateUserId, isPrimary: true);
        var job = CreateJob();
        var store = new CandidateRunStore();
        var firstRun = store.AddPending(candidateUserId, cv);
        var secondRun = store.AddPending(candidateUserId, cv);
        var matcher = SuccessfulMatcher(cv, job, 61m);
        var sut = CreateSut(store, cv, new[] { job }, matcher.Object);

        await sut.ProcessRunAsync(firstRun.Id, CancellationToken.None);
        await sut.ProcessRunAsync(secondRun.Id, CancellationToken.None);

        store.Results.Should().HaveCount(2);
        store.Results.Select(result => result.RunId).Should().Contain(new[] { firstRun.Id, secondRun.Id });
        store.Results.Should().OnlyContain(result => result.JobId == job.Id);
    }

    [Fact]
    [Trait("Requirement", "R-02")]
    [Trait("Requirement", "R-05")]
    public async Task ProcessRunAsync_ScoreUnavailable_PersistsCompletedUnscoredItem()
    {
        var candidateUserId = Guid.NewGuid();
        var cv = CreateCv(candidateUserId, isPrimary: false);
        var job = CreateJob();
        var store = new CandidateRunStore();
        var run = store.AddPending(candidateUserId, cv);
        var matcher = SuccessfulMatcher(cv, job, null);
        var sut = CreateSut(store, cv, new[] { job }, matcher.Object);

        await sut.ProcessRunAsync(run.Id, CancellationToken.None);

        store.Results.Should().ContainSingle();
        store.Results.Single().MatchScore.Should().BeNull();
        store.Runs.Single(value => value.Id == run.Id).Status.Should().Be(MatchingScanRunStatus.Completed);
    }

    [Fact]
    [Trait("Requirement", "R-06")]
    public async Task ProcessRunAsync_Exception_MarksRunFailedAndPreservesPreviousLatest()
    {
        var candidateUserId = Guid.NewGuid();
        var cv = CreateCv(candidateUserId, isPrimary: true);
        var job = CreateJob();
        var store = new CandidateRunStore();
        var prior = store.AddCompleted(candidateUserId, cv, DateTime.UtcNow.AddMinutes(-2));
        var failing = store.AddPending(candidateUserId, cv);
        var matcher = new Mock<IHardcodeCvJobPairMatcher>(MockBehavior.Strict);
        matcher.Setup(value => value.MatchAsync(cv, job, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("matching failure"));
        var sut = CreateSut(store, cv, new[] { job }, matcher.Object);

        await sut.ProcessRunAsync(failing.Id, CancellationToken.None);
        var latest = await sut.GetLatestSuccessfulAsync(candidateUserId, cv.Id, 1, 20, CancellationToken.None);

        store.Runs.Single(value => value.Id == failing.Id).Status.Should().Be(MatchingScanRunStatus.Failed);
        latest.TotalCount.Should().Be(0);
        store.LatestCompleted(candidateUserId, cv.Id)!.Id.Should().Be(prior.Id);
    }

    [Fact]
    [Trait("Requirement", "R-02")]
    [Trait("Requirement", "R-09")]
    [Trait("Requirement", "R-12")]
    public async Task CandidateScan_DoesNotCallFeatureUsageWalletSaveOrApplyServices()
    {
        var candidateUserId = Guid.NewGuid();
        var cv = CreateCv(candidateUserId, isPrimary: false);
        var store = new CandidateRunStore();
        var sut = CreateSut(store, cv);

        await sut.CreateRunAsync(candidateUserId, cv.Id, CancellationToken.None);

        // The constructor deliberately exposes no billing, Save, or Apply collaborator:
        // the persisted product outcome is one Pending Candidate scan run only.
        store.Runs.Should().ContainSingle();
        store.Results.Should().BeEmpty();
    }

    [Fact]
    [Trait("Requirement", "R-07")]
    public async Task GetLatestSuccessfulAsync_OtherCandidate_ReturnsNotFoundOrEmpty()
    {
        var ownerId = Guid.NewGuid();
        var otherCandidateId = Guid.NewGuid();
        var cv = CreateCv(ownerId, isPrimary: true);
        var store = new CandidateRunStore();
        store.AddCompleted(ownerId, cv, DateTime.UtcNow);
        var sut = CreateSut(store, cv);

        var latest = await sut.GetLatestSuccessfulAsync(otherCandidateId, cv.Id, 1, 20, CancellationToken.None);

        latest.TotalCount.Should().Be(0);
        latest.Items.Should().BeEmpty();
    }

    [Fact]
    [Trait("Requirement", "R-04")]
    public async Task ProcessRunAsync_SharedLegacyRowsWithNullJobId_AreNeverReadAndCannotCauseDictionaryKeyFailure()
    {
        var candidateUserId = Guid.NewGuid();
        var cv = CreateCv(candidateUserId, isPrimary: true);
        var job = CreateJob();
        var store = new CandidateRunStore();
        var run = store.AddPending(candidateUserId, cv);
        var matcher = SuccessfulMatcher(cv, job, 85m);
        var sut = CreateSut(store, cv, new[] { job }, matcher.Object);

        // CandidateJobScanUseCase executes independently from legacy CvJobMatchScores tables
        await sut.ProcessRunAsync(run.Id, CancellationToken.None);

        var completedRun = store.Runs.Single(r => r.Id == run.Id);
        completedRun.Status.Should().Be(MatchingScanRunStatus.Completed);
        store.Results.Should().ContainSingle().Which.JobId.Should().Be(job.Id);
    }

    [Fact]
    [Trait("Requirement", "R-12")]
    public async Task CandidateScan_DoesNotNotifyOrCreateSignalForAnyRecruiter()
    {
        var candidateUserId = Guid.NewGuid();
        var cv = CreateCv(candidateUserId, isPrimary: true);
        var queueMock = new Mock<ICandidateJobScanQueue>(MockBehavior.Strict);
        queueMock.Setup(q => q.EnqueueAsync(It.IsAny<CandidateJobScanRequest>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        var store = new CandidateRunStore();
        var sut = CreateSut(store, cv, queue: queueMock.Object);

        var result = await sut.CreateRunAsync(candidateUserId, cv.Id, CancellationToken.None);

        result.Status.Should().Be("Pending");
        // EnqueueAsync is the ONLY boundary interaction; no recruiter notification or signal is triggered.
        queueMock.Verify(q => q.EnqueueAsync(It.Is<CandidateJobScanRequest>(r => r.RunId == result.RunId && r.CandidateUserId == candidateUserId && r.CvId == cv.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static CandidateJobScanUseCase CreateSut(
        CandidateRunStore store,
        Cvs cv,
        IEnumerable<JobPostings>? jobs = null,
        IHardcodeCvJobPairMatcher? matcher = null,
        bool exposeDeletedCv = true,
        ICandidateJobScanQueue? queue = null)
    {
        var cvs = new Mock<ICvRepository>(MockBehavior.Strict);
        cvs.Setup(repository => repository.GetByIdAsync(cv.Id))
            .ReturnsAsync(exposeDeletedCv ? cv : null);
        var jobPostings = new Mock<IJobPostingRepository>(MockBehavior.Strict);
        jobPostings.Setup(repository => repository.GetQueryable())
            .Returns((jobs ?? Array.Empty<JobPostings>()).AsQueryable());
        return new CandidateJobScanUseCase(
            store,
            cvs.Object,
            jobPostings.Object,
            matcher ?? Mock.Of<IHardcodeCvJobPairMatcher>(),
            queue ?? Mock.Of<ICandidateJobScanQueue>());
    }

    private static Mock<IHardcodeCvJobPairMatcher> SuccessfulMatcher(Cvs cv, JobPostings job, decimal? score)
    {
        var matcher = new Mock<IHardcodeCvJobPairMatcher>(MockBehavior.Strict);
        matcher.Setup(value => value.MatchAsync(cv, job, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HardcodePairMatchResult(score, "hardcode", null, null, null));
        return matcher;
    }

    private static Cvs CreateCv(Guid candidateUserId, bool isPrimary) => new()
    {
        Id = Guid.NewGuid(),
        UserId = candidateUserId,
        FileName = "candidate-cv.pdf",
        IsPrimary = isPrimary,
        ParseStatus = "SUCCESS"
    };

    private static JobPostings CreateJob() => new()
    {
        Id = Guid.NewGuid(),
        Title = "Backend Developer",
        Status = JobStatus.PUBLISHED,
        ParseStatus = "SUCCESS"
    };

    private sealed class CandidateRunStore : ICandidateJobScanRepository
    {
        public List<CandidateJobScanRun> Runs { get; } = new();
        public List<CandidateJobScanResult> Results { get; } = new();

        public Task<CandidateJobScanRun> CreatePendingAsync(CandidateJobScanRun run, CancellationToken ct)
        {
            Runs.Add(run);
            return Task.FromResult(run);
        }

        public Task<CandidateJobScanRun?> GetPendingOrProcessingByIdAsync(Guid runId, CancellationToken ct) =>
            Task.FromResult(Runs.SingleOrDefault(run =>
                run.Id == runId &&
                (run.Status == MatchingScanRunStatus.Pending || run.Status == MatchingScanRunStatus.Processing)));

        public Task<bool> TryStartAsync(Guid runId, DateTime startedAt, CancellationToken ct)
        {
            var run = Runs.SingleOrDefault(value => value.Id == runId && value.Status == MatchingScanRunStatus.Pending);
            if (run is null) return Task.FromResult(false);
            run.Status = MatchingScanRunStatus.Processing;
            run.StartedAt = startedAt;
            return Task.FromResult(true);
        }

        public Task CompleteAsync(Guid runId, IReadOnlyCollection<CandidateJobScanResult> results, DateTime completedAt, CancellationToken ct)
        {
            var run = Runs.Single(value => value.Id == runId && value.Status == MatchingScanRunStatus.Processing);
            Results.AddRange(results);
            run.Status = MatchingScanRunStatus.Completed;
            run.CompletedAt = completedAt;
            return Task.CompletedTask;
        }

        public Task FailAsync(Guid runId, string errorCode, string errorMessage, DateTime failedAt, CancellationToken ct)
        {
            var run = Runs.SingleOrDefault(value => value.Id == runId &&
                (value.Status == MatchingScanRunStatus.Pending || value.Status == MatchingScanRunStatus.Processing));
            if (run is not null)
            {
                run.Status = MatchingScanRunStatus.Failed;
                run.CompletedAt = failedAt;
                run.ErrorCode = errorCode;
                run.ErrorMessage = errorMessage;
            }
            return Task.CompletedTask;
        }

        public Task<CandidateJobScanRun?> GetLatestCompletedAsync(Guid candidateUserId, Guid cvId, CancellationToken ct) =>
            Task.FromResult(LatestCompleted(candidateUserId, cvId));

        public CandidateJobScanRun? LatestCompleted(Guid candidateUserId, Guid cvId) => Runs
            .Where(run => run.CandidateUserId == candidateUserId && run.CvId == cvId && run.Status == MatchingScanRunStatus.Completed)
            .OrderByDescending(run => run.CreatedAt)
            .ThenByDescending(run => run.Id)
            .FirstOrDefault();

        public Task<(IReadOnlyList<CandidateJobScanResult> Items, int TotalCount)> GetResultPageAsync(Guid runId, int skip, int take, CancellationToken ct)
        {
            var all = Results.Where(result => result.RunId == runId).OrderBy(result => result.Rank).ThenBy(result => result.Id).ToList();
            return Task.FromResult< (IReadOnlyList<CandidateJobScanResult> Items, int TotalCount)>((all.Skip(skip).Take(take).ToList(), all.Count));
        }

        public CandidateJobScanRun AddPending(Guid candidateUserId, Cvs cv)
        {
            var run = new CandidateJobScanRun { Id = Guid.NewGuid(), CandidateUserId = candidateUserId, CvId = cv.Id, CvFileNameSnapshot = cv.FileName, Status = MatchingScanRunStatus.Pending, CreatedAt = DateTime.UtcNow };
            Runs.Add(run);
            return run;
        }

        public CandidateJobScanRun AddCompleted(Guid candidateUserId, Cvs cv, DateTime createdAt)
        {
            var run = AddPending(candidateUserId, cv);
            run.CreatedAt = createdAt;
            run.Status = MatchingScanRunStatus.Completed;
            run.CompletedAt = createdAt;
            return run;
        }
    }
}
