using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.UseCase;

/// <summary>
/// Owns the one-to-one matching submission transaction. No work is dispatched
/// from this class; a durable worker will claim the committed Pending row.
/// </summary>
public sealed class CvJdMatchingSubmissionUseCase : ICvJdMatchingSubmissionUseCase
{
    public const string FeatureKey = "CvJdMatching";
    public const int MaximumIdempotencyKeyLength = 128;

    private readonly ITHunterviewContext _context;
    private readonly IMatchingRequestValidator _requestValidator;
    private readonly IMatchingInputPreflightUseCase _preflightUseCase;
    private readonly MatchingInputSnapshotBuilder _snapshotBuilder;
    private readonly ICvJdMatchingJobRepository _jobRepository;
    private readonly ICandidateFeatureUsageUseCase _featureUsageUseCase;

    public CvJdMatchingSubmissionUseCase(
        ITHunterviewContext context,
        IMatchingRequestValidator requestValidator,
        IMatchingInputPreflightUseCase preflightUseCase,
        MatchingInputSnapshotBuilder snapshotBuilder,
        ICvJdMatchingJobRepository jobRepository,
        ICandidateFeatureUsageUseCase featureUsageUseCase)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _requestValidator = requestValidator ?? throw new ArgumentNullException(nameof(requestValidator));
        _preflightUseCase = preflightUseCase ?? throw new ArgumentNullException(nameof(preflightUseCase));
        _snapshotBuilder = snapshotBuilder ?? throw new ArgumentNullException(nameof(snapshotBuilder));
        _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        _featureUsageUseCase = featureUsageUseCase ?? throw new ArgumentNullException(nameof(featureUsageUseCase));
    }

    public async Task<MatchingSubmissionResult> SubmitAsync(
        Guid userId,
        MatchingRequestDto request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("USER_ID_REQUIRED", nameof(userId));

        var normalizedKey = NormalizeIdempotencyKey(idempotencyKey);
        var validation = _requestValidator.Validate(request);
        if (!validation.IsValid || validation.Selection is null)
            throw new ArgumentException(validation.FailureCode ?? "INVALID_MATCHING_REQUEST");

        var requestHash = ComputeRequestHash(validation.Selection);
        var (transaction, ownsTransaction) = await BeginTransactionAsync(cancellationToken);
        try
        {
            // This lock is the serialization point for wallet/quota and the
            // idempotency recheck. It also closes the first-use wallet race.
            await _featureUsageUseCase.AcquireFeatureSubmissionLockAsync(userId, cancellationToken);

            var existing = await _jobRepository.GetByIdempotencyKeyForUpdateAsync(
                userId,
                normalizedKey,
                cancellationToken);
            if (existing != null)
            {
                if (!string.Equals(existing.IdempotencyRequestHash, requestHash, StringComparison.Ordinal))
                    throw new InvalidOperationException("IDEMPOTENCY_KEY_REUSED");

                if (ownsTransaction)
                    await transaction!.CommitAsync(cancellationToken);
                return new MatchingSubmissionResult(existing.Id, true);
            }

            // Both authorization and authoritative source reads happen after
            // the transaction starts. SnapshotBuilder re-queries ownership and
            // copies detached scalar values; the worker never needs live rows.
            var prepared = await _preflightUseCase.PrepareAsync(userId, request, cancellationToken);
            var snapshot = await _snapshotBuilder.BuildAsync(userId, prepared, cancellationToken);
            var now = DateTime.UtcNow;
            var job = new CvJobMatchScores
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CvId = prepared.Cv is PreparedSavedCvSource savedCv ? savedCv.CvId : null,
                CvFileName = snapshot.Snapshot.Cv.FileName ?? "Pasted CV",
                JobId = prepared.Jd is PreparedSavedJdSource savedJob ? savedJob.JobId : null,
                JdTitle = snapshot.Snapshot.Jd.Title ?? "Pasted JD",
                RawJdText = snapshot.Snapshot.Jd.OriginalText,
                MatchScore = null,
                MatchDetails = string.Empty,
                Status = "Pending",
                ProcessingStage = MatchingProcessingStages.Queued,
                UpdatedAt = now,
                MatchType = "AI",
                InputSnapshotJson = snapshot.Json,
                InputHash = snapshot.Sha256,
                IdempotencyKey = normalizedKey,
                IdempotencyRequestHash = requestHash,
                AttemptCount = 0,
                MaxAttempts = 3,
                CreatedAt = now,
                NextAttemptAt = now,
                ManualRetryUsed = false
            };

            _jobRepository.AddPending(job);
            await _context.SaveChangesAsync(cancellationToken);

            var reservation = await _featureUsageUseCase.ReserveFeatureAsync(
                userId,
                FeatureKey,
                job.Id,
                cancellationToken);
            await _featureUsageUseCase.CaptureFeatureReservationAsync(
                reservation.ReservationId,
                cancellationToken);
            job.BillingReservationId = reservation.ReservationId;
            job.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            if (ownsTransaction)
                await transaction!.CommitAsync(cancellationToken);
            return new MatchingSubmissionResult(job.Id, false);
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

    private async Task<(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? Transaction, bool OwnsTransaction)> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        var current = _context.Database.CurrentTransaction;
        if (current != null || IsInMemoryProvider())
            return (current, false);
        return (await _context.Database.BeginTransactionAsync(cancellationToken), true);
    }

    private static string NormalizeIdempotencyKey(string idempotencyKey)
    {
        var key = idempotencyKey?.Trim() ?? string.Empty;
        if (key.Length == 0 || key.Length > MaximumIdempotencyKeyLength)
            throw new ArgumentException("IDEMPOTENCY_KEY_INVALID", nameof(idempotencyKey));
        if (!key.All(ch => char.IsLetterOrDigit(ch) || ch is '.' or '_' or ':' or '-'))
            throw new ArgumentException("IDEMPOTENCY_KEY_INVALID", nameof(idempotencyKey));
        return key;
    }

    private static string ComputeRequestHash(MatchingInputSelection selection)
    {
        var canonical = new
        {
            mode = selection.Mode.ToString(),
            cvId = selection.CvId?.ToString("N"),
            cvTextSha256 = selection.CvId.HasValue ? null : Sha256(selection.CvText ?? string.Empty),
            jdId = selection.JobId?.ToString("N"),
            jdTextSha256 = selection.JobId.HasValue ? null : Sha256(selection.RawJdText ?? string.Empty),
            jdTitle = selection.JobId.HasValue ? null : selection.JdTitle
        };
        var json = JsonSerializer.Serialize(canonical, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
        return Sha256(json);
    }

    private static string Sha256(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private bool IsInMemoryProvider()
        => string.Equals(_context.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal);
}
