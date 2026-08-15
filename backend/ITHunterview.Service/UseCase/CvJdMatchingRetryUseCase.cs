using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.Service.Matching;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.UseCase;

/// <summary>
/// Creates one controlled retry from an immutable failed-job snapshot. The
/// original job remains a permanent audit record and can only be retried once.
/// </summary>
public sealed class CvJdMatchingRetryUseCase : ICvJdMatchingRetryUseCase
{
    private readonly ITHunterviewContext _context;
    private readonly ICvJdMatchingJobRepository _jobRepository;
    private readonly ICandidateFeatureUsageUseCase _featureUsageUseCase;

    public CvJdMatchingRetryUseCase(
        ITHunterviewContext context,
        ICvJdMatchingJobRepository jobRepository,
        ICandidateFeatureUsageUseCase featureUsageUseCase)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        _featureUsageUseCase = featureUsageUseCase ?? throw new ArgumentNullException(nameof(featureUsageUseCase));
    }

    public async Task<MatchingSubmissionResult> RetryAsync(
        Guid userId,
        Guid failedJobId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("USER_ID_REQUIRED", nameof(userId));
        if (failedJobId == Guid.Empty)
            throw new ArgumentException("MATCHING_JOB_ID_REQUIRED", nameof(failedJobId));

        var normalizedKey = NormalizeIdempotencyKey(idempotencyKey);
        var (transaction, ownsTransaction) = await BeginTransactionAsync(cancellationToken);
        try
        {
            await _featureUsageUseCase.AcquireFeatureSubmissionLockAsync(userId, cancellationToken);

            var existing = await _jobRepository.GetByIdempotencyKeyForUpdateAsync(
                userId,
                normalizedKey,
                cancellationToken);
            if (existing != null)
            {
                if (existing.RetryOfJobId != failedJobId)
                    throw new InvalidOperationException("IDEMPOTENCY_KEY_REUSED");
                return new MatchingSubmissionResult(existing.Id, true);
            }

            var failedJob = await _jobRepository.GetFailedJobForRetryForUpdateAsync(
                userId,
                failedJobId,
                cancellationToken);
            if (failedJob == null)
                throw new KeyNotFoundException("MATCHING_FAILED_JOB_NOT_FOUND");
            if (failedJob.ManualRetryUsed)
                throw new InvalidOperationException("MANUAL_RETRY_ALREADY_USED");
            if (!MatchingRetryPolicy.IsManualRetryAllowed(failedJob.ErrorCode))
                throw new InvalidOperationException("MATCHING_RETRY_NOT_ALLOWED");
            if (string.IsNullOrWhiteSpace(failedJob.InputSnapshotJson) || string.IsNullOrWhiteSpace(failedJob.InputHash))
                throw new InvalidOperationException("MATCHING_SNAPSHOT_UNAVAILABLE");

            var now = DateTime.UtcNow;
            var retryJob = new CvJobMatchScores
            {
                Id = Guid.NewGuid(),
                UserId = failedJob.UserId,
                CvId = failedJob.CvId,
                CvFileName = failedJob.CvFileName,
                JobId = failedJob.JobId,
                RawJdText = failedJob.RawJdText,
                JdTitle = failedJob.JdTitle,
                MatchScore = null,
                MatchDetails = string.Empty,
                Status = "Pending",
                ProcessingStage = MatchingProcessingStages.Queued,
                ErrorMessage = null,
                UpdatedAt = now,
                MatchType = "AI",
                SfiaExtractResult = null,
                InputSnapshotJson = failedJob.InputSnapshotJson,
                InputHash = failedJob.InputHash,
                IdempotencyKey = normalizedKey,
                IdempotencyRequestHash = failedJob.IdempotencyRequestHash,
                AttemptCount = 0,
                MaxAttempts = Math.Max(1, failedJob.MaxAttempts),
                CreatedAt = now,
                NextAttemptAt = now,
                ManualRetryUsed = false,
                RetryOfJobId = failedJob.Id
            };

            failedJob.ManualRetryUsed = true;
            failedJob.UpdatedAt = now;
            _jobRepository.AddPending(retryJob);
            await _context.SaveChangesAsync(cancellationToken);

            var reservation = await _featureUsageUseCase.ReserveFeatureAsync(
                userId,
                CvJdMatchingSubmissionUseCase.FeatureKey,
                retryJob.Id,
                cancellationToken);
            await _featureUsageUseCase.CaptureFeatureReservationAsync(
                reservation.ReservationId,
                cancellationToken);
            retryJob.BillingReservationId = reservation.ReservationId;
            retryJob.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            if (ownsTransaction)
                await transaction!.CommitAsync(cancellationToken);
            return new MatchingSubmissionResult(retryJob.Id, false);
        }
        catch
        {
            if (ownsTransaction && transaction != null)
                await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (ownsTransaction && transaction != null)
                await transaction.DisposeAsync();
        }
    }

    private static string NormalizeIdempotencyKey(string idempotencyKey)
    {
        var key = idempotencyKey?.Trim() ?? string.Empty;
        if (key.Length == 0 || key.Length > CvJdMatchingSubmissionUseCase.MaximumIdempotencyKeyLength ||
            !key.All(ch => char.IsLetterOrDigit(ch) || ch is '.' or '_' or ':' or '-'))
            throw new ArgumentException("IDEMPOTENCY_KEY_INVALID", nameof(idempotencyKey));
        return key;
    }

    private async Task<(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? Transaction, bool OwnsTransaction)> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        var current = _context.Database.CurrentTransaction;
        if (current != null || IsInMemoryProvider())
            return (current, false);
        return (await _context.Database.BeginTransactionAsync(cancellationToken), true);
    }

    private bool IsInMemoryProvider()
        => string.Equals(_context.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal);
}
