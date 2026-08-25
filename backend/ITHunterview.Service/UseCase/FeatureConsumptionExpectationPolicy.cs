using System;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.FeatureUsage;

namespace ITHunterview.Service.UseCase;

public static class FeatureConsumptionExpectationPolicy
{
    public static void Enforce(
        FeatureConsumptionExpectation? expectation,
        FeatureConsumptionPaymentMethod resolvedMethod,
        int liveFeatureCoinCost,
        int candidateQuotaRemaining)
    {
        if (expectation == null)
        {
            return;
        }

        if (expectation.ExpectedPaymentMethod == FeatureConsumptionPaymentMethod.SUBSCRIPTION_QUOTA)
        {
            if (resolvedMethod != FeatureConsumptionPaymentMethod.SUBSCRIPTION_QUOTA)
            {
                throw new InvalidOperationException(
                    "Subscription quota is exhausted. Please confirm payment with coins.");
            }
        }
        else if (expectation.ExpectedPaymentMethod == FeatureConsumptionPaymentMethod.COIN)
        {
            if (resolvedMethod == FeatureConsumptionPaymentMethod.SUBSCRIPTION_QUOTA && candidateQuotaRemaining > 0)
            {
                throw new InvalidOperationException(
                    "Subscription quota is now available. Please confirm consumption using quota.");
            }

            if (expectation.ExpectedCoinCost.HasValue &&
                liveFeatureCoinCost != expectation.ExpectedCoinCost.Value)
            {
                throw new InvalidOperationException(
                    $"Giá dịch vụ đã thay đổi từ {expectation.ExpectedCoinCost.Value:N0} xu thành {liveFeatureCoinCost:N0} xu. Vui lòng xác nhận lại giao dịch.");
            }
        }
    }
}
