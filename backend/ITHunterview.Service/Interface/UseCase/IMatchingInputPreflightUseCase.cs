using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.UseCase;

public interface IMatchingInputPreflightUseCase
{
    Task<PreparedMatchingRequest> PrepareAsync(
        Guid userId,
        MatchingRequestDto request,
        CancellationToken ct = default);

    Task RecheckAccessAsync(
        Guid userId,
        PreparedMatchingRequest request,
        CancellationToken ct = default);
}
