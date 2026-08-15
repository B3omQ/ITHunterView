using System;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.Service.Matching;

/// <summary>
/// Optional progress-aware execution capability. Keeping it separate preserves
/// compatibility for existing processors and tests that only need final output.
/// </summary>
public interface ICvJdOneToOneMatchingProgressEngine
{
    Task<CvJdMatchingExecutionResult> ExecuteWithProgressAsync(
        Guid matchId,
        MatchingInputSnapshotV1 snapshot,
        MatchingProgressCallback progressCallback,
        CancellationToken cancellationToken = default);
}
