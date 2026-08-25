using System.Text.Json.Serialization;

namespace ITHunterview.Service.DTOs.FeatureUsage;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FeatureConsumptionPaymentMethod
{
    SUBSCRIPTION_QUOTA,
    COIN
}

public sealed record FeatureConsumptionExpectation(
    FeatureConsumptionPaymentMethod ExpectedPaymentMethod,
    int? ExpectedCoinCost);
