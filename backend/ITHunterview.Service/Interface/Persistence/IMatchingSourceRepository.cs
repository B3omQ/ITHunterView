using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Persistence;

/// <summary>
/// Persistence boundary for matching-source authorization. These methods are
/// intentionally separate from general CV and job repositories so callers do
/// not accidentally use unscoped lookups in a candidate matching flow.
/// </summary>
public interface IMatchingSourceRepository
{
    Task<Cvs?> GetOwnedCvAsync(Guid cvId, Guid userId, CancellationToken ct = default);

    Task<Cvs?> GetOwnedCvForUpdateAsync(Guid cvId, Guid userId, CancellationToken ct = default);

    Task<JobPostings?> GetAccessiblePublishedJobAsync(
        Guid jobId,
        DateTime utcNow,
        CancellationToken ct = default);

    Task<JobPostings?> GetAccessibleJobAsync(
        Guid jobId,
        Guid candidateId,
        DateTime utcNow,
        CancellationToken ct = default);
}
