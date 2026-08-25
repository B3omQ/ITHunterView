using FluentAssertions;
using ITHunterview.Service.DTOs.FeatureUsage;
using ITHunterview.Service.UseCase;

namespace ITHunterview.Service.Tests.UseCase;

public sealed class PushTopBillingExpectationPolicyTests
{
    [Fact]
    public void Enforce_WhenQuotaWasConfirmedAndIsAvailable_AllowsQuota()
    {
        var expectation = QuotaExpectation();

        var action = () => PushTopBillingExpectationPolicy.Enforce(
            expectation, FeatureConsumptionPaymentMethod.SUBSCRIPTION_QUOTA, liveFeatureCoinCost: 0);

        action.Should().NotThrow();
    }

    [Fact]
    public void Enforce_WhenQuotaWasConfirmedButIsExhausted_RejectsCoinFallback()
    {
        var action = () => PushTopBillingExpectationPolicy.Enforce(
            QuotaExpectation(), FeatureConsumptionPaymentMethod.COIN, liveFeatureCoinCost: 7200);

        action.Should().Throw<InvalidOperationException>().WithMessage("*quota*");
    }

    [Fact]
    public void Enforce_WhenCoinWasConfirmedButQuotaBecameAvailable_AllowsFreeQuota()
    {
        var action = () => PushTopBillingExpectationPolicy.Enforce(
            CoinExpectation(7200), FeatureConsumptionPaymentMethod.SUBSCRIPTION_QUOTA, liveFeatureCoinCost: 0);

        action.Should().NotThrow();
    }

    [Theory]
    [InlineData(7200)]
    [InlineData(0)]
    public void Enforce_WhenCoinWasConfirmedAndPriceIsUnchanged_AllowsConfiguredCost(int coinCost)
    {
        var action = () => PushTopBillingExpectationPolicy.Enforce(
            CoinExpectation(coinCost), FeatureConsumptionPaymentMethod.COIN, liveFeatureCoinCost: coinCost);

        action.Should().NotThrow();
    }

    [Fact]
    public void Enforce_WhenCoinPriceChanged_RejectsTheStaleConfirmation()
    {
        var action = () => PushTopBillingExpectationPolicy.Enforce(
            CoinExpectation(7200), FeatureConsumptionPaymentMethod.COIN, liveFeatureCoinCost: 9000);

        action.Should().Throw<InvalidOperationException>().WithMessage("*thay đổi*");
    }

    [Fact]
    public void Enforce_WhenQuotaExpectationContainsCoinCost_RejectsInvalidInternalContract()
    {
        var expectation = new FeatureConsumptionExpectation(
            FeatureConsumptionPaymentMethod.SUBSCRIPTION_QUOTA, 7200);

        var action = () => PushTopBillingExpectationPolicy.Enforce(
            expectation, FeatureConsumptionPaymentMethod.SUBSCRIPTION_QUOTA, liveFeatureCoinCost: 0);

        action.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-1)]
    public void Enforce_WhenCoinExpectationHasInvalidCost_RejectsInvalidInternalContract(int? expectedCost)
    {
        var expectation = new FeatureConsumptionExpectation(
            FeatureConsumptionPaymentMethod.COIN, expectedCost);

        var action = () => PushTopBillingExpectationPolicy.Enforce(
            expectation, FeatureConsumptionPaymentMethod.COIN, liveFeatureCoinCost: 7200);

        action.Should().Throw<ArgumentException>();
    }

    private static FeatureConsumptionExpectation QuotaExpectation() => new(
        FeatureConsumptionPaymentMethod.SUBSCRIPTION_QUOTA, null);

    private static FeatureConsumptionExpectation CoinExpectation(int cost) => new(
        FeatureConsumptionPaymentMethod.COIN, cost);
}
