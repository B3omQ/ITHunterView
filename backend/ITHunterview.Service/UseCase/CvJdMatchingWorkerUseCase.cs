using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
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
    private readonly StagedMatchingResultReader _stagedResultReader;

    public CvJdMatchingWorkerUseCase(
        ITHunterviewContext context,
        ICvJdMatchingJobRepository jobRepository,
        ICvJdOneToOneMatchingProcessor processor,
        ICandidateFeatureUsageUseCase featureUsageUseCase,
        ILogger<CvJdMatchingWorkerUseCase> logger,
        IMatchingSourceAnalysisPersistence? sourceAnalysisPersistence = null,
        StagedMatchingResultReader? stagedResultReader = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        _featureUsageUseCase = featureUsageUseCase ?? throw new ArgumentNullException(nameof(featureUsageUseCase));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sourceAnalysisPersistence = sourceAnalysisPersistence;
        _stagedResultReader = stagedResultReader ?? new StagedMatchingResultReader();
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

        if (string.Equals(
                job.ErrorCode,
                ICvJdMatchingJobRepository.ResultFinalizationPending,
                StringComparison.Ordinal))
        {
            await FinalizeAlreadyStagedAsync(job, workerId, leaseToken, cancellationToken);
            return;
        }

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
            var previousUserId =
                ITHunterview.Service.Utils.UserContext.CurrentUserId;

            ITHunterview.Service.Utils.UserContext.CurrentUserId = job.UserId;

            CvJdMatchingExecutionResult result;
            try
            {
                MatchingProgressCallback progressCallback =
                    (processingStage, stageCancellationToken) => ReportProcessingStageAsync(
                        job.Id,
                        workerId,
                        leaseToken,
                        processingStage,
                        stageCancellationToken,
                        cancellationToken);
                result = _processor is ICvJdOneToOneMatchingProgressProcessor progressProcessor
                    ? await progressProcessor.ExecuteWithProgressAsync(
                        job.Id,
                        snapshot,
                        progressCallback,
                        timeoutCts.Token)
                    : await _processor.ExecuteAsync(
                        job.Id,
                        snapshot,
                        timeoutCts.Token);
            }
            finally
            {
                ITHunterview.Service.Utils.UserContext.CurrentUserId =
                    previousUserId;
            }
            await ReportProcessingStageAsync(
                job.Id,
                workerId,
                leaseToken,
                MatchingProcessingStages.Finalizing,
                timeoutCts.Token,
                cancellationToken);
            var stagedResult = _stagedResultReader.ReadOrCreateSafeFallback(
                            result.Score,
                            result.MatchDetails);
            var staged = await _jobRepository.StageTerminalResultAsync(
                job.Id,
                workerId,
                leaseToken,
                stagedResult.Score,
                stagedResult.MatchDetails,
                result.SfiaExtractResult,
                result.CvAnalysisQuality,
                CvAnalysisMetadataReader.SerializeCoverage(result.CvAnalysisCoverage),
                CvAnalysisMetadataReader.SerializeDiagnostics(result.CvAnalysisDiagnostics),
                result.JdAnalysisQuality,
                JdAnalysisMetadataReader.SerializeCoverage(result.JdAnalysisCoverage),
                JdAnalysisMetadataReader.SerializeDiagnostics(result.JdAnalysisDiagnostics),
                DateTime.UtcNow,
                cancellationToken);
            if (!staged)
            {
                _logger.LogInformation("Matching lease lost before terminal staging for job {JobId}.", job.Id);
                return;
            }

            try
            {
                var completed = await FinalizeStagedAsync(
                    job,
                    workerId,
                    leaseToken,
                    stagedResult,
                    result.SfiaExtractResult,
                    result.CvAnalysisQuality,
                    CvAnalysisMetadataReader.SerializeCoverage(result.CvAnalysisCoverage),
                    CvAnalysisMetadataReader.SerializeDiagnostics(result.CvAnalysisDiagnostics),
                    result.JdAnalysisQuality,
                    JdAnalysisMetadataReader.SerializeCoverage(result.JdAnalysisCoverage),
                    JdAnalysisMetadataReader.SerializeDiagnostics(result.JdAnalysisDiagnostics),
                    cancellationToken);
                if (!completed)
                {
                    _logger.LogInformation("Matching lease lost before finalization for job {JobId}.", job.Id);
                    return;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Matching terminal finalization deferred for job {JobId}.", job.Id);
                await _jobRepository.ScheduleFinalizationRetryAsync(
                    job.Id,
                    workerId,
                    leaseToken,
                    DateTime.UtcNow.Add(GetRetryDelay(job.AttemptCount)),
                    DateTime.UtcNow,
                    cancellationToken);
                return;
            }

            if (_sourceAnalysisPersistence is not null)
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

    private async Task ReportProcessingStageAsync(
        Guid jobId,
        string workerId,
        Guid leaseToken,
        string processingStage,
        CancellationToken attemptCancellationToken,
        CancellationToken hostCancellationToken)
    {
        try
        {
            var updated = await _jobRepository.UpdateProcessingStageAsync(
                jobId,
                workerId,
                leaseToken,
                processingStage,
                DateTime.UtcNow,
                attemptCancellationToken);
            if (!updated)
            {
                _logger.LogDebug(
                    "Matching progress stage {ProcessingStage} was not persisted because the lease is no longer active for job {JobId}.",
                    processingStage,
                    jobId);
            }
        }
        catch (OperationCanceledException) when (!hostCancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug(
                "Matching progress stage {ProcessingStage} was skipped after the attempt deadline for job {JobId}.",
                processingStage,
                jobId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Progress is observability data. A transient write failure must never
            // turn an otherwise valid matching result into a failed job.
            _logger.LogWarning(
                exception,
                "Matching progress stage {ProcessingStage} could not be persisted for job {JobId}; processing continues.",
                processingStage,
                jobId);
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
                if (string.Equals(
                        job.ErrorCode,
                        ICvJdMatchingJobRepository.ResultFinalizationPending,
                        StringComparison.Ordinal))
                {
                    await _jobRepository.ScheduleRecoveredFinalizationRetryAsync(
                        job.Id,
                        utcNow,
                        utcNow,
                        cancellationToken);
                    continue;
                }

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

    private async Task FinalizeAlreadyStagedAsync(
        CvJobMatchScores job,
        string workerId,
        Guid leaseToken,
        CancellationToken cancellationToken)
    {
        var staged = _stagedResultReader.ReadOrCreateSafeFallback(job);
        try
        {
            await FinalizeStagedAsync(
                job,
                workerId,
                leaseToken,
                staged,
                job.SfiaExtractResult,
                job.CvAnalysisQuality,
                job.CvAnalysisCoverageJson,
                job.CvAnalysisDiagnosticsJson,
                job.JdAnalysisQuality,
                job.JdAnalysisCoverageJson,
                job.JdAnalysisDiagnosticsJson,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Matching staged finalization deferred for job {JobId}.", job.Id);
            await _jobRepository.ScheduleFinalizationRetryAsync(
                job.Id,
                workerId,
                leaseToken,
                DateTime.UtcNow.Add(GetRetryDelay(job.AttemptCount)),
                DateTime.UtcNow,
                cancellationToken);
        }
    }

    private async Task<bool> FinalizeStagedAsync(
        CvJobMatchScores job,
        string workerId,
        Guid leaseToken,
        StagedMatchingResult staged,
        string? sfiaExtractResult,
        CvAnalysisQuality? cvAnalysisQuality,
        string? cvAnalysisCoverageJson,
        string? cvAnalysisDiagnosticsJson,
        JdAnalysisQuality? jdAnalysisQuality,
        string? jdAnalysisCoverageJson,
        string? jdAnalysisDiagnosticsJson,
        CancellationToken cancellationToken)
    {
        var transaction = IsInMemoryProvider()
            ? null
            : await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var completed = await _jobRepository.CompleteAsync(
                job.Id,
                workerId,
                leaseToken,
                staged.Score,
                staged.MatchDetails,
                sfiaExtractResult,
                cvAnalysisQuality,
                cvAnalysisCoverageJson,
                cvAnalysisDiagnosticsJson,
                DateTime.UtcNow,
                jdAnalysisQuality,
                jdAnalysisCoverageJson,
                jdAnalysisDiagnosticsJson,
                cancellationToken);
            if (!completed)
            {
                if (transaction != null)
                    await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            if (staged.RequiresRefund)
            {
                await _featureUsageUseCase.RefundFeatureReservationAsync(
                    job.UserId,
                    job.Id,
                    "score_unavailable",
                    cancellationToken);
            }

            if (transaction != null)
                await transaction.CommitAsync(cancellationToken);
            return true;
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
