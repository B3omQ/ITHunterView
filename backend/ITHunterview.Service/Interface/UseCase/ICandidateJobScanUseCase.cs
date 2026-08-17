using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.UseCase;

public interface ICandidateJobScanUseCase
{
    Task<CandidateJobScanAcceptedDto> CreateRunAsync(
        Guid candidateUserId,
        Guid cvId,
        CancellationToken ct);

    Task ProcessRunAsync(Guid runId, CancellationToken ct);

    Task<PagedResult<CandidateJobScanResultDto>> GetLatestSuccessfulAsync(
        Guid candidateUserId,
        Guid cvId,
        int page,
        int pageSize,
        CancellationToken ct);
}
