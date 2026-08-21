using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Persistence;

public interface ICandidateJobScanRepository
{
    Task<CandidateJobScanRun> CreatePendingAsync(CandidateJobScanRun run, CancellationToken ct);

    Task<bool> TryStartAsync(Guid runId, DateTime startedAt, CancellationToken ct);

    Task CompleteAsync(
        Guid runId,
        IReadOnlyCollection<CandidateJobScanResult> results,
        DateTime completedAt,
        CancellationToken ct);

    Task FailAsync(
        Guid runId,
        string errorCode,
        string errorMessage,
        DateTime failedAt,
        CancellationToken ct);

    Task<CandidateJobScanRun?> GetPendingOrProcessingByIdAsync(
        Guid runId,
        CancellationToken ct);

    Task<CandidateJobScanRun?> GetLatestCompletedAsync(
        Guid candidateUserId,
        Guid cvId,
        CancellationToken ct);

    Task<(IReadOnlyList<CandidateJobScanResult> Items, int TotalCount)> GetResultPageAsync(
        Guid runId,
        int skip,
        int take,
        CancellationToken ct);
}
