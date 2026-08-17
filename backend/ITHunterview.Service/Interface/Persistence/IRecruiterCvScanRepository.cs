using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Persistence;

public interface IRecruiterCvScanRepository
{
    Task<RecruiterCvScanRun> CreatePendingAsync(RecruiterCvScanRun run, CancellationToken ct);

    Task<bool> TryStartAsync(Guid runId, DateTime startedAt, CancellationToken ct);

    Task CompleteAsync(
        Guid runId,
        IReadOnlyCollection<RecruiterCvScanResult> results,
        DateTime completedAt,
        CancellationToken ct);

    Task FailAsync(
        Guid runId,
        string errorCode,
        string errorMessage,
        DateTime failedAt,
        CancellationToken ct);

    Task<RecruiterCvScanRun?> GetLatestCompletedAsync(
        Guid recruiterUserId,
        Guid companyId,
        Guid jobId,
        CancellationToken ct);

    Task<(IReadOnlyList<RecruiterCvScanResult> Items, int TotalCount)> GetResultPageAsync(
        Guid runId,
        int skip,
        int take,
        CancellationToken ct);

    Task<(RecruiterCvScanResult Result, RecruiterCvScanRun Run)?> GetOwnedResultAsync(
        Guid scanResultId,
        Guid recruiterUserId,
        CancellationToken ct);
}
