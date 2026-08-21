using System;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.UseCase;

public interface IRecruiterCvUnlockUseCase
{
    Task<UnlockCandidateResponseDto> UnlockAsync(
        Guid recruiterUserId,
        Guid scanResultId,
        CancellationToken ct = default);
}
