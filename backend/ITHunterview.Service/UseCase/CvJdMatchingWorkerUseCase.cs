using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.Service.Matching;
using ITHunterview.Service.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ITHunterview.Service.UseCase;

public sealed class CvJdMatchingWorkerUseCase : ICvJdMatchingWorkerUseCase
{
    public static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(4);
    public static readonly TimeSpan AttemptTimeout = TimeSpan.FromMinutes(3);

    private readonly ITHunterviewContext _context;
    private readonly ICvJdMatchingJobRepository _jobRepository;
    private readonly ICvJdOneToOneMatchingProcessor _processor;
    private readonly ICandidateFeatureUsageUseCase _featureUsageUseCase;
    private readonly ILogger<CvJdMatchingWorkerUseCase> _logger;
    private readonly IMatchingSourceAnalysisPersistence? _sourceAnalysisPersistence;

    public CvJdMatchingWorkerUseCase(
        ITHunterviewContext context,
        ICvJdMatchingJobRepository jobRepository,
        ICvJdOneToOneMatchingProcessor processor,
        ICandidateFeatureUsageUseCase featureUsageUseCase,
        ILogger<CvJdMatchingWorkerUseCase> logger,
        IMatchingSourceAnalysisPersistence? sourceAnalysisPersistence = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        _featureUsageUseCase = featureUsageUseCase ?? throw new ArgumentNullException(nameof(featureUsageUseCase));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sourceAnalysisPersistence = sourceAnalysisPersistence;
    }

