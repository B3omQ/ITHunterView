using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.Persistence;

public interface IRecruiterCvUnlockRepository
{
    Task<RecruiterUnlockedCvs?> GetByRecruiterAndCvAsync(Guid recruiterUserId, Guid cvId, CancellationToken ct = default);
    Task<RecruiterUnlockedCvs?> GetByIdAsync(Guid unlockId, CancellationToken ct = default);
    Task<(RecruiterUnlockedCvs Ledger, bool IsCaptureOwner)> AcquirePendingAsync(
        Guid recruiterUserId,
        Guid cvId,
        Guid? sourceScanResultId,
        Guid? jobId,
        CancellationToken ct = default);
    Task<bool> CompleteAsync(
        Guid unlockId,
        RetainedCvSnapshot snapshot,
        string unlockedVia,
        int coinsSpent,
        DateTime completedAt,
        CancellationToken ct = default);
    Task FailAsync(Guid unlockId, string failureCode, CancellationToken ct = default);
    Task<IReadOnlySet<Guid>> GetUnlockedCvIdsAsync(Guid recruiterUserId, IReadOnlyCollection<Guid> cvIds, CancellationToken ct = default);
}
