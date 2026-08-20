using System;
using System.Collections.Generic;
using System.Linq;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ITHunterview.Service.UseCase;

public sealed class RecruiterCvUnlockUseCase : IRecruiterCvUnlockUseCase
{
    private const int UnlockCostCoins = 50;

    private readonly ITHunterviewContext _context;
    private readonly IRecruiterCvUnlockRepository _unlockRepository;
    private readonly IRecruiterUnlockedCvSnapshotStore _snapshotStore;
    private readonly ILogger<RecruiterCvUnlockUseCase> _logger;

    public RecruiterCvUnlockUseCase(
        ITHunterviewContext context,
        IRecruiterCvUnlockRepository unlockRepository,
        IRecruiterUnlockedCvSnapshotStore snapshotStore,
        ILogger<RecruiterCvUnlockUseCase> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _unlockRepository = unlockRepository ?? throw new ArgumentNullException(nameof(unlockRepository));
        _snapshotStore = snapshotStore ?? throw new ArgumentNullException(nameof(snapshotStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UnlockCandidateResponseDto> UnlockAsync(
        Guid recruiterUserId,
        Guid scanResultId,
        CancellationToken ct = default)
    {
        if (recruiterUserId == Guid.Empty)
        {
            throw new ArgumentException("Recruiter user ID cannot be empty.", nameof(recruiterUserId));
        }

        if (scanResultId == Guid.Empty)
        {
            throw new ArgumentException("Scan result ID cannot be empty.", nameof(scanResultId));
        }

        // 1. Authority validation: Scan result must exist and belong to a scan run owned by the recruiter
        var scanResult = await _context.RecruiterCvScanResults
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == scanResultId, ct);
        if (scanResult == null)
        {
            throw new KeyNotFoundException("SCAN_RESULT_NOT_FOUND: Recruiter CV scan result does not exist.");
        }

        var scanRun = await _context.RecruiterCvScanRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == scanResult.RunId, ct);
        if (scanRun == null || scanRun.RecruiterUserId != recruiterUserId)
        {
            throw new UnauthorizedAccessException("SCAN_RESULT_FORBIDDEN: Caller does not own this scan result.");
        }

        var cvId = scanResult.CvId;
        var candidateUserId = scanResult.CandidateUserId;

        // 2. Check for existing completed unlock ledger
        var existing = await _unlockRepository.GetByRecruiterAndCvAsync(recruiterUserId, cvId, ct);
        if (existing is not null && existing.Status == RecruiterCvUnlockStatus.Completed)
        {
            return await BuildCompletedResponseAsync(existing, scanResultId, candidateUserId, cvId, ct);
        }

        // 3. Acquire or observe Pending row (Concurrency Coordinator)
        var (ledger, isCaptureOwner) = await _unlockRepository.AcquirePendingAsync(
            recruiterUserId,
            cvId,
            scanResultId,
            scanRun.JobId,
            ct);

        if (!isCaptureOwner)
        {
            if (ledger.Status == RecruiterCvUnlockStatus.Completed)
            {
                return await BuildCompletedResponseAsync(ledger, scanResultId, candidateUserId, cvId, ct);
            }

            var reloaded = await _unlockRepository.GetByIdAsync(ledger.Id, ct);
            if (reloaded is not null && reloaded.Status == RecruiterCvUnlockStatus.Completed)
            {
                return await BuildCompletedResponseAsync(reloaded, scanResultId, candidateUserId, cvId, ct);
            }
        }

        // 4. Candidate CV existence check (linearized at first unlock attempt)
        var cv = await _context.Cvs.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cvId, ct);
        if (cv == null || cv.DeletedAt.HasValue)
        {
            await _unlockRepository.FailAsync(ledger.Id, "CV_NOT_FOUND_OR_DELETED", ct);
            throw new InvalidOperationException("CV_NOT_FOUND_OR_DELETED: Candidate CV no longer exists or was deleted.");
        }

        // 5. Capture immutable snapshot copy
        RetainedCvSnapshot snapshot;
        try
        {
            snapshot = await _snapshotStore.CaptureAsync(ledger.Id, cv, ct);
        }
        catch (Exception ex)
        {
            await _unlockRepository.FailAsync(ledger.Id, "RETAINED_CV_CAPTURE_FAILED", ct);
            throw new InvalidOperationException("RETAINED_CV_CAPTURE_FAILED: Failed to capture immutable retained CV copy.", ex);
        }

        // 6. Billing check and execution
        string unlockedVia;
        int coinsSpent;

        var now = DateTime.UtcNow;
        var activeSub = await _context.UserSubscriptions
            .Where(us => us.UserId == recruiterUserId &&
                         us.Status == UserSubscriptionStatus.ACTIVE &&
                         us.EndDate >= now &&
                         us.StartDate <= now)
            .OrderByDescending(us => us.EndDate)
            .FirstOrDefaultAsync(ct);

        int unlockQuota = 0;
        if (activeSub != null)
        {
            var sub = await _context.Subscriptions.FirstOrDefaultAsync(s => s.Id == activeSub.SubId, ct);
            if (sub != null && !string.IsNullOrEmpty(sub.FeaturesConfig))
            {
                try
                {
                    using var doc = JsonDocument.Parse(sub.FeaturesConfig);
                    if (doc.RootElement.TryGetProperty("unlockCvLimit", out var limitProp))
                    {
                        unlockQuota = limitProp.GetInt32();
                    }
                }
                catch
                {
                    // Ignored if JSON parsing fails
                }
            }
        }

        bool hasQuota = false;
        if (unlockQuota > 0 && activeSub != null)
        {
            var usedQuota = await _context.RecruiterUnlockedCvs
                .CountAsync(u => u.RecruiterId == recruiterUserId &&
                                 u.Status == RecruiterCvUnlockStatus.Completed &&
                                 u.UnlockedVia == "SUBSCRIPTION" &&
                                 u.UnlockedAt >= activeSub.StartDate &&
                                 u.UnlockedAt <= activeSub.EndDate, ct);
            if (usedQuota < unlockQuota)
            {
                hasQuota = true;
            }
        }

        // 7. Complete the unlock ledger FIRST (before any billing deduction).
        // This guarantees: unlock is never lost due to a billing failure, and coin
        // is never deducted without a corresponding Completed ledger row.
        // If CompleteAsync returns false (concurrent loser), the coin is NOT deducted —
        // the concurrent winner already deducted (or used subscription quota).
        if (hasQuota)
        {
            unlockedVia = "SUBSCRIPTION";
            coinsSpent = 0;
        }
        else
        {
            // Determine billing intent but do NOT deduct yet — wait until after CompleteAsync.
            var wallet = await _context.UserWallets.FirstOrDefaultAsync(w => w.UserId == recruiterUserId, ct);
            if (wallet == null || wallet.Balance < UnlockCostCoins)
            {
                await _unlockRepository.FailAsync(ledger.Id, "INSUFFICIENT_FUNDS_OR_QUOTA", ct);
                throw new InvalidOperationException("INSUFFICIENT_FUNDS_OR_QUOTA: Insufficient subscription quota and coin balance to unlock CV.");
            }

            unlockedVia = "COINS";
            coinsSpent = UnlockCostCoins;
        }

        var completed = await _unlockRepository.CompleteAsync(
            ledger.Id, snapshot, unlockedVia, coinsSpent, DateTime.UtcNow, ct);

        if (!completed)
        {
            // Concurrent loser: another request won the CAS race and already completed this unlock.
            // Do NOT deduct coin — the winner handles billing.
            var reloaded = await _unlockRepository.GetByIdAsync(ledger.Id, ct);
            if (reloaded != null && reloaded.Status == RecruiterCvUnlockStatus.Completed)
            {
                return await BuildCompletedResponseAsync(reloaded, scanResultId, candidateUserId, cvId, ct);
            }
            // If still not Completed (edge case), fail safe.
            throw new InvalidOperationException("UNLOCK_RACE_CONDITION: Unlock could not be completed. Please retry.");
        }

        // 8. Billing execution (only for the CAS winner, only when unlock is Completed).
        if (unlockedVia == "COINS")
        {
            try
            {
                var wallet = await _context.UserWallets.FirstOrDefaultAsync(w => w.UserId == recruiterUserId, ct);
                if (wallet != null && wallet.Balance >= UnlockCostCoins)
                {
                    wallet.Balance -= UnlockCostCoins;
                    wallet.UpdatedAt = DateTime.UtcNow;
                    _context.UserWallets.Update(wallet);

                    var creditTx = new CreditTransactions
                    {
                        Id = Guid.NewGuid(),
                        WalletId = wallet.Id,
                        Amount = -UnlockCostCoins,
                        TransactionType = CreditTransactionType.DEDUCT,
                        ReferenceId = cvId,
                        Description = $"Mở khóa hồ sơ CV ứng viên ({cv.FileName ?? "Candidate CV"})",
                        CreatedAt = DateTime.UtcNow
                    };
                    await _context.CreditTransactions.AddAsync(creditTx, ct);
                    await _context.SaveChangesAsync(ct);
                }
                else
                {
                    _logger.LogWarning(
                        "Unlock {LedgerId}: CompleteAsync succeeded but coin deduction failed (wallet insufficient at deduction time). Unlock is retained; billing audit required.",
                        ledger.Id);
                }
            }
            catch (Exception ex)
            {
                // Unlock is already Completed — do not roll back or throw.
                // Log for billing audit; the unlock entitlement is valid.
                _logger.LogError(ex,
                    "Unlock {LedgerId}: CompleteAsync succeeded but coin deduction threw. Unlock retained; billing audit required.",
                    ledger.Id);
            }
        }

        // 9. Reload the persisted entity to build the response — never use a local object.
        var persistedUnlock = await _unlockRepository.GetByIdAsync(ledger.Id, ct);
        if (persistedUnlock == null)
        {
            throw new InvalidOperationException("UNLOCK_PERSIST_ERROR: Completed unlock record could not be reloaded.");
        }

        return await BuildCompletedResponseAsync(persistedUnlock, scanResultId, candidateUserId, cvId, ct);
    }

