using System.Threading;
using System.Threading.Tasks;

namespace ITHunterview.Service.Interface.Service.Matching;

/// <summary>
/// Best-effort progress notification emitted at real matching boundaries.
/// Implementations must not use this callback to change matching behavior.
/// </summary>
public delegate Task MatchingProgressCallback(
    string processingStage,
    CancellationToken cancellationToken);
