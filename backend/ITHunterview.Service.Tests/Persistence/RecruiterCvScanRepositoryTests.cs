using FluentAssertions;
using FluentAssertions.Execution;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Tests.Persistence;

public sealed class RecruiterCvScanRepositoryContractTests
{
    [Fact]
    [Trait("Requirement", "R-05")]
    public async Task CreatePendingAsync_SameScopeTwice_CreatesDistinctRuns_WithoutPostgres()
    {
        await using var context = MatchingScanInMemoryContextFactory.Create();
        var repository = MatchingScanRepositoryFactory.Recruiter(context);
        var recruiterUserId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var jobId = Guid.NewGuid();

        var first = await repository.CreatePendingAsync(
            CreateRun(recruiterUserId, companyId, jobId, At(0)),
            CancellationToken.None);
        var second = await repository.CreatePendingAsync(
            CreateRun(recruiterUserId, companyId, jobId, At(0)),
            CancellationToken.None);

        first.Id.Should().NotBe(second.Id);
        (await context.RecruiterCvScanRuns.CountAsync()).Should().Be(2);
    }

    [Theory]
    [InlineData(InvalidInitialLifecycle.ProcessingStatus)]
    [InlineData(InvalidInitialLifecycle.CompletedStatus)]
    [InlineData(InvalidInitialLifecycle.FailedStatus)]
    [InlineData(InvalidInitialLifecycle.StartedAt)]
    [InlineData(InvalidInitialLifecycle.CompletedAt)]
    [InlineData(InvalidInitialLifecycle.ErrorCode)]
    [InlineData(InvalidInitialLifecycle.ErrorMessage)]
    [Trait("Requirement", "R-05")]
    public async Task CreatePendingAsync_InvalidInitialLifecycle_RejectsWithoutMutationOrPersistence_WithoutPostgres(
        InvalidInitialLifecycle invalidLifecycle)
    {
        await using var context = MatchingScanInMemoryContextFactory.Create();
        var repository = MatchingScanRepositoryFactory.Recruiter(context);
        var recruiterUserId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var supplied = CreateRun(recruiterUserId, companyId, jobId, At(0));
        switch (invalidLifecycle)
        {
            case InvalidInitialLifecycle.ProcessingStatus:
                supplied.Status = MatchingScanRunStatus.Processing;
                break;
            case InvalidInitialLifecycle.CompletedStatus:
                supplied.Status = MatchingScanRunStatus.Completed;
                break;
            case InvalidInitialLifecycle.FailedStatus:
                supplied.Status = MatchingScanRunStatus.Failed;
                break;
            case InvalidInitialLifecycle.StartedAt:
                supplied.StartedAt = At(1);
                break;
            case InvalidInitialLifecycle.CompletedAt:
                supplied.CompletedAt = At(2);
                break;
            case InvalidInitialLifecycle.ErrorCode:
                supplied.ErrorCode = "CALLER_SUPPLIED_ERROR";
                break;
            case InvalidInitialLifecycle.ErrorMessage:
                supplied.ErrorMessage = "caller-supplied lifecycle metadata";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(invalidLifecycle), invalidLifecycle, null);
        }

        var callerSnapshot = new RecruiterCvScanRun
        {
            Id = supplied.Id,
            RecruiterUserId = supplied.RecruiterUserId,
            RecruiterProfileId = supplied.RecruiterProfileId,
            CompanyId = supplied.CompanyId,
            JobId = supplied.JobId,
            JobTitleSnapshot = supplied.JobTitleSnapshot,
            Status = supplied.Status,
            CreatedAt = supplied.CreatedAt,
            StartedAt = supplied.StartedAt,
            CompletedAt = supplied.CompletedAt,
            ErrorCode = supplied.ErrorCode,
            ErrorMessage = supplied.ErrorMessage
        };

        var exception = await Record.ExceptionAsync(() =>
            repository.CreatePendingAsync(supplied, CancellationToken.None));
        var persistedCount = await context.RecruiterCvScanRuns.CountAsync(run => run.Id == supplied.Id);

        using (new AssertionScope())
        {
            exception.Should().BeAssignableTo<ArgumentException>();
            persistedCount.Should().Be(0);
            supplied.Should().BeEquivalentTo(callerSnapshot);
        }
    }

    [Fact]
    [Trait("Requirement", "R-06")]
    public async Task GetLatestCompletedAsync_UsesExactOwnerScopeAndCreatedOrder_WithoutPostgres()
    {
        await using var context = MatchingScanInMemoryContextFactory.Create();
        var repository = MatchingScanRepositoryFactory.Recruiter(context);
        var recruiterUserId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var olderCompleted = CreateRun(recruiterUserId, companyId, jobId, At(0), MatchingScanRunStatus.Completed);
        var newerCompleted = CreateRun(recruiterUserId, companyId, jobId, At(1), MatchingScanRunStatus.Completed);
        var newestFailed = CreateRun(recruiterUserId, companyId, jobId, At(2), MatchingScanRunStatus.Failed);
        context.RecruiterCvScanRuns.AddRange(olderCompleted, newerCompleted, newestFailed);
        await context.SaveChangesAsync();

        var latest = await repository.GetLatestCompletedAsync(
            recruiterUserId,
            companyId,
            jobId,
            CancellationToken.None);
        var otherRecruiter = await repository.GetLatestCompletedAsync(
            Guid.NewGuid(), companyId, jobId, CancellationToken.None);
        var otherCompany = await repository.GetLatestCompletedAsync(
            recruiterUserId, Guid.NewGuid(), jobId, CancellationToken.None);
        var otherJob = await repository.GetLatestCompletedAsync(
            recruiterUserId, companyId, Guid.NewGuid(), CancellationToken.None);

        latest!.Id.Should().Be(newerCompleted.Id);
        otherRecruiter.Should().BeNull();
        otherCompany.Should().BeNull();
        otherJob.Should().BeNull();
    }

    private static RecruiterCvScanRun CreateRun(
        Guid recruiterUserId,
        Guid companyId,
        Guid jobId,
        DateTime createdAt,
        MatchingScanRunStatus status = MatchingScanRunStatus.Pending) => new()
    {
        Id = Guid.NewGuid(),
        RecruiterUserId = recruiterUserId,
        RecruiterProfileId = Guid.NewGuid(),
        CompanyId = companyId,
        JobId = jobId,
        JobTitleSnapshot = "Synthetic job",
        Status = status,
        CreatedAt = createdAt
    };

    private static DateTime At(int minute) =>
        new DateTime(2026, 8, 16, 3, 0, 0, DateTimeKind.Utc).AddMinutes(minute);

    public enum InvalidInitialLifecycle
    {
        ProcessingStatus,
        CompletedStatus,
        FailedStatus,
        StartedAt,
        CompletedAt,
        ErrorCode,
        ErrorMessage
    }
}