    private async Task<UnlockCandidateResponseDto> BuildCompletedResponseAsync(
        RecruiterUnlockedCvs unlock,
        Guid scanResultId,
        Guid candidateUserId,
        Guid cvId,
        CancellationToken ct)
    {
        var candidateUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == candidateUserId, ct);
        var candidateProfile = await _context.CandidateProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == candidateUserId, ct);

        string fileUrl;
        bool isRetainedCopy = false;

        if (!string.IsNullOrWhiteSpace(unlock.SnapshotStorageKey))
        {
            fileUrl = await _snapshotStore.CreateAuthorizedReadUrlAsync(unlock.SnapshotStorageKey, ct);
            isRetainedCopy = true;
        }
        else
        {
            var fallbackCv = await _context.Cvs.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cvId, ct);
            fileUrl = fallbackCv?.FileUrl ?? string.Empty;
        }

        var candidateName = string.Join(" ", new[] { candidateProfile?.FirstName, candidateProfile?.LastName }
            .Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
        if (string.IsNullOrWhiteSpace(candidateName))
        {
            candidateName = "Candidate";
        }

        var recruiterWallet = await _context.UserWallets.AsNoTracking()
            .FirstOrDefaultAsync(w => w.UserId == unlock.RecruiterId, ct);

        return new UnlockCandidateResponseDto
        {
            UnlockId = unlock.Id,
            ScanResultId = scanResultId,
            CvId = cvId,
            CandidateUserId = candidateUserId,
            CandidateName = candidateName,
            Email = candidateUser?.Email ?? string.Empty,
            Phone = candidateProfile?.Phone ?? string.Empty,
            FileName = unlock.SnapshotFileName ?? "cv.pdf",
            FileUrl = fileUrl,
            UnlockedVia = unlock.UnlockedVia,
            CoinsSpent = unlock.CoinsSpent,
            UnlockedAt = unlock.UnlockedAt,
            IsRetainedCopy = isRetainedCopy,
            Success = true,
            Message = unlock.UnlockedVia == "SUBSCRIPTION"
                ? "Mở khóa hồ sơ ứng viên thành công bằng quyền Subscription!"
                : "Mở khóa hồ sơ ứng viên thành công bằng Coin!",
            RemainingCoins = recruiterWallet?.Balance ?? 0
        };
    }
}
