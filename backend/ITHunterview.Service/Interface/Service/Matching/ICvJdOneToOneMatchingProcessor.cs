using System;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.Service.Matching;

public interface ICvJdOneToOneMatchingProcessor
{
    Task<CvJdMatchingExecutionResult> ExecuteAsync(
        Guid matchId,
        MatchingInputSnapshotV1 snapshot,
        CancellationToken cancellationToken = default);
}
