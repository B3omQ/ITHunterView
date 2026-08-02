using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.UseCase;

public interface ICvJdMatchingWorkerUseCase
{
    Task<IReadOnlyList<ClaimedMatchingJob>> ClaimRunnableJobsAsync(
        int limit,
        string workerId,
        DateTime utcNow,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    Task ProcessClaimedJobAsync(
        Guid jobId,
        string workerId,
        Guid leaseToken,
        CancellationToken cancellationToken = default);

    Task RecoverExpiredLeasesAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default);
}