[Collection(MatchingScanPostgresCollection.Name)]
public sealed class RecruiterCvScanRepositoryTests
{
    private readonly MatchingScanPostgresFixture _database;

    public RecruiterCvScanRepositoryTests(MatchingScanPostgresFixture database)
    {
        _database = database;
    }

    [Task6PostgresFact]
    [Trait("Requirement", "R-05")]
    public async Task CreatePendingAsync_SameScopeTwice_CreatesDistinctRuns()
    {
        var seed = await _database.SeedGraphAsync();
        await using var context = _database.CreateContext();
        var repository = MatchingScanRepositoryFactory.Recruiter(context);

        var first = await repository.CreatePendingAsync(CreateRun(seed, At(0)), CancellationToken.None);
        var second = await repository.CreatePendingAsync(CreateRun(seed, At(0)), CancellationToken.None);

        first.Id.Should().NotBe(second.Id);
        (await context.RecruiterCvScanRuns.CountAsync(run =>
                run.RecruiterUserId == seed.RecruiterUserId &&
                run.CompanyId == seed.CompanyId &&
                run.JobId == seed.JobId))
            .Should().Be(2);
    }

    [Task6PostgresFact]
    [Trait("Requirement", "R-05")]
    public async Task TryStartAsync_TwoConcurrentCallers_OnlyOneClaimsPendingRun()
    {
        var seed = await _database.SeedGraphAsync();
        Guid runId;
        await using (var createContext = _database.CreateContext())
        {
            var createRepository = MatchingScanRepositoryFactory.Recruiter(createContext);
            runId = (await createRepository.CreatePendingAsync(CreateRun(seed, At(0)), CancellationToken.None)).Id;
        }

        var updateBarrier = new MatchingScanUpdateBarrier("recruiter_cv_scan_runs");
        await using var firstContext = _database.CreateContext(updateBarrier.CreateParticipant());
        await using var secondContext = _database.CreateContext(updateBarrier.CreateParticipant());
        var firstRepository = MatchingScanRepositoryFactory.Recruiter(firstContext);
        var secondRepository = MatchingScanRepositoryFactory.Recruiter(secondContext);
        var first = Task.Run(() => firstRepository.TryStartAsync(runId, At(1), CancellationToken.None));
        var second = Task.Run(() => secondRepository.TryStartAsync(runId, At(2), CancellationToken.None));
        Exception? coordinationFailure = null;
        var bothReachedBeforeRelease = false;
        try
        {
            await updateBarrier.WaitForBothParticipantsAsync();
            bothReachedBeforeRelease =
                updateBarrier.ArrivedParticipantCount == 2 && !updateBarrier.IsReleased;
        }
        catch (Exception exception)
        {
            coordinationFailure = exception;
        }
        finally
        {
            updateBarrier.Release();
        }

        var claims = await Task.WhenAll(first, second)
            .WaitAsync(MatchingScanUpdateBarrier.RaceCompletionTimeout);

        coordinationFailure.Should().BeNull("both PostgreSQL UPDATE participants must reach the exact-table barrier");
        bothReachedBeforeRelease.Should().BeTrue();
        claims.Should().ContainSingle(result => result);
        await using var verifyContext = _database.CreateContext();
        var persisted = await verifyContext.RecruiterCvScanRuns.SingleAsync(run => run.Id == runId);
        persisted.Status.Should().Be(MatchingScanRunStatus.Processing);
        persisted.StartedAt.Should().BeOneOf(At(1), At(2));
    }

    [Task6PostgresFact]
    [Trait("Requirement", "R-05")]
    public async Task CompleteAsync_WritesItemsAndTerminalStateAtomically()
    {
        await AssertSuccessfulCompletionAsync(
            Guid.Parse("70000000-0000-0000-0000-000000000031"),
            Guid.Parse("70000000-0000-0000-0000-000000000032"));
    }

    [Task6PostgresFact]
    [Trait("Requirement", "R-05")]
    public async Task ResultsAndCompletedTransition_CommitTogether()
    {
        await AssertSuccessfulCompletionAsync(
            Guid.Parse("70000000-0000-0000-0000-000000000041"),
            Guid.Parse("70000000-0000-0000-0000-000000000042"));
    }

