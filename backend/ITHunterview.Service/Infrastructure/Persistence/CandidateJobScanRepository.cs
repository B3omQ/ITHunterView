using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ITHunterview.Service.Infrastructure.Persistence;

public sealed class CandidateJobScanRepository : ICandidateJobScanRepository
{
    private const int MaximumErrorCodeLength = 128;
    private const int MaximumErrorMessageLength = 1000;

    private readonly ITHunterviewContext _context;

    public CandidateJobScanRepository(ITHunterviewContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task<CandidateJobScanRun> CreatePendingAsync(
        CandidateJobScanRun run,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(run);
        EnsureCleanPendingRun(run);

        await _context.CandidateJobScanRuns.AddAsync(run, ct);
        await _context.SaveChangesAsync(ct);
        return run;
    }

    public async Task<bool> TryStartAsync(Guid runId, DateTime startedAt, CancellationToken ct)
    {
        var affected = await _context.CandidateJobScanRuns
            .Where(run => run.Id == runId && run.Status == MatchingScanRunStatus.Pending)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(run => run.Status, MatchingScanRunStatus.Processing)
                    .SetProperty(run => run.StartedAt, startedAt),
                ct);

        return affected == 1;
    }

    public async Task CompleteAsync(
        Guid runId,
        IReadOnlyCollection<CandidateJobScanResult> results,
        DateTime completedAt,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(results);
        EnsureResultsBelongToRun(runId, results);

        var transaction = await BeginOwnedTransactionAsync(ct);
        try
        {
            var affected = await _context.CandidateJobScanRuns
                .Where(run => run.Id == runId && run.Status == MatchingScanRunStatus.Processing)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(run => run.Status, MatchingScanRunStatus.Completed)
                        .SetProperty(run => run.CompletedAt, completedAt)
                        .SetProperty(run => run.ErrorCode, (string?)null)
                        .SetProperty(run => run.ErrorMessage, (string?)null),
                    ct);

            if (affected != 1)
            {
                throw new InvalidOperationException("Only a processing candidate scan run can be completed.");
            }

            _context.CandidateJobScanResults.AddRange(results);
            await _context.SaveChangesAsync(ct);

            if (transaction is not null)
            {
                await transaction.CommitAsync(ct);
            }
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }

            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    public async Task FailAsync(
        Guid runId,
        string errorCode,
        string errorMessage,
        DateTime failedAt,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(errorCode);
        ArgumentNullException.ThrowIfNull(errorMessage);

        var boundedErrorCode = Bound(errorCode, MaximumErrorCodeLength);
        var boundedErrorMessage = Bound(errorMessage, MaximumErrorMessageLength);

        await _context.CandidateJobScanRuns
            .Where(run =>
                run.Id == runId &&
                (run.Status == MatchingScanRunStatus.Pending ||
                 run.Status == MatchingScanRunStatus.Processing))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(run => run.Status, MatchingScanRunStatus.Failed)
                    .SetProperty(run => run.CompletedAt, failedAt)
                    .SetProperty(run => run.ErrorCode, boundedErrorCode)
                    .SetProperty(run => run.ErrorMessage, boundedErrorMessage),
                ct);
    }

    public Task<CandidateJobScanRun?> GetPendingOrProcessingByIdAsync(
        Guid runId,
        CancellationToken ct) =>
        _context.CandidateJobScanRuns
            .AsNoTracking()
            .SingleOrDefaultAsync(
                run =>
                    run.Id == runId &&
                    (run.Status == MatchingScanRunStatus.Pending ||
                     run.Status == MatchingScanRunStatus.Processing),
                ct);

    public Task<CandidateJobScanRun?> GetLatestCompletedAsync(
        Guid candidateUserId,
        Guid cvId,
        CancellationToken ct) =>
        _context.CandidateJobScanRuns
            .AsNoTracking()
            .Where(run =>
                run.CandidateUserId == candidateUserId &&
                run.CvId == cvId &&
                run.Status == MatchingScanRunStatus.Completed)
            .OrderByDescending(run => run.CreatedAt)
            .ThenByDescending(run => run.Id)
            .FirstOrDefaultAsync(ct);

    public async Task<(IReadOnlyList<CandidateJobScanResult> Items, int TotalCount)> GetResultPageAsync(
        Guid runId,
        int skip,
        int take,
        CancellationToken ct)
    {
        var query = _context.CandidateJobScanResults
            .AsNoTracking()
            .Where(result => result.RunId == runId);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(result => result.Rank)
            .ThenBy(result => result.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    private async Task<IDbContextTransaction?> BeginOwnedTransactionAsync(CancellationToken ct)
    {
        if (_context.Database.CurrentTransaction is not null)
        {
            return null;
        }

        return await _context.Database.BeginTransactionAsync(ct);
    }

    private static void EnsureResultsBelongToRun(
        Guid runId,
        IEnumerable<CandidateJobScanResult> results)
    {
        if (results.Any(result => result.RunId != runId))
        {
            throw new ArgumentException("Every candidate scan result must belong to the completed run.", nameof(results));
        }
    }

    private static void EnsureCleanPendingRun(CandidateJobScanRun run)
    {
        if (run.Status != MatchingScanRunStatus.Pending ||
            run.StartedAt is not null ||
            run.CompletedAt is not null ||
            run.ErrorCode is not null ||
            run.ErrorMessage is not null)
        {
            throw new ArgumentException(
                "A new candidate scan run must have a clean Pending lifecycle state.",
                nameof(run));
        }
    }

    private static string Bound(string value, int maximumLength) =>
        value.Length <= maximumLength ? value : value[..maximumLength];
}
