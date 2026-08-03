using System;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.Persistence;

public interface ICvJdMatchingHistoryRepository
{
    Task<HideMatchHistoryResult> HideAsync(
        Guid jobId,
        Guid userId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);
}