    [Task6PostgresFact]
    [Trait("Requirement", "R-05")]
    public async Task CompleteAsync_DuplicatePairInRun_RollsBackEntireCompletion()
    {
        var seed = await _database.SeedGraphAsync();
        var runId = await CreateProcessingRunAsync(seed, At(0));
        RecruiterCvScanRun beforeRun;
        await using (var beforeContext = _database.CreateContext())
        {
            beforeRun = await beforeContext.RecruiterCvScanRuns.AsNoTracking().SingleAsync(run => run.Id == runId);
            (await beforeContext.RecruiterCvScanResults.CountAsync(result => result.RunId == runId)).Should().Be(0);
        }

        await using var context = _database.CreateContext();
        var repository = MatchingScanRepositoryFactory.Recruiter(context);
        var duplicateResults = new[]
        {
            CreateResult(
                runId,
                seed.CvId,
                seed.CandidateUserId,
                1,
                Guid.Parse("70000000-0000-0000-0000-000000000011")),
            CreateResult(
                runId,
                seed.CvId,
                seed.CandidateUserId,
                2,
                Guid.Parse("70000000-0000-0000-0000-000000000012"))
        };

        var action = () => repository.CompleteAsync(runId, duplicateResults, At(2), CancellationToken.None);

        await action.Should().ThrowAsync<Exception>();
        await using var verifyContext = _database.CreateContext();
        var afterRun = await verifyContext.RecruiterCvScanRuns.AsNoTracking().SingleAsync(run => run.Id == runId);
        afterRun.Should().BeEquivalentTo(beforeRun);
        (await verifyContext.RecruiterCvScanResults.CountAsync(result => result.RunId == runId)).Should().Be(0);
    }

    [Task6PostgresFact]
    [Trait("Requirement", "R-05")]
    public async Task Failure_DoesNotPublishPartialResultsOrLatestSnapshot()
    {
        var seed = await _database.SeedGraphAsync();
        var completedRunId = await CreateProcessingRunAsync(seed, At(0));
        await using (var completedContext = _database.CreateContext())
        {
            var repository = MatchingScanRepositoryFactory.Recruiter(completedContext);
            await repository.CompleteAsync(
                completedRunId,
                [CreateResult(completedRunId, seed.CvId, seed.CandidateUserId, 1)],
                At(1),
                CancellationToken.None);
        }

        var failedCompletionRunId = await CreateProcessingRunAsync(seed, At(2));
        await using (var failingContext = _database.CreateContext())
        {
            var repository = MatchingScanRepositoryFactory.Recruiter(failingContext);
            var action = () => repository.CompleteAsync(
                failedCompletionRunId,
                [
                    CreateResult(failedCompletionRunId, seed.OtherCvId, seed.OtherCandidateUserId, 1),
                    CreateResult(failedCompletionRunId, seed.OtherCvId, seed.OtherCandidateUserId, 2)
                ],
                At(3),
                CancellationToken.None);
            await action.Should().ThrowAsync<Exception>();
        }

        await using var verifyContext = _database.CreateContext();
        var verifyRepository = MatchingScanRepositoryFactory.Recruiter(verifyContext);
        var latest = await verifyRepository.GetLatestCompletedAsync(
            seed.RecruiterUserId,
            seed.CompanyId,
            seed.JobId,
            CancellationToken.None);
        latest!.Id.Should().Be(completedRunId);
        (await verifyContext.RecruiterCvScanResults.CountAsync(result => result.RunId == failedCompletionRunId))
            .Should().Be(0);
    }

    [Task6PostgresFact]
    [Trait("Requirement", "R-05")]
    public async Task FailAsync_AfterCompleted_DoesNotMutateTerminalRun()
    {
        var seed = await _database.SeedGraphAsync();
        var runId = await CreateProcessingRunAsync(seed, At(0));
        await using var context = _database.CreateContext();
        var repository = MatchingScanRepositoryFactory.Recruiter(context);
        await repository.CompleteAsync(
            runId,
            [CreateResult(runId, seed.CvId, seed.CandidateUserId, 1)],
            At(2),
            CancellationToken.None);

        RecruiterCvScanRun beforeRun;
        IReadOnlyList<RecruiterCvScanResult> beforeResults;
        await using (var beforeContext = _database.CreateContext())
        {
            beforeRun = await beforeContext.RecruiterCvScanRuns.AsNoTracking().SingleAsync(run => run.Id == runId);
            beforeResults = await beforeContext.RecruiterCvScanResults.AsNoTracking()
                .Where(result => result.RunId == runId)
                .OrderBy(result => result.Id)
                .ToListAsync();
        }

        await repository.FailAsync(runId, "LATE_FAILURE", "synthetic late failure", At(3), CancellationToken.None);

        await using var verifyContext = _database.CreateContext();
        var afterRun = await verifyContext.RecruiterCvScanRuns.AsNoTracking().SingleAsync(run => run.Id == runId);
        var afterResults = await verifyContext.RecruiterCvScanResults.AsNoTracking()
            .Where(result => result.RunId == runId)
            .OrderBy(result => result.Id)
            .ToListAsync();
        afterRun.Should().BeEquivalentTo(beforeRun);
        afterResults.Should().BeEquivalentTo(beforeResults, options => options.WithStrictOrdering());
    }

    [Task6PostgresFact]
    [Trait("Requirement", "R-05")]
    public async Task CompleteAsync_AfterFailed_DoesNotReopenRun()
    {
        var seed = await _database.SeedGraphAsync();
        Guid runId;
        RecruiterCvScanRun beforeRun;
        await using (var context = _database.CreateContext())
        {
            var repository = MatchingScanRepositoryFactory.Recruiter(context);
            runId = (await repository.CreatePendingAsync(CreateRun(seed, At(0)), CancellationToken.None)).Id;
            await repository.FailAsync(runId, "SYNTHETIC_FAILURE", "synthetic failure", At(1), CancellationToken.None);
            beforeRun = await context.RecruiterCvScanRuns.AsNoTracking().SingleAsync(run => run.Id == runId);
            var action = () => repository.CompleteAsync(
                runId,
                [CreateResult(runId, seed.CvId, seed.CandidateUserId, 1)],
                At(2),
                CancellationToken.None);
            await action.Should().ThrowAsync<Exception>();
        }

        await using var verifyContext = _database.CreateContext();
        var afterRun = await verifyContext.RecruiterCvScanRuns.AsNoTracking().SingleAsync(run => run.Id == runId);
        afterRun.Should().BeEquivalentTo(beforeRun);
        (await verifyContext.RecruiterCvScanResults.CountAsync(result => result.RunId == runId)).Should().Be(0);
    }

