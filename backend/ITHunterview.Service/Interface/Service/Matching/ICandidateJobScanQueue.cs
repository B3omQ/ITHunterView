namespace ITHunterview.Service.Interface.Service.Matching;

public sealed record CandidateJobScanRequest(Guid RunId, Guid CandidateUserId, Guid CvId);

public interface ICandidateJobScanQueue
{
    ValueTask EnqueueAsync(CandidateJobScanRequest request, CancellationToken ct = default);
    ValueTask<CandidateJobScanRequest> DequeueAsync(CancellationToken ct);
}
