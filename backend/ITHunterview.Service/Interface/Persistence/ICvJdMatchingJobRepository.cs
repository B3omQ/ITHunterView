using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.Persistence;

public interface ICvJdMatchingJobRepository
{
    Task<CvJobMatchScores?> GetByIdempotencyKeyForUpdateAsync(
        Guid userId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<CvJobMatchScores?> GetFailedJobForRetryForUpdateAsync(
        Guid userId,
        Guid jobId,
        CancellationToken cancellationToken = default);

    void AddPending(CvJobMatchScores job);

    Task<IReadOnlyList<ClaimedMatchingJob>> ClaimRunnableJobsAsync(
        int limit,
        string workerId,
        DateTime utcNow,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    Task<CvJobMatchScores?> GetClaimedJobAsync(
        Guid jobId,
        string workerId,
        Guid leaseToken,
        CancellationToken cancellationToken = default);

    Task<bool> CompleteAsync(
        Guid jobId,
        string workerId,
        Guid leaseToken,
        decimal score,
        string matchDetails,
        string? sfiaExtractResult,
        CvAnalysisQuality? cvAnalysisQuality,
        string? cvAnalysisCoverageJson,
        string? cvAnalysisDiagnosticsJson,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task<bool> ScheduleRetryAsync(
        Guid jobId,
        string workerId,
        Guid leaseToken,
        string errorCode,
        DateTime nextAttemptAt,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task<bool> MarkFailedAsync(
        Guid jobId,
        string workerId,
        Guid leaseToken,
        string errorCode,
        CvAnalysisQuality? cvAnalysisQuality,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CvJobMatchScores>> GetExpiredLeasesForUpdateAsync(
        DateTime utcNow,
        int limit,
        CancellationToken cancellationToken = default);

    Task<bool> ScheduleRecoveredRetryAsync(
        Guid jobId,
        string errorCode,
        DateTime nextAttemptAt,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task<bool> MarkRecoveredFailedAsync(
        Guid jobId,
        string errorCode,
        DateTime utcNow,
        CancellationToken cancellationToken = default);
}
