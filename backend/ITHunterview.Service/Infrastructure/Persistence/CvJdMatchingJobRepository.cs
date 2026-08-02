using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence;

/// <summary>
/// Durable AI matching-job persistence. The caller owns the transaction so
/// idempotency, snapshot, job and billing mutations can commit together.
/// </summary>
public sealed class CvJdMatchingJobRepository : ICvJdMatchingJobRepository
{
    private readonly ITHunterviewContext _context;

    public CvJdMatchingJobRepository(ITHunterviewContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<CvJobMatchScores?> GetByIdempotencyKeyForUpdateAsync(
        Guid userId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (IsInMemoryProvider())
        {
            return await _context.CvJobMatchScores
                .Where(x => x.MatchType == "AI" && x.UserId == userId && x.IdempotencyKey == idempotencyKey)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return await _context.CvJobMatchScores
            .FromSqlInterpolated($"SELECT * FROM cv_job_match_scores WHERE match_type = 'AI' AND user_id = {userId} AND idempotency_key = {idempotencyKey} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void AddPending(CvJobMatchScores job)
    {
        _context.CvJobMatchScores.Add(job);
    }

    private bool IsInMemoryProvider()
        => string.Equals(_context.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal);
}
