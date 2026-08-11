using System;

namespace ITHunterview.Service.DTOs.FeatureUsage;

/// <summary>
/// The durable entitlement reservation returned for one matching submission.
/// It is intentionally separate from <see cref="FeatureConsumptionResult"/>
/// because a reservation is created before a coin deduction is captured.
/// </summary>
public sealed record FeatureReservationResult(
    Guid ReservationId,
    Guid ReferenceId,
    string FeatureKey,
    string Source,
    string Status,
    int CoinAmount,
    Guid? DeductTransactionId);