    [Task6PostgresFact]
    [Trait("Requirement", "R-06")]
    public async Task GetLatestCompletedAsync_NewerFailed_ReturnsPreviousCompleted()
    {
        var seed = await _database.SeedGraphAsync();
        var completedRunId = await CreateProcessingRunAsync(seed, At(0));
        await using (var completedContext = _database.CreateContext())
        {
            var repository = MatchingScanRepositoryFactory.Recruiter(completedContext);
            await repository.CompleteAsync(completedRunId, [], At(1), CancellationToken.None);
        }

        await using var context = _database.CreateContext();
        var currentRepository = MatchingScanRepositoryFactory.Recruiter(context);
        var failed = await currentRepository.CreatePendingAsync(CreateRun(seed, At(2)), CancellationToken.None);
        await currentRepository.FailAsync(
            failed.Id,
            "SYNTHETIC_FAILURE",
            "synthetic failure",
            At(3),
            CancellationToken.None);

        var latest = await currentRepository.GetLatestCompletedAsync(
            seed.RecruiterUserId,
            seed.CompanyId,
            seed.JobId,
            CancellationToken.None);

        latest!.Id.Should().Be(completedRunId);
    }

    [Task6PostgresFact]
    [Trait("Requirement", "R-06")]
    public async Task GetLatestCompletedAsync_OlderFinishesLater_ReturnsNewerCreatedCompleted()
    {
        var seed = await _database.SeedGraphAsync();
        var olderRunId = await CreateProcessingRunAsync(seed, At(0));
        var newerRunId = await CreateProcessingRunAsync(seed, At(1));

        await using (var newerContext = _database.CreateContext())
        {
            var newerRepository = MatchingScanRepositoryFactory.Recruiter(newerContext);
            await newerRepository.CompleteAsync(newerRunId, [], At(2), CancellationToken.None);
        }

        await using (var olderContext = _database.CreateContext())
        {
            var olderRepository = MatchingScanRepositoryFactory.Recruiter(olderContext);
            await olderRepository.CompleteAsync(olderRunId, [], At(3), CancellationToken.None);
        }

        await using var readContext = _database.CreateContext();
        var readRepository = MatchingScanRepositoryFactory.Recruiter(readContext);
        var latest = await readRepository.GetLatestCompletedAsync(
            seed.RecruiterUserId,
            seed.CompanyId,
            seed.JobId,
            CancellationToken.None);

        latest!.Id.Should().Be(newerRunId);
        latest.CompletedAt.Should().Be(At(2));
    }

    [Task6PostgresFact]
    [Trait("Requirement", "R-05")]
    public async Task CompleteAsync_ZeroItems_PublishesSuccessfulEmptySnapshot()
    {
        var seed = await _database.SeedGraphAsync();
        var runId = await CreateProcessingRunAsync(seed, At(0));
        await using var context = _database.CreateContext();
        var repository = MatchingScanRepositoryFactory.Recruiter(context);

        await repository.CompleteAsync(runId, [], At(1), CancellationToken.None);

        await using var verifyContext = _database.CreateContext();
        var persisted = await verifyContext.RecruiterCvScanRuns.SingleAsync(run => run.Id == runId);
        persisted.Status.Should().Be(MatchingScanRunStatus.Completed);
        persisted.CompletedAt.Should().Be(At(1));
        (await verifyContext.RecruiterCvScanResults.CountAsync(result => result.RunId == runId)).Should().Be(0);
    }

    [Task6PostgresFact]
    [Trait("Requirement", "R-06")]
    public async Task GetLatestCompletedAsync_OtherOwner_ReturnsNull()
    {
        var seed = await _database.SeedGraphAsync();
        var runId = await CreateProcessingRunAsync(seed, At(0));
        await using var context = _database.CreateContext();
        var repository = MatchingScanRepositoryFactory.Recruiter(context);
        await repository.CompleteAsync(runId, [], At(1), CancellationToken.None);

        (await repository.GetLatestCompletedAsync(
                Guid.NewGuid(), seed.CompanyId, seed.JobId, CancellationToken.None))
            .Should().BeNull();
        (await repository.GetLatestCompletedAsync(
                seed.RecruiterUserId, Guid.NewGuid(), seed.JobId, CancellationToken.None))
            .Should().BeNull();
        (await repository.GetLatestCompletedAsync(
                seed.RecruiterUserId, seed.CompanyId, Guid.NewGuid(), CancellationToken.None))
            .Should().BeNull();
    }

