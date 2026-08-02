using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Cv.Matching;
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

    public async Task<CvJobMatchScores?> GetFailedJobForRetryForUpdateAsync(
        Guid userId,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        if (IsInMemoryProvider())
        {
            return await _context.CvJobMatchScores
                .FirstOrDefaultAsync(x => x.Id == jobId
                                          && x.UserId == userId
                                          && x.MatchType == "AI"
                                          && x.Status == "Failed",
                    cancellationToken);
        }

        return await _context.CvJobMatchScores
            .FromSqlInterpolated($"SELECT * FROM cv_job_match_scores WHERE id = {jobId} AND user_id = {userId} AND match_type = 'AI' AND status = 'Failed' FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ClaimedMatchingJob>> ClaimRunnableJobsAsync(
        int limit,
        string workerId,
        DateTime utcNow,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 4);
        var leaseTokenByJob = new List<ClaimedMatchingJob>(limit);
        var leaseExpiresAt = utcNow.Add(leaseDuration);

        if (IsInMemoryProvider())
        {
            var candidates = await _context.CvJobMatchScores
                .Where(x => x.MatchType == "AI"
                            && (x.Status == "Pending" || x.Status == "RetryScheduled")
                            && (!x.NextAttemptAt.HasValue || x.NextAttemptAt <= utcNow))
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .Take(limit)
                .ToListAsync(cancellationToken);
            foreach (var job in candidates)
            {
                var token = Guid.NewGuid();
                job.Status = "Processing";
                job.AttemptCount++;
                job.StartedAt ??= utcNow;
                job.UpdatedAt = utcNow;
                job.LeaseOwner = workerId;
                job.LeaseToken = token;
                job.LeaseExpiresAt = leaseExpiresAt;
                job.LastHeartbeatAt = utcNow;
                leaseTokenByJob.Add(new ClaimedMatchingJob(job.Id, token));
            }
            await _context.SaveChangesAsync(cancellationToken);
            return leaseTokenByJob;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var rows = await _context.CvJobMatchScores
            .FromSqlInterpolated($"SELECT * FROM cv_job_match_scores WHERE match_type = 'AI' AND status IN ('Pending', 'RetryScheduled') AND (next_attempt_at IS NULL OR next_attempt_at <= {utcNow}) ORDER BY created_at, id LIMIT {limit} FOR UPDATE SKIP LOCKED")
            .ToListAsync(cancellationToken);
        foreach (var job in rows)
        {
            var token = Guid.NewGuid();
            job.Status = "Processing";
            job.AttemptCount++;
            job.StartedAt ??= utcNow;
            job.UpdatedAt = utcNow;
            job.LeaseOwner = workerId;
            job.LeaseToken = token;
            job.LeaseExpiresAt = leaseExpiresAt;
            job.LastHeartbeatAt = utcNow;
            leaseTokenByJob.Add(new ClaimedMatchingJob(job.Id, token));
        }
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return leaseTokenByJob;
    }

    public async Task<CvJobMatchScores?> GetClaimedJobAsync(
        Guid jobId,
        string workerId,
        Guid leaseToken,
        CancellationToken cancellationToken = default)
    {
        return await _context.CvJobMatchScores
            .Where(x => x.Id == jobId
                        && x.MatchType == "AI"
                        && x.Status == "Processing"
                        && x.LeaseOwner == workerId
                        && x.LeaseToken == leaseToken)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> CompleteAsync(
        Guid jobId,
        string workerId,
        Guid leaseToken,
        decimal score,
        string matchDetails,
        string? sfiaExtractResult,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        if (IsInMemoryProvider())
        {
            var job = await GetClaimedJobAsync(jobId, workerId, leaseToken, cancellationToken);
            if (job == null) return false;
            job.Status = "Completed";
            job.MatchScore = score;
            job.MatchDetails = matchDetails;
            job.SfiaExtractResult = sfiaExtractResult;
            job.CompletedAt = utcNow;
            job.UpdatedAt = utcNow;
            job.ErrorCode = null;
            job.ErrorMessage = null;
            job.LeaseOwner = null;
            job.LeaseToken = null;
            job.LeaseExpiresAt = null;
            job.LastHeartbeatAt = null;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        var affected = await _context.Database.ExecuteSqlInterpolatedAsync($"UPDATE cv_job_match_scores SET status = 'Completed', match_score = {score}, match_details = {matchDetails}, sfia_extract_result = {sfiaExtractResult}, completed_at = {utcNow}, updated_at = {utcNow}, error_code = NULL, error_message = NULL, lease_owner = NULL, lease_token = NULL, lease_expires_at = NULL, last_heartbeat_at = NULL WHERE id = {jobId} AND match_type = 'AI' AND status = 'Processing' AND lease_owner = {workerId} AND lease_token = {leaseToken}", cancellationToken);
        return affected == 1;
    }

    public async Task<bool> ScheduleRetryAsync(
        Guid jobId,
        string workerId,
        Guid leaseToken,
        string errorCode,
        DateTime nextAttemptAt,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        if (IsInMemoryProvider())
        {
            var job = await GetClaimedJobAsync(jobId, workerId, leaseToken, cancellationToken);
            if (job == null) return false;
            ApplyRetry(job, errorCode, nextAttemptAt, utcNow);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        var affected = await _context.Database.ExecuteSqlInterpolatedAsync($"UPDATE cv_job_match_scores SET status = 'RetryScheduled', next_attempt_at = {nextAttemptAt}, updated_at = {utcNow}, error_code = {errorCode}, error_message = {errorCode}, lease_owner = NULL, lease_token = NULL, lease_expires_at = NULL, last_heartbeat_at = NULL WHERE id = {jobId} AND match_type = 'AI' AND status = 'Processing' AND lease_owner = {workerId} AND lease_token = {leaseToken}", cancellationToken);
        return affected == 1;
    }

    public async Task<bool> MarkFailedAsync(
        Guid jobId,
        string workerId,
        Guid leaseToken,
        string errorCode,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        if (IsInMemoryProvider())
        {
            var job = await GetClaimedJobAsync(jobId, workerId, leaseToken, cancellationToken);
            if (job == null) return false;
            ApplyFailed(job, errorCode, utcNow);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        var affected = await _context.Database.ExecuteSqlInterpolatedAsync($"UPDATE cv_job_match_scores SET status = 'Failed', completed_at = {utcNow}, updated_at = {utcNow}, error_code = {errorCode}, error_message = {errorCode}, lease_owner = NULL, lease_token = NULL, lease_expires_at = NULL, last_heartbeat_at = NULL WHERE id = {jobId} AND match_type = 'AI' AND status = 'Processing' AND lease_owner = {workerId} AND lease_token = {leaseToken}", cancellationToken);
        return affected == 1;
    }

    public async Task<IReadOnlyList<CvJobMatchScores>> GetExpiredLeasesForUpdateAsync(
        DateTime utcNow,
        int limit,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 50);
        if (IsInMemoryProvider())
        {
            return await _context.CvJobMatchScores
                .Where(x => x.MatchType == "AI"
                            && x.Status == "Processing"
                            && x.LeaseExpiresAt.HasValue
                            && x.LeaseExpiresAt <= utcNow)
                .OrderBy(x => x.LeaseExpiresAt)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        return await _context.CvJobMatchScores
            .FromSqlInterpolated($"SELECT * FROM cv_job_match_scores WHERE match_type = 'AI' AND status = 'Processing' AND lease_expires_at <= {utcNow} ORDER BY lease_expires_at LIMIT {limit} FOR UPDATE SKIP LOCKED")
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ScheduleRecoveredRetryAsync(
        Guid jobId,
        string errorCode,
        DateTime nextAttemptAt,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        if (IsInMemoryProvider())
        {
            var job = await _context.CvJobMatchScores.FirstOrDefaultAsync(x => x.Id == jobId && x.MatchType == "AI" && x.Status == "Processing", cancellationToken);
            if (job == null) return false;
            ApplyRetry(job, errorCode, nextAttemptAt, utcNow);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        var affected = await _context.Database.ExecuteSqlInterpolatedAsync($"UPDATE cv_job_match_scores SET status = 'RetryScheduled', next_attempt_at = {nextAttemptAt}, updated_at = {utcNow}, error_code = {errorCode}, error_message = {errorCode}, lease_owner = NULL, lease_token = NULL, lease_expires_at = NULL, last_heartbeat_at = NULL WHERE id = {jobId} AND match_type = 'AI' AND status = 'Processing' AND lease_expires_at <= {utcNow}", cancellationToken);
        return affected == 1;
    }

    public async Task<bool> MarkRecoveredFailedAsync(
        Guid jobId,
        string errorCode,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        if (IsInMemoryProvider())
        {
            var job = await _context.CvJobMatchScores.FirstOrDefaultAsync(x => x.Id == jobId && x.MatchType == "AI" && x.Status == "Processing", cancellationToken);
            if (job == null) return false;
            ApplyFailed(job, errorCode, utcNow);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        var affected = await _context.Database.ExecuteSqlInterpolatedAsync($"UPDATE cv_job_match_scores SET status = 'Failed', completed_at = {utcNow}, updated_at = {utcNow}, error_code = {errorCode}, error_message = {errorCode}, lease_owner = NULL, lease_token = NULL, lease_expires_at = NULL, last_heartbeat_at = NULL WHERE id = {jobId} AND match_type = 'AI' AND status = 'Processing' AND lease_expires_at <= {utcNow}", cancellationToken);
        return affected == 1;
    }

    private static void ApplyRetry(CvJobMatchScores job, string errorCode, DateTime nextAttemptAt, DateTime utcNow)
    {
        job.Status = "RetryScheduled";
        job.NextAttemptAt = nextAttemptAt;
        job.UpdatedAt = utcNow;
        job.ErrorCode = errorCode;
        job.ErrorMessage = errorCode;
        job.LeaseOwner = null;
        job.LeaseToken = null;
        job.LeaseExpiresAt = null;
        job.LastHeartbeatAt = null;
    }

    private static void ApplyFailed(CvJobMatchScores job, string errorCode, DateTime utcNow)
    {
        job.Status = "Failed";
        job.CompletedAt = utcNow;
        job.UpdatedAt = utcNow;
        job.ErrorCode = errorCode;
        job.ErrorMessage = errorCode;
        job.LeaseOwner = null;
        job.LeaseToken = null;
        job.LeaseExpiresAt = null;
        job.LastHeartbeatAt = null;
    }

    private bool IsInMemoryProvider()
        => string.Equals(_context.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal);
}
