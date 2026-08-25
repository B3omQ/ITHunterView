using System;
using ITHunterview.Service.DTOs.FeatureUsage;

namespace ITHunterview.Service.UseCase;

internal static class PushTopBillingExpectationPolicy
{
    public static void Enforce(
        FeatureConsumptionExpectation expectation,
        FeatureConsumptionPaymentMethod resolvedMethod,
        int liveFeatureCoinCost)
    {
        ArgumentNullException.ThrowIfNull(expectation);

        if (expectation.ExpectedPaymentMethod == FeatureConsumptionPaymentMethod.SUBSCRIPTION_QUOTA &&
            expectation.ExpectedCoinCost.HasValue)
        {
            throw new ArgumentException(
                "ExpectedCoinCost must be null for SUBSCRIPTION_QUOTA.",
                nameof(expectation));
        }

        if (expectation.ExpectedPaymentMethod == FeatureConsumptionPaymentMethod.COIN &&
            (!expectation.ExpectedCoinCost.HasValue || expectation.ExpectedCoinCost.Value < 0))
        {
            throw new ArgumentException(
                "ExpectedCoinCost must be a non-negative integer for COIN.",
                nameof(expectation));
        }

        // If quota became available after the modal was opened, use the free benefit.
        if (resolvedMethod == FeatureConsumptionPaymentMethod.SUBSCRIPTION_QUOTA)
        {
            return;
        }

        if (expectation.ExpectedPaymentMethod == FeatureConsumptionPaymentMethod.SUBSCRIPTION_QUOTA)
        {
            throw new InvalidOperationException(
                "Subscription quota is exhausted. Please confirm payment with coins.");
        }

        if (liveFeatureCoinCost != expectation.ExpectedCoinCost!.Value)
        {
            throw new InvalidOperationException(
                $"Giá dịch vụ đã thay đổi từ {expectation.ExpectedCoinCost.Value:N0} Coin thành {liveFeatureCoinCost:N0} Coin. Vui lòng xác nhận lại giao dịch.");
        }
    }
}
