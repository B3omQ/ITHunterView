using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.Service.Matching;

public interface IMatchingJdPreparationService
{
    Task<PreparedJdMatchingInput> PrepareAsync(
        MatchingInputSnapshotV1 snapshot,
        CancellationToken ct = default);
}
