using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.UseCase;

public interface IRecruiterCvScanUseCase
{
    Task<RecruiterCvScanRunDto> ScanAsync(Guid recruiterUserId, Guid jobId, CancellationToken ct);

    Task<PagedResult<RecruiterCvScanResultDto>> GetLatestSuccessfulAsync(
        Guid recruiterUserId,
        Guid jobId,
        int page,
        int pageSize,
        CancellationToken ct);
}
