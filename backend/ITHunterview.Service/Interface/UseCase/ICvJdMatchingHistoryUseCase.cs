using System;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.UseCase;

public interface ICvJdMatchingHistoryUseCase
{
    Task<HideMatchHistoryResult> HideAsync(
        Guid jobId,
        Guid userId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);
}
