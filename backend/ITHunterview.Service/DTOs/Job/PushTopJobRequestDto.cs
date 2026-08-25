using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ITHunterview.Service.DTOs.FeatureUsage;

namespace ITHunterview.Service.DTOs.Job;

public sealed class PushTopJobRequestDto : IValidatableObject
{
    public FeatureConsumptionPaymentMethod? ExpectedPaymentMethod { get; init; }
    public int? ExpectedCoinCost { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!ExpectedPaymentMethod.HasValue)
        {
            yield return new ValidationResult(
                "ExpectedPaymentMethod is required.",
                [nameof(ExpectedPaymentMethod)]);
            yield break;
        }

        if (ExpectedPaymentMethod == FeatureConsumptionPaymentMethod.SUBSCRIPTION_QUOTA &&
            ExpectedCoinCost.HasValue)
        {
            yield return new ValidationResult(
                "ExpectedCoinCost must be null for SUBSCRIPTION_QUOTA.",
                [nameof(ExpectedCoinCost)]);
        }

        if (ExpectedPaymentMethod == FeatureConsumptionPaymentMethod.COIN &&
            (!ExpectedCoinCost.HasValue || ExpectedCoinCost.Value < 0))
        {
            yield return new ValidationResult(
                "ExpectedCoinCost must be a non-negative integer for COIN.",
                [nameof(ExpectedCoinCost)]);
        }
    }
}
