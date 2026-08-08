using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.Service.Matching;

public interface IMatchingCvPreparationService
{
    Task<PreparedCvMatchingInput> PrepareAsync(
        Guid userId,
        MatchingInputSnapshotV1 snapshot,
        CancellationToken ct = default);
}
