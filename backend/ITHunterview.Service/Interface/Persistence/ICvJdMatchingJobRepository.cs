using System;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Persistence;

public interface ICvJdMatchingJobRepository
{
    Task<CvJobMatchScores?> GetByIdempotencyKeyForUpdateAsync(
        Guid userId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    void AddPending(CvJobMatchScores job);
}