    [Task6PostgresFact]
    [Trait("Requirement", "R-04")]
    public async Task GetResultPageAsync_ReturnsOnlyRequestedRun_InRankThenIdOrder()
    {
        var seed = await _database.SeedGraphAsync();
        var requestedRunId = await CreateProcessingRunAsync(seed, At(0));
        var otherRunId = await CreateProcessingRunAsync(seed, At(1));
        var rankOneLowerId = Guid.Parse("30000000-0000-0000-0000-000000000002");
        var rankOneHigherId = Guid.Parse("30000000-0000-0000-0000-000000000003");
        var rankTwoOpposingLowerId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        await using var context = _database.CreateContext();
        var repository = MatchingScanRepositoryFactory.Recruiter(context);
        await repository.CompleteAsync(
            requestedRunId,
            [
                CreateResult(requestedRunId, seed.OtherCvId, seed.OtherCandidateUserId, 1, rankOneHigherId),
                CreateResult(requestedRunId, seed.CvId, seed.CandidateUserId, 1, rankOneLowerId),
                CreateResult(requestedRunId, seed.ThirdCvId, seed.ThirdCandidateUserId, 2, rankTwoOpposingLowerId)
            ],
            At(2),
            CancellationToken.None);
        await repository.CompleteAsync(
            otherRunId,
            [CreateResult(otherRunId, seed.CvId, seed.CandidateUserId, 0)],
            At(3),
            CancellationToken.None);

        var firstPage = await repository.GetResultPageAsync(
            requestedRunId,
            skip: 0,
            take: 2,
            ct: CancellationToken.None);
        var secondPage = await repository.GetResultPageAsync(
            requestedRunId,
            skip: 2,
            take: 2,
            ct: CancellationToken.None);

        firstPage.TotalCount.Should().Be(3);
        firstPage.Items.Select(result => result.Id).Should().Equal(rankOneLowerId, rankOneHigherId);
        secondPage.TotalCount.Should().Be(3);
        secondPage.Items.Should().ContainSingle().Which.Id.Should().Be(rankTwoOpposingLowerId);
        firstPage.Items.Concat(secondPage.Items).Should().OnlyContain(result => result.RunId == requestedRunId);
    }

