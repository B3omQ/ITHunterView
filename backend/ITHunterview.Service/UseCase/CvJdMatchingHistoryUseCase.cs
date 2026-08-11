using System;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase;

public sealed class CvJdMatchingHistoryUseCase : ICvJdMatchingHistoryUseCase
{
    private readonly ICvJdMatchingHistoryRepository _repository;

    public CvJdMatchingHistoryUseCase(ICvJdMatchingHistoryRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Task<HideMatchHistoryResult> HideAsync(
        Guid jobId,
        Guid userId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
        => _repository.HideAsync(jobId, userId, utcNow, cancellationToken);
}
