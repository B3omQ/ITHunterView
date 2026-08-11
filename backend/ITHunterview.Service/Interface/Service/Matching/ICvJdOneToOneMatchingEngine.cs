using System;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.Service.Matching;

/// <summary>
/// Execution-only boundary used by the durable worker. Implementations return
/// the result and never own job status or billing transitions.
/// </summary>
public interface ICvJdOneToOneMatchingEngine
{
    Task<CvJdMatchingExecutionResult> ExecuteAsync(
        Guid matchId,
        MatchingInputSnapshotV1 snapshot,
        CancellationToken cancellationToken = default);
}
