using System;
using System.Threading.Tasks;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.FeatureUsage;
using ITHunterview.Service.DTOs.Job;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.UseCase;
using ITHunterview.Service.Utils;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ITHunterview.Service.Tests.UseCase;

public sealed class PushTopBillingExpectationContractTests
{
    public enum BillingDecisionOutcome
    {
        ConsumeSubscriptionQuota,
        ChargeExactCoin,
        AllowZeroPriceFree,
        RejectStaleQuotaConflict,
        RejectChangedPriceConflict,
        RejectInsufficientBalance
    }

    public static BillingDecisionOutcome EvaluateBillingDecision(
        string confirmedMethod,
        int? confirmedCoinCost,
        bool isQuotaAvailable,
        int currentBackendCoinCost,
        long currentWalletBalance)
    {
        if (confirmedMethod == "SUBSCRIPTION_QUOTA")
        {
            if (isQuotaAvailable)
            {
                return BillingDecisionOutcome.ConsumeSubscriptionQuota;
            }
            // If user expected quota but quota is exhausted, reject with conflict - NEVER fallback silently to coin
            return BillingDecisionOutcome.RejectStaleQuotaConflict;
        }

        if (confirmedMethod == "COIN")
        {
            // If quota became available unexpectedly, allow free use
            if (isQuotaAvailable)
            {
                return BillingDecisionOutcome.ConsumeSubscriptionQuota;
            }

            // If backend price changed compared to confirmed cost, reject with conflict
            if (confirmedCoinCost != currentBackendCoinCost)
            {
                return BillingDecisionOutcome.RejectChangedPriceConflict;
            }

            if (currentBackendCoinCost == 0)
            {
                return BillingDecisionOutcome.AllowZeroPriceFree;
            }

            if (currentWalletBalance < currentBackendCoinCost)
            {
                return BillingDecisionOutcome.RejectInsufficientBalance;
            }

            return BillingDecisionOutcome.ChargeExactCoin;
        }

        throw new ArgumentException($"Unknown payment method: {confirmedMethod}");
    }

    [Theory]
    [InlineData("SUBSCRIPTION_QUOTA", null, true, 7200, 0, BillingDecisionOutcome.ConsumeSubscriptionQuota)]
    [InlineData("SUBSCRIPTION_QUOTA", null, false, 7200, 50000, BillingDecisionOutcome.RejectStaleQuotaConflict)]
    [InlineData("COIN", 7200, true, 7200, 50000, BillingDecisionOutcome.ConsumeSubscriptionQuota)]
    [InlineData("COIN", 7200, false, 7200, 7200, BillingDecisionOutcome.ChargeExactCoin)]
    [InlineData("COIN", 7200, false, 9000, 50000, BillingDecisionOutcome.RejectChangedPriceConflict)]
    [InlineData("COIN", 0, false, 0, 0, BillingDecisionOutcome.AllowZeroPriceFree)]
    [InlineData("COIN", 7200, false, 7200, 7199, BillingDecisionOutcome.RejectInsufficientBalance)]
    public void BILL_DEC_DecisionMatrix_ProducesExactOutcomeWithoutSilentOverchargeOrFallback(
        string confirmedMethod,
        int? confirmedCost,
        bool quotaAvailable,
        int currentCost,
        long balance,
        BillingDecisionOutcome expectedOutcome)
    {
        var outcome = EvaluateBillingDecision(confirmedMethod, confirmedCost, quotaAvailable, currentCost, balance);
        outcome.Should().Be(expectedOutcome);
    }
}
