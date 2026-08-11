using System;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Service.Matching;

namespace ITHunterview.Service.Service.Matching;

/// <summary>
/// Thin execution boundary around the existing matching engine. Lifecycle
/// state, leases, retries and billing stay in the worker; this component only
/// passes an immutable snapshot to the engine and returns its result.
/// </summary>
public sealed class CvJdOneToOneMatchingProcessor : ICvJdOneToOneMatchingProcessor
{
    private readonly ICvJdOneToOneMatchingEngine _engine;

    public CvJdOneToOneMatchingProcessor(ICvJdOneToOneMatchingEngine engine)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    public Task<CvJdMatchingExecutionResult> ExecuteAsync(
        Guid matchId,
        MatchingInputSnapshotV1 snapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return _engine.ExecuteAsync(matchId, snapshot, cancellationToken);
    }
}
