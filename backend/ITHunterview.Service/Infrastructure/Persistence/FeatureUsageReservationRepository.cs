using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence;

/// <summary>
/// Persistence-only operations for billing reservations. Transactions are
/// deliberately owned by the use case so reservation and match-job writes can
/// commit or roll back as one unit.
/// </summary>
public sealed class FeatureUsageReservationRepository : IFeatureUsageReservationRepository
{
    private readonly ITHunterviewContext _context;

    public FeatureUsageReservationRepository(ITHunterviewContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<FeatureUsageReservations?> GetByReferenceForUpdateAsync(
        Guid referenceId,
        CancellationToken cancellationToken = default)
    {
        if (IsInMemoryProvider())
        {
            return await _context.FeatureUsageReservations
                .Where(x => x.ReferenceId == referenceId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return await _context.FeatureUsageReservations
            .FromSqlInterpolated($"SELECT * FROM feature_usage_reservations WHERE reference_id = {referenceId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<FeatureUsageReservations?> GetByIdForUpdateAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        if (IsInMemoryProvider())
        {
            return await _context.FeatureUsageReservations
                .Where(x => x.Id == reservationId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return await _context.FeatureUsageReservations
            .FromSqlInterpolated($"SELECT * FROM feature_usage_reservations WHERE id = {reservationId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<int> CountActiveAsync(
        Guid userId,
        string featureKey,
        DateTime startUtc,
        DateTime endUtc,
        Guid? excludeReferenceId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.FeatureUsageReservations
            .Where(x => x.UserId == userId
                        && x.FeatureKey == featureKey
                        && (x.Status == "Reserved" || x.Status == "Captured")
                        && x.CreatedAt >= startUtc
                        && x.CreatedAt <= endUtc);

        if (excludeReferenceId.HasValue)
        {
            query = query.Where(x => x.ReferenceId != excludeReferenceId.Value);
        }

        return query.CountAsync(cancellationToken);
    }

    public void Add(FeatureUsageReservations reservation)
    {
        _context.FeatureUsageReservations.Add(reservation);
    }

    private bool IsInMemoryProvider()
        => string.Equals(_context.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal);

}
