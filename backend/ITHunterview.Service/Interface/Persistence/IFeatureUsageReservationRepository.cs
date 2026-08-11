using System;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Persistence;

public interface IFeatureUsageReservationRepository
{
    Task<FeatureUsageReservations?> GetByReferenceForUpdateAsync(
        Guid referenceId,
        CancellationToken cancellationToken = default);

    Task<FeatureUsageReservations?> GetByIdForUpdateAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default);

    Task<int> CountActiveAsync(
        Guid userId,
        string featureKey,
        DateTime startUtc,
        DateTime endUtc,
        Guid? excludeReferenceId = null,
        CancellationToken cancellationToken = default);

    void Add(FeatureUsageReservations reservation);
}
