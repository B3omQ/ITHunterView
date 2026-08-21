using System.Threading.Channels;
using ITHunterview.Service.Interface.Service.Matching;

namespace ITHunterview.WebAPI.BackgroundServices;

public sealed class CandidateJobScanQueue : ICandidateJobScanQueue
{
    private readonly Channel<CandidateJobScanRequest> _queue = Channel.CreateBounded<CandidateJobScanRequest>(new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.Wait });
    public ValueTask EnqueueAsync(CandidateJobScanRequest request, CancellationToken ct = default) => _queue.Writer.WriteAsync(request, ct);
    public ValueTask<CandidateJobScanRequest> DequeueAsync(CancellationToken ct) => _queue.Reader.ReadAsync(ct);
}