    public Task<IReadOnlyList<ClaimedMatchingJob>> ClaimRunnableJobsAsync(
        int limit,
        string workerId,
        DateTime utcNow,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
        => _jobRepository.ClaimRunnableJobsAsync(limit, workerId, utcNow, leaseDuration, cancellationToken);

    public async Task ProcessClaimedJobAsync(
        Guid jobId,
        string workerId,
        Guid leaseToken,
        CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetClaimedJobAsync(jobId, workerId, leaseToken, cancellationToken);
        if (job == null)
            return;

        MatchingInputSnapshotV1 snapshot;
        try
        {
            snapshot = MatchingInputSnapshotIntegrity.Deserialize(job.InputSnapshotJson!);
            if (!MatchingInputSnapshotIntegrity.IsValid(snapshot, job.InputHash))
            {
                throw new InvalidOperationException("SNAPSHOT_HASH_MISMATCH");
            }
        }
        catch (InvalidOperationException ex) when (ex.Message is "SNAPSHOT_INVALID" or "SNAPSHOT_HASH_MISMATCH")
        {
            await FailOrRetryAsync(job, workerId, leaseToken, new MatchingFailureClassification(ex.Message, false), cancellationToken);
            return;
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(AttemptTimeout);
        try
        {
            var result = await _processor.ExecuteAsync(job.Id, snapshot, timeoutCts.Token);
            var completed = await _jobRepository.CompleteAsync(
                job.Id,
                workerId,
                leaseToken,
                result.Score,
                result.MatchDetails,
                result.SfiaExtractResult,
                result.CvAnalysisQuality,
                CvAnalysisMetadataReader.SerializeCoverage(result.CvAnalysisCoverage),
                CvAnalysisMetadataReader.SerializeDiagnostics(result.CvAnalysisDiagnostics),
                DateTime.UtcNow,
                result.JdAnalysisQuality,
                JdAnalysisMetadataReader.SerializeCoverage(result.JdAnalysisCoverage),
                JdAnalysisMetadataReader.SerializeDiagnostics(result.JdAnalysisDiagnostics),
                cancellationToken);
            if (!completed)
                _logger.LogInformation("Matching lease lost before completion for job {JobId}.", job.Id);
            else if (_sourceAnalysisPersistence is not null)
            {
                if (result.CvPersistenceIntent is not null)
                    await TryPersistCvSourceAnalysisAsync(job.Id, result.CvPersistenceIntent, cancellationToken);
                if (result.JdPersistenceIntent is not null)
                    await TryPersistJdSourceAnalysisAsync(job.Id, result.JdPersistenceIntent, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await FailOrRetryAsync(job, workerId, leaseToken, new MatchingFailureClassification("AI_PROVIDER_TIMEOUT", true), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Host shutdown/caller cancellation must leave the lease for the
            // recovery loop; do not attempt a database transition with a
            // canceled token.
            throw;
        }
        catch (Exception ex)
        {
            var classification = MatchingFailureClassifier.Classify(ex);
            _logger.LogWarning("Matching job {JobId} failed with code {ErrorCode}; retryable={Retryable}.", job.Id, classification.ErrorCode, classification.Retryable);
            await FailOrRetryAsync(job, workerId, leaseToken, classification, cancellationToken);
        }
    }

    public async Task RecoverExpiredLeasesAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var transaction = IsInMemoryProvider()
            ? null
            : await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var expired = await _jobRepository.GetExpiredLeasesForUpdateAsync(utcNow, 50, cancellationToken);
            foreach (var job in expired)
            {
                if (job.AttemptCount < Math.Max(1, job.MaxAttempts))
                {
                    await _jobRepository.ScheduleRecoveredRetryAsync(
                        job.Id,
                        "LEASE_EXPIRED",
                        utcNow.Add(GetRetryDelay(job.AttemptCount)),
                        utcNow,
                        cancellationToken);
                }
                else
                {
                    var failed = await _jobRepository.MarkRecoveredFailedAsync(
                        job.Id,
                        "LEASE_EXPIRED",
                        utcNow,
                        cancellationToken);
                    if (failed)
                    {
                        await _featureUsageUseCase.RefundFeatureReservationAsync(
                            job.UserId,
                            job.Id,
                            "lease_expired",
                            cancellationToken);
                    }
                }
            }

            if (transaction != null)
                await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            if (transaction != null)
                await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (transaction != null)
                await transaction.DisposeAsync();
        }
    }

    private async Task FailOrRetryAsync(
        CvJobMatchScores job,
        string workerId,
        Guid leaseToken,
        MatchingFailureClassification classification,
        CancellationToken cancellationToken)
    {
        var canRetry = classification.Retryable && job.AttemptCount < Math.Max(1, job.MaxAttempts);
        if (canRetry)
        {
            await _jobRepository.ScheduleRetryAsync(
                job.Id,
                workerId,
                leaseToken,
                classification.ErrorCode,
                DateTime.UtcNow.Add(GetRetryDelay(job.AttemptCount)),
                DateTime.UtcNow,
                cancellationToken);
            return;
        }

        var transaction = IsInMemoryProvider()
            ? null
            : await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var failed = await _jobRepository.MarkFailedAsync(
                job.Id,
                workerId,
                leaseToken,
                classification.ErrorCode,
                classification.CvAnalysisQuality,
                DateTime.UtcNow,
                cancellationToken);
            if (failed)
            {
                await _featureUsageUseCase.RefundFeatureReservationAsync(
                    job.UserId,
                    job.Id,
                    classification.ErrorCode.ToLowerInvariant(),
                    cancellationToken);
            }

            if (transaction != null)
                await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            if (transaction != null)
                await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (transaction != null)
                await transaction.DisposeAsync();
        }
    }

    public static TimeSpan GetRetryDelay(int attemptCount)
        => attemptCount switch
        {
            <= 1 => TimeSpan.FromSeconds(10),
            2 => TimeSpan.FromSeconds(30),
            _ => TimeSpan.FromSeconds(120)
        };

    // Source-analysis caching is intentionally best-effort after the immutable
    // matching result is committed. A later source edit or a cache-write fault
    // must never turn a completed, billable match into a failed/refunded job.
    private async Task TryPersistCvSourceAnalysisAsync(
        Guid matchingJobId,
        CvAnalysisPersistenceIntent intent,
        CancellationToken cancellationToken)
    {
        try
        {
            var outcome = await _sourceAnalysisPersistence!.TryPersistCvAsync(intent, cancellationToken);
            _logger.LogInformation("Matching source CV persistence completed. MatchingJobId={MatchingJobId}; CvId={CvId}; Outcome={Outcome}",
                matchingJobId, intent.CvId, outcome);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Matching source CV persistence failed after match completion. MatchingJobId={MatchingJobId}; CvId={CvId}",
                matchingJobId, intent.CvId);
        }
    }

    private async Task TryPersistJdSourceAnalysisAsync(
        Guid matchingJobId,
        JdAnalysisPersistenceIntent intent,
        CancellationToken cancellationToken)
    {
        try
        {
            var outcome = await _sourceAnalysisPersistence!.TryPersistJdAsync(intent, cancellationToken);
            _logger.LogInformation("Matching source JD persistence completed. MatchingJobId={MatchingJobId}; JobId={JobId}; Outcome={Outcome}",
                matchingJobId, intent.JobId, outcome);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Matching source JD persistence failed after match completion. MatchingJobId={MatchingJobId}; JobId={JobId}",
                matchingJobId, intent.JobId);
        }
    }

    private bool IsInMemoryProvider()
        => string.Equals(_context.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal);
}
