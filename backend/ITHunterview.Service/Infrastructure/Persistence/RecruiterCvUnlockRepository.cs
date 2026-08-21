using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ITHunterview.Service.Infrastructure.Persistence;

public sealed class RecruiterCvUnlockRepository : IRecruiterCvUnlockRepository
{
    private const int MaximumFailureCodeLength = 128;
    private readonly ITHunterviewContext _context;

    public RecruiterCvUnlockRepository(ITHunterviewContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<RecruiterUnlockedCvs?> GetByRecruiterAndCvAsync(
        Guid recruiterUserId,
        Guid cvId,
        CancellationToken ct = default)
    {
        return await _context.RecruiterUnlockedCvs
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.RecruiterId == recruiterUserId && u.CvId == cvId, ct);
    }

    public async Task<RecruiterUnlockedCvs?> GetByIdAsync(Guid unlockId, CancellationToken ct = default)
    {
        return await _context.RecruiterUnlockedCvs
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == unlockId, ct);
    }

    public async Task<(RecruiterUnlockedCvs Ledger, bool IsCaptureOwner)> AcquirePendingAsync(
        Guid recruiterUserId,
        Guid cvId,
        Guid? sourceScanResultId,
        Guid? jobId,
        CancellationToken ct = default)
    {
        var existing = await _context.RecruiterUnlockedCvs
            .FirstOrDefaultAsync(u => u.RecruiterId == recruiterUserId && u.CvId == cvId, ct);

        if (existing is not null)
        {
            if (existing.Status == RecruiterCvUnlockStatus.Completed)
            {
                return (existing, false);
            }

            if (existing.Status == RecruiterCvUnlockStatus.Pending)
            {
                return (existing, false);
            }

            // Retry for Failed status: transition to Pending
            existing.Status = RecruiterCvUnlockStatus.Pending;
            existing.SourceScanResultId = sourceScanResultId ?? existing.SourceScanResultId;
            existing.JobId = jobId ?? existing.JobId;
            existing.FailureCode = null;
            existing.UnlockedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            return (existing, true);
        }

        var newLedger = new RecruiterUnlockedCvs
        {
            Id = Guid.NewGuid(),
            RecruiterId = recruiterUserId,
            CvId = cvId,
            SourceScanResultId = sourceScanResultId,
            JobId = jobId,
            Status = RecruiterCvUnlockStatus.Pending,
            CoinsSpent = 0,
            UnlockedVia = "PENDING",
            UnlockedAt = DateTime.UtcNow
        };

        try
        {
            await _context.RecruiterUnlockedCvs.AddAsync(newLedger, ct);
            await _context.SaveChangesAsync(ct);
            return (newLedger, true);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            _context.Entry(newLedger).State = EntityState.Detached;
            var winner = await _context.RecruiterUnlockedCvs
                .AsNoTracking()
                .FirstAsync(u => u.RecruiterId == recruiterUserId && u.CvId == cvId, ct);
            return (winner, false);
        }
    }

    public async Task<bool> CompleteAsync(
        Guid unlockId,
        RetainedCvSnapshot snapshot,
        string unlockedVia,
        int coinsSpent,
        DateTime completedAt,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (!_context.Database.IsRelational())
        {
            var row = await _context.RecruiterUnlockedCvs.FirstOrDefaultAsync(u => u.Id == unlockId, ct);
            if (row is null || row.Status != RecruiterCvUnlockStatus.Pending)
            {
                return false;
            }

            row.Status = RecruiterCvUnlockStatus.Completed;
            row.SnapshotStorageKey = snapshot.StorageKey;
            row.SnapshotFileName = snapshot.FileName;
            row.SnapshotContentHash = snapshot.ContentHash;
            row.SnapshotCreatedAt = snapshot.CreatedAt;
            row.UnlockedVia = unlockedVia;
            row.CoinsSpent = coinsSpent;
            row.UnlockedAt = completedAt;
            row.FailureCode = null;

            await _context.SaveChangesAsync(ct);
            return true;
        }

        var affected = await _context.RecruiterUnlockedCvs
            .Where(u => u.Id == unlockId && u.Status == RecruiterCvUnlockStatus.Pending)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(u => u.Status, RecruiterCvUnlockStatus.Completed)
                    .SetProperty(u => u.SnapshotStorageKey, snapshot.StorageKey)
                    .SetProperty(u => u.SnapshotFileName, snapshot.FileName)
                    .SetProperty(u => u.SnapshotContentHash, snapshot.ContentHash)
                    .SetProperty(u => u.SnapshotCreatedAt, snapshot.CreatedAt)
                    .SetProperty(u => u.UnlockedVia, unlockedVia)
                    .SetProperty(u => u.CoinsSpent, coinsSpent)
                    .SetProperty(u => u.UnlockedAt, completedAt)
                    .SetProperty(u => u.FailureCode, (string?)null),
                ct);

        return affected == 1;
    }

    public async Task FailAsync(Guid unlockId, string failureCode, CancellationToken ct = default)
    {
        var boundedCode = Truncate(failureCode, MaximumFailureCodeLength);

        if (!_context.Database.IsRelational())
        {
            var row = await _context.RecruiterUnlockedCvs.FirstOrDefaultAsync(u => u.Id == unlockId, ct);
            if (row is not null && row.Status == RecruiterCvUnlockStatus.Pending)
            {
                row.Status = RecruiterCvUnlockStatus.Failed;
                row.FailureCode = boundedCode;
                await _context.SaveChangesAsync(ct);
            }
            return;
        }

        await _context.RecruiterUnlockedCvs
            .Where(u => u.Id == unlockId && u.Status == RecruiterCvUnlockStatus.Pending)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(u => u.Status, RecruiterCvUnlockStatus.Failed)
                    .SetProperty(u => u.FailureCode, boundedCode),
                ct);
    }

    public async Task<IReadOnlySet<Guid>> GetUnlockedCvIdsAsync(
        Guid recruiterUserId,
        IReadOnlyCollection<Guid> cvIds,
        CancellationToken ct = default)
    {
        if (cvIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        var unlocked = await _context.RecruiterUnlockedCvs
            .AsNoTracking()
            .Where(u => u.RecruiterId == recruiterUserId &&
                        u.Status == RecruiterCvUnlockStatus.Completed &&
                        cvIds.Contains(u.CvId))
            .Select(u => u.CvId)
            .ToListAsync(ct);

        return unlocked.ToHashSet();
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation;
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
    }
}
