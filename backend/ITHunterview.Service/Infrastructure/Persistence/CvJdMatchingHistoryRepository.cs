using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence;

public sealed class CvJdMatchingHistoryRepository : ICvJdMatchingHistoryRepository
{
    private readonly ITHunterviewContext _context;

    public CvJdMatchingHistoryRepository(ITHunterviewContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<HideMatchHistoryResult> HideAsync(
        Guid jobId,
        Guid userId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        if (IsInMemoryProvider())
        {
            var inMemoryJob = await _context.CvJobMatchScores
                .SingleOrDefaultAsync(x => x.Id == jobId && x.UserId == userId, cancellationToken);
            if (inMemoryJob is null)
                return HideMatchHistoryResult.NotFound;

            if (inMemoryJob.HistoryHiddenAt.HasValue)
                return HideMatchHistoryResult.Hidden;

            if (!IsTerminal(inMemoryJob.Status))
                return HideMatchHistoryResult.ActiveJob;

            inMemoryJob.HistoryHiddenAt = utcNow;
            inMemoryJob.UpdatedAt = utcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return HideMatchHistoryResult.Hidden;
        }

        var affected = await _context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE cv_job_match_scores
            SET history_hidden_at = {utcNow}, updated_at = {utcNow}
            WHERE id = {jobId}
              AND user_id = {userId}
              AND status IN ('Completed', 'Failed')
              AND history_hidden_at IS NULL
            """, cancellationToken);
        if (affected == 1)
            return HideMatchHistoryResult.Hidden;

        var state = await _context.CvJobMatchScores
            .AsNoTracking()
            .Where(x => x.Id == jobId && x.UserId == userId)
            .Select(x => new { x.Status, x.HistoryHiddenAt })
            .SingleOrDefaultAsync(cancellationToken);
        if (state is null)
            return HideMatchHistoryResult.NotFound;
        if (state.HistoryHiddenAt.HasValue || IsTerminal(state.Status))
            return HideMatchHistoryResult.Hidden;
        return HideMatchHistoryResult.ActiveJob;
    }

    private static bool IsTerminal(string? status)
        => string.Equals(status, "Completed", StringComparison.Ordinal)
           || string.Equals(status, "Failed", StringComparison.Ordinal);

    private bool IsInMemoryProvider()
        => string.Equals(_context.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal);
}
