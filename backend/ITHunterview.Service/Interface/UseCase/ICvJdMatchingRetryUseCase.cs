using System;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.UseCase;

public interface ICvJdMatchingRetryUseCase
{
    Task<MatchingSubmissionResult> RetryAsync(
        Guid userId,
        Guid failedJobId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}
