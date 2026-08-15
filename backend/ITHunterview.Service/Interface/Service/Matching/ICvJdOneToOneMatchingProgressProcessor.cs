using System;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.Service.Matching;

public interface ICvJdOneToOneMatchingProgressProcessor
{
    Task<CvJdMatchingExecutionResult> ExecuteWithProgressAsync(
        Guid matchId,
        MatchingInputSnapshotV1 snapshot,
        MatchingProgressCallback progressCallback,
        CancellationToken cancellationToken = default);
}
