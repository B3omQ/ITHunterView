using System;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.UseCase;

public interface ICvJdMatchingSubmissionUseCase
{
    Task<MatchingSubmissionResult> SubmitAsync(
        Guid userId,
        MatchingRequestDto request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

}