    [Task6PostgresFact]
    [Trait("Requirement", "R-10")]
    public async Task GetOwnedResultAsync_OtherRecruiter_ReturnsNull()
    {
        var seed = await _database.SeedGraphAsync();
        var runId = await CreateProcessingRunAsync(seed, At(0));
        var resultId = Guid.NewGuid();
        await using var context = _database.CreateContext();
        var repository = MatchingScanRepositoryFactory.Recruiter(context);
        await repository.CompleteAsync(
            runId,
            [CreateResult(runId, seed.CvId, seed.CandidateUserId, 1, resultId)],
            At(1),
            CancellationToken.None);

        var result = await repository.GetOwnedResultAsync(
            resultId,
            Guid.NewGuid(),
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Task6PostgresFact]
    [Trait("Requirement", "R-10")]
    public async Task GetOwnedResultAsync_ReturnsResultAndStoredRunProvenance()
    {
        var seed = await _database.SeedGraphAsync();
        var runId = await CreateProcessingRunAsync(seed, At(0));
        var resultId = Guid.NewGuid();
        await using var context = _database.CreateContext();
        var repository = MatchingScanRepositoryFactory.Recruiter(context);
        await repository.CompleteAsync(
            runId,
            [CreateResult(runId, seed.CvId, seed.CandidateUserId, 1, resultId)],
            At(1),
            CancellationToken.None);

        var owned = await repository.GetOwnedResultAsync(
            resultId,
            seed.RecruiterUserId,
            CancellationToken.None);

        owned.Should().NotBeNull();
        owned!.Value.Result.Id.Should().Be(resultId);
        owned.Value.Result.RunId.Should().Be(runId);
        owned.Value.Result.CvId.Should().Be(seed.CvId);
        owned.Value.Result.CandidateUserId.Should().Be(seed.CandidateUserId);
        owned.Value.Run.Id.Should().Be(runId);
        owned.Value.Run.RecruiterUserId.Should().Be(seed.RecruiterUserId);
        owned.Value.Run.RecruiterProfileId.Should().Be(seed.RecruiterProfileId);
        owned.Value.Run.CompanyId.Should().Be(seed.CompanyId);
        owned.Value.Run.JobId.Should().Be(seed.JobId);
    }

    [Task6PostgresFact]
    [Trait("Requirement", "R-06")]
    public async Task GetLatestCompletedAsync_SameCreatedAt_UsesIdDescendingTieBreak()
    {
        var seed = await _database.SeedGraphAsync();
        var lowerId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var higherId = Guid.Parse("10000000-0000-0000-0000-000000000002");
        await using var context = _database.CreateContext();
        var repository = MatchingScanRepositoryFactory.Recruiter(context);
        await repository.CreatePendingAsync(CreateRun(seed, At(0), lowerId), CancellationToken.None);
        await repository.CreatePendingAsync(CreateRun(seed, At(0), higherId), CancellationToken.None);
        (await repository.TryStartAsync(lowerId, At(1), CancellationToken.None)).Should().BeTrue();
        (await repository.TryStartAsync(higherId, At(1), CancellationToken.None)).Should().BeTrue();
        await repository.CompleteAsync(lowerId, [], At(3), CancellationToken.None);
        await repository.CompleteAsync(higherId, [], At(2), CancellationToken.None);

        var latest = await repository.GetLatestCompletedAsync(
            seed.RecruiterUserId,
            seed.CompanyId,
            seed.JobId,
            CancellationToken.None);

        latest!.Id.Should().Be(higherId);
    }

    [Task6PostgresFact]
    [Trait("Requirement", "R-05")]
    public async Task TryStartAsync_CompletedOrFailed_ReturnsFalseWithoutMutatingTerminalRuns()
    {
        var seed = await _database.SeedGraphAsync();
        var completedRunId = await CreateProcessingRunAsync(seed, At(0));
        var completedResult = CreateResult(
            completedRunId,
            seed.CvId,
            seed.CandidateUserId,
            1,
            Guid.Parse("70000000-0000-0000-0000-000000000021"));
        Guid failedRunId;
        await using (var setupContext = _database.CreateContext())
        {
            var setupRepository = MatchingScanRepositoryFactory.Recruiter(setupContext);
            await setupRepository.CompleteAsync(
                completedRunId,
                [completedResult],
                At(2),
                CancellationToken.None);
            failedRunId = (await setupRepository.CreatePendingAsync(CreateRun(seed, At(1)), CancellationToken.None)).Id;
            await setupRepository.FailAsync(
                failedRunId,
                "SYNTHETIC_FAILURE",
                "synthetic failure",
                At(2),
                CancellationToken.None);
        }

        RecruiterCvScanRun beforeCompletedRun;
        RecruiterCvScanRun beforeFailedRun;
        IReadOnlyList<RecruiterCvScanResult> beforeCompletedResults;
        IReadOnlyList<RecruiterCvScanResult> beforeFailedResults;
        await using (var beforeContext = _database.CreateContext())
        {
            beforeCompletedRun = await beforeContext.RecruiterCvScanRuns.AsNoTracking()
                .SingleAsync(run => run.Id == completedRunId);
            beforeFailedRun = await beforeContext.RecruiterCvScanRuns.AsNoTracking()
                .SingleAsync(run => run.Id == failedRunId);
            beforeCompletedResults = await beforeContext.RecruiterCvScanResults.AsNoTracking()
                .Where(result => result.RunId == completedRunId)
                .OrderBy(result => result.Id)
                .ToListAsync();
            beforeFailedResults = await beforeContext.RecruiterCvScanResults.AsNoTracking()
                .Where(result => result.RunId == failedRunId)
                .OrderBy(result => result.Id)
                .ToListAsync();
        }
        beforeCompletedResults.Should().ContainSingle().Which.Should().BeEquivalentTo(completedResult);
        beforeFailedResults.Should().BeEmpty();

        await using (var attemptContext = _database.CreateContext())
        {
            var attemptRepository = MatchingScanRepositoryFactory.Recruiter(attemptContext);
            (await attemptRepository.TryStartAsync(completedRunId, At(3), CancellationToken.None)).Should().BeFalse();
            (await attemptRepository.TryStartAsync(failedRunId, At(3), CancellationToken.None)).Should().BeFalse();
        }

        await using var verifyContext = _database.CreateContext();
        var afterCompletedRun = await verifyContext.RecruiterCvScanRuns.AsNoTracking()
            .SingleAsync(run => run.Id == completedRunId);
        var afterFailedRun = await verifyContext.RecruiterCvScanRuns.AsNoTracking()
            .SingleAsync(run => run.Id == failedRunId);
        var afterCompletedResults = await verifyContext.RecruiterCvScanResults.AsNoTracking()
            .Where(result => result.RunId == completedRunId)
            .OrderBy(result => result.Id)
            .ToListAsync();
        var afterFailedResults = await verifyContext.RecruiterCvScanResults.AsNoTracking()
            .Where(result => result.RunId == failedRunId)
            .OrderBy(result => result.Id)
            .ToListAsync();
        afterCompletedRun.Should().BeEquivalentTo(beforeCompletedRun);
        afterFailedRun.Should().BeEquivalentTo(beforeFailedRun);
        afterCompletedResults.Should().BeEquivalentTo(
            beforeCompletedResults,
            options => options.WithStrictOrdering());
        afterFailedResults.Should().BeEmpty();
    }

    [Task6PostgresFact]
    [Trait("Requirement", "R-05")]
    public async Task CompleteAsync_Pending_RejectsWithoutResults()
    {
        var seed = await _database.SeedGraphAsync();
        await using var context = _database.CreateContext();
        var repository = MatchingScanRepositoryFactory.Recruiter(context);
        var run = await repository.CreatePendingAsync(CreateRun(seed, At(0)), CancellationToken.None);
        var beforeRun = await context.RecruiterCvScanRuns.AsNoTracking().SingleAsync(value => value.Id == run.Id);

        var action = () => repository.CompleteAsync(
            run.Id,
            [CreateResult(run.Id, seed.CvId, seed.CandidateUserId, 1)],
            At(1),
            CancellationToken.None);

        await action.Should().ThrowAsync<Exception>();
        await using var verifyContext = _database.CreateContext();
        var afterRun = await verifyContext.RecruiterCvScanRuns.AsNoTracking().SingleAsync(value => value.Id == run.Id);
        afterRun.Should().BeEquivalentTo(beforeRun);
        (await verifyContext.RecruiterCvScanResults.CountAsync(result => result.RunId == run.Id)).Should().Be(0);
    }

    [Task6PostgresFact]
    [Trait("Requirement", "R-05")]
    public async Task FailAsync_PendingOrProcessing_TransitionsBothToFailed()
    {
        var seed = await _database.SeedGraphAsync();
        await using var context = _database.CreateContext();
        var repository = MatchingScanRepositoryFactory.Recruiter(context);
        var pending = await repository.CreatePendingAsync(CreateRun(seed, At(0)), CancellationToken.None);
        var processing = await repository.CreatePendingAsync(CreateRun(seed, At(1)), CancellationToken.None);
        (await repository.TryStartAsync(processing.Id, At(2), CancellationToken.None)).Should().BeTrue();

        await repository.FailAsync(pending.Id, "PENDING_FAILURE", "synthetic pending failure", At(3), CancellationToken.None);
        await repository.FailAsync(processing.Id, "PROCESSING_FAILURE", "synthetic processing failure", At(4), CancellationToken.None);

        await using var verifyContext = _database.CreateContext();
        var failed = await verifyContext.RecruiterCvScanRuns
            .Where(run => run.Id == pending.Id || run.Id == processing.Id)
            .OrderBy(run => run.CreatedAt)
            .ToListAsync();
        failed.Select(run => run.Status).Should().OnlyContain(status => status == MatchingScanRunStatus.Failed);
        failed[0].CompletedAt.Should().Be(At(3));
        failed[1].CompletedAt.Should().Be(At(4));
    }

    [Task6PostgresFact]
    [Trait("Requirement", "R-05")]
    public async Task FailAsync_AfterFailed_DoesNotMutateTerminalRun()
    {
        var seed = await _database.SeedGraphAsync();
        await using var context = _database.CreateContext();
        var repository = MatchingScanRepositoryFactory.Recruiter(context);
        var run = await repository.CreatePendingAsync(CreateRun(seed, At(0)), CancellationToken.None);
        await repository.FailAsync(run.Id, "FIRST_FAILURE", "first synthetic failure", At(1), CancellationToken.None);

        RecruiterCvScanRun beforeRun;
        await using (var beforeContext = _database.CreateContext())
        {
            beforeRun = await beforeContext.RecruiterCvScanRuns.AsNoTracking().SingleAsync(value => value.Id == run.Id);
        }

        await repository.FailAsync(run.Id, "SECOND_FAILURE", "second synthetic failure", At(2), CancellationToken.None);

        await using var verifyContext = _database.CreateContext();
        var afterRun = await verifyContext.RecruiterCvScanRuns.AsNoTracking().SingleAsync(value => value.Id == run.Id);
        afterRun.Should().BeEquivalentTo(beforeRun);
    }

    [Task6PostgresFact]
    [Trait("Requirement", "R-05")]
    public async Task CompleteAsync_AfterCompleted_DoesNotDuplicateResults()
    {
        var seed = await _database.SeedGraphAsync();
        var runId = await CreateProcessingRunAsync(seed, At(0));
        await using var context = _database.CreateContext();
        var repository = MatchingScanRepositoryFactory.Recruiter(context);
        var originalResult = CreateResult(
            runId,
            seed.CvId,
            seed.CandidateUserId,
            1,
            Guid.Parse("50000000-0000-0000-0000-000000000001"));
        await repository.CompleteAsync(
            runId,
            [originalResult],
            At(1),
            CancellationToken.None);

        RecruiterCvScanRun beforeRun;
        IReadOnlyList<RecruiterCvScanResult> beforeResults;
        await using (var beforeContext = _database.CreateContext())
        {
            beforeRun = await beforeContext.RecruiterCvScanRuns.AsNoTracking().SingleAsync(run => run.Id == runId);
            beforeResults = await beforeContext.RecruiterCvScanResults.AsNoTracking()
                .Where(result => result.RunId == runId)
                .OrderBy(result => result.Id)
                .ToListAsync();
        }

        var action = () => repository.CompleteAsync(
            runId,
            [CreateResult(runId, seed.OtherCvId, seed.OtherCandidateUserId, 2)],
            At(2),
            CancellationToken.None);

        await action.Should().ThrowAsync<Exception>();
        await using var verifyContext = _database.CreateContext();
        var afterRun = await verifyContext.RecruiterCvScanRuns.AsNoTracking().SingleAsync(run => run.Id == runId);
        var afterResults = await verifyContext.RecruiterCvScanResults.AsNoTracking()
            .Where(result => result.RunId == runId)
            .OrderBy(result => result.Id)
            .ToListAsync();
        afterRun.Should().BeEquivalentTo(beforeRun);
        afterResults.Should().BeEquivalentTo(beforeResults, options => options.WithStrictOrdering());
    }

    [Task6PostgresFact]
    [Trait("Requirement", "R-05")]
    public async Task FailAsync_LongOperationalMetadata_PersistsBoundedValues()
    {
        var seed = await _database.SeedGraphAsync();
        await using var context = _database.CreateContext();
        var repository = MatchingScanRepositoryFactory.Recruiter(context);
        var run = await repository.CreatePendingAsync(CreateRun(seed, At(0)), CancellationToken.None);

        await repository.FailAsync(
            run.Id,
            new string('E', 200),
            new string('M', 1200),
            At(1),
            CancellationToken.None);

        await using var verifyContext = _database.CreateContext();
        var persisted = await verifyContext.RecruiterCvScanRuns.SingleAsync(value => value.Id == run.Id);
        persisted.ErrorCode.Should().Be(new string('E', 128));
        persisted.ErrorMessage.Should().Be(new string('M', 1000));
    }

    [Task6PostgresFact]
    [Trait("Requirement", "R-05")]
    public async Task ConcurrentCompleteAndFail_ProducesOneImmutableTerminalState()
    {
        var seed = await _database.SeedGraphAsync();
        var runId = await CreateProcessingRunAsync(seed, At(0));
        var updateBarrier = new MatchingScanUpdateBarrier("recruiter_cv_scan_runs");
        await using var completeContext = _database.CreateContext(updateBarrier.CreateParticipant());
        await using var failContext = _database.CreateContext(updateBarrier.CreateParticipant());
        var completeRepository = MatchingScanRepositoryFactory.Recruiter(completeContext);
        var failRepository = MatchingScanRepositoryFactory.Recruiter(failContext);
        var completionResult = CreateResult(
            runId,
            seed.CvId,
            seed.CandidateUserId,
            1,
            Guid.Parse("50000000-0000-0000-0000-000000000002"));
        var complete = Task.Run(async () =>
        {
            return await CaptureAsync(() => completeRepository.CompleteAsync(
                runId,
                [completionResult],
                At(2),
                CancellationToken.None));
        });
        var fail = Task.Run(async () =>
        {
            return await CaptureAsync(() => failRepository.FailAsync(
                runId,
                "SYNTHETIC_FAILURE",
                "synthetic failure",
                At(3),
                CancellationToken.None));
        });

        Exception? coordinationFailure = null;
        var bothReachedBeforeRelease = false;
        try
        {
            await updateBarrier.WaitForBothParticipantsAsync();
            bothReachedBeforeRelease =
                updateBarrier.ArrivedParticipantCount == 2 && !updateBarrier.IsReleased;
        }
        catch (Exception exception)
        {
            coordinationFailure = exception;
        }
        finally
        {
            updateBarrier.Release();
        }

        await Task.WhenAll(complete, fail)
            .WaitAsync(MatchingScanUpdateBarrier.RaceCompletionTimeout);
        coordinationFailure.Should().BeNull("both PostgreSQL UPDATE participants must reach the exact-table barrier");
        bothReachedBeforeRelease.Should().BeTrue();
        fail.Result.Should().BeNull();

        await using var verifyContext = _database.CreateContext();
        var terminalRun = await verifyContext.RecruiterCvScanRuns.AsNoTracking().SingleAsync(run => run.Id == runId);
        var terminalResults = await verifyContext.RecruiterCvScanResults.AsNoTracking()
            .Where(result => result.RunId == runId)
            .OrderBy(result => result.Id)
            .ToListAsync();
        if (terminalRun.Status == MatchingScanRunStatus.Completed)
        {
            complete.Result.Should().BeNull();
            terminalResults.Should().ContainSingle().Which.Should().BeEquivalentTo(completionResult);
            terminalRun.ErrorCode.Should().BeNull();
            terminalRun.ErrorMessage.Should().BeNull();
        }
        else
        {
            terminalRun.Status.Should().Be(MatchingScanRunStatus.Failed);
            complete.Result.Should().NotBeNull();
            terminalResults.Should().BeEmpty();
            terminalRun.ErrorCode.Should().Be("SYNTHETIC_FAILURE");
            terminalRun.ErrorMessage.Should().Be("synthetic failure");
        }

        await using (var lateFailContext = _database.CreateContext())
        {
            var lateFailRepository = MatchingScanRepositoryFactory.Recruiter(lateFailContext);
            await lateFailRepository.FailAsync(
                runId,
                "LATE_FAILURE",
                "late synthetic failure",
                At(4),
                CancellationToken.None);
        }

        await using (var lateCompleteContext = _database.CreateContext())
        {
            var lateCompleteRepository = MatchingScanRepositoryFactory.Recruiter(lateCompleteContext);
            (await CaptureAsync(() => lateCompleteRepository.CompleteAsync(
                    runId,
                    [],
                    At(5),
                    CancellationToken.None)))
                .Should().NotBeNull();
        }

        verifyContext.ChangeTracker.Clear();
        var afterRun = await verifyContext.RecruiterCvScanRuns.AsNoTracking().SingleAsync(run => run.Id == runId);
        var afterResults = await verifyContext.RecruiterCvScanResults.AsNoTracking()
            .Where(result => result.RunId == runId)
            .OrderBy(result => result.Id)
            .ToListAsync();
        afterRun.Should().BeEquivalentTo(terminalRun);
        afterResults.Should().BeEquivalentTo(terminalResults, options => options.WithStrictOrdering());
    }

    private async Task AssertSuccessfulCompletionAsync(Guid firstResultId, Guid secondResultId)
    {
        var seed = await _database.SeedGraphAsync();
        var runId = await CreateProcessingRunAsync(seed, At(0));
        await using var context = _database.CreateContext();
        var repository = MatchingScanRepositoryFactory.Recruiter(context);
        var results = new[]
        {
            CreateResult(runId, seed.CvId, seed.CandidateUserId, 1, firstResultId),
            CreateResult(runId, seed.OtherCvId, seed.OtherCandidateUserId, 2, secondResultId)
        };

        await repository.CompleteAsync(runId, results, At(2), CancellationToken.None);

        await using var verifyContext = _database.CreateContext();
        var persisted = await verifyContext.RecruiterCvScanRuns.SingleAsync(run => run.Id == runId);
        persisted.Status.Should().Be(MatchingScanRunStatus.Completed);
        persisted.CompletedAt.Should().Be(At(2));
        persisted.ErrorCode.Should().BeNull();
        persisted.ErrorMessage.Should().BeNull();
        var persistedResults = await verifyContext.RecruiterCvScanResults.AsNoTracking()
                .Where(result => result.RunId == runId)
                .OrderBy(result => result.Rank)
                .ThenBy(result => result.Id)
                .ToListAsync();
        persistedResults.Should().BeEquivalentTo(results, options => options.WithStrictOrdering());
    }

    private async Task<Guid> CreateProcessingRunAsync(MatchingScanSeed seed, DateTime createdAt)
    {
        await using var context = _database.CreateContext();
        var repository = MatchingScanRepositoryFactory.Recruiter(context);
        var run = await repository.CreatePendingAsync(CreateRun(seed, createdAt), CancellationToken.None);
        (await repository.TryStartAsync(run.Id, createdAt.AddSeconds(1), CancellationToken.None)).Should().BeTrue();
        return run.Id;
    }

    private static RecruiterCvScanRun CreateRun(
        MatchingScanSeed seed,
        DateTime createdAt,
        Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        RecruiterUserId = seed.RecruiterUserId,
        RecruiterProfileId = seed.RecruiterProfileId,
        CompanyId = seed.CompanyId,
        JobId = seed.JobId,
        JobTitleSnapshot = "Synthetic job",
        Status = MatchingScanRunStatus.Pending,
        CreatedAt = createdAt
    };

    private static RecruiterCvScanResult CreateResult(
        Guid runId,
        Guid cvId,
        Guid candidateUserId,
        int rank,
        Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        RunId = runId,
        CvId = cvId,
        CandidateUserId = candidateUserId,
        MatchScore = 75m - rank,
        MatchDetails = $"synthetic recruiter match details {rank}",
        CvAnalysisQuality = rank % 2 == 0 ? CvAnalysisQuality.PARTIAL : CvAnalysisQuality.COMPLETE,
        CvAnalysisCoverageJson = $"{{\"covered\": [\"synthetic-skill-{rank}\"]}}",
        CvAnalysisDiagnosticsJson = $"{{\"warnings\": [\"synthetic-warning-{rank}\"]}}",
        Rank = rank
    };

    private static async Task<Exception?> CaptureAsync(Func<Task> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private static DateTime At(int minute) =>
        new DateTime(2026, 8, 16, 4, 0, 0, DateTimeKind.Utc).AddMinutes(minute);
}
