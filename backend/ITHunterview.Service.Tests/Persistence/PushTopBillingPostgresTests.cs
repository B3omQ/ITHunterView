using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.FeatureUsage;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.UseCase;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ITHunterview.Service.Tests.Persistence;

[AttributeUsage(AttributeTargets.Method)]
internal sealed class PushTopPostgresFactAttribute : FactAttribute
{
    public PushTopPostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable(MatchingScanPostgresFixture.AdminConnectionEnvironmentVariable)))
        {
            Skip = $"Set {MatchingScanPostgresFixture.AdminConnectionEnvironmentVariable} to run Push Top PostgreSQL integration tests.";
        }
    }
}

[Collection(MatchingScanPostgresCollection.Name)]
public sealed class PushTopBillingPostgresTests
{
    private const string FeatureKey = "PushTop";
    private readonly MatchingScanPostgresFixture _fixture;

    public PushTopBillingPostgresTests(MatchingScanPostgresFixture fixture) => _fixture = fixture;

    [PushTopPostgresFact]
    public async Task BILL_PG_01_WhenQuotaConfirmedAndAvailable_ConsumesQuotaWithoutWalletDeduction()
    {
        await using var context = _fixture.CreateContext();
        var userId = await SeedAsync(context, 10_000, 7_200, quotaLimit: 1);

        var result = await CreateSut(context).TryConsumePushTopAsync(
            userId, Guid.NewGuid().ToString(), QuotaExpectation());

        result.ChargedCoins.Should().Be(0);
        await AssertStateAsync(context, userId, 10_000, subLogs: 1, coinLogs: 0, debits: 0);
    }

    [PushTopPostgresFact]
    public async Task BILL_PG_02_WhenQuotaConfirmedButExhausted_RejectsConflictWithoutWalletMutationOrFallback()
    {
        await using var context = _fixture.CreateContext();
        var userId = await SeedAsync(context, 10_000, 7_200, quotaLimit: 1, quotaUsed: 1);

        var action = () => CreateSut(context).TryConsumePushTopAsync(
            userId, Guid.NewGuid().ToString(), QuotaExpectation());

        await action.Should().ThrowAsync<InvalidOperationException>();
        await AssertStateAsync(context, userId, 10_000, subLogs: 1, coinLogs: 0, debits: 0);
    }

    [PushTopPostgresFact]
    public async Task BILL_PG_03_WhenCoinConfirmedAndPriceMatches_DeductsExactCoinAmountAndLogsActivity()
    {
        await using var context = _fixture.CreateContext();
        var userId = await SeedAsync(context, 10_000, 7_200);

        var result = await CreateSut(context).TryConsumePushTopAsync(
            userId, Guid.NewGuid().ToString(), CoinExpectation(7_200));

        result.ChargedCoins.Should().Be(7_200);
        await AssertStateAsync(context, userId, 2_800, subLogs: 0, coinLogs: 1, debits: 1, debitAmount: -7_200);
    }

    [PushTopPostgresFact]
    public async Task BILL_PG_04_WhenCoinConfirmedButPriceChanged_RejectsConflictWithoutDeduction()
    {
        await using var context = _fixture.CreateContext();
        var userId = await SeedAsync(context, 10_000, 7_200);

        var action = () => CreateSut(context).TryConsumePushTopAsync(
            userId, Guid.NewGuid().ToString(), CoinExpectation(5_000));

        await action.Should().ThrowAsync<InvalidOperationException>();
        await AssertStateAsync(context, userId, 10_000, subLogs: 0, coinLogs: 0, debits: 0);
    }

    [PushTopPostgresFact]
    public async Task BILL_PG_05_WhenCoinConfirmedButQuotaBecomesAvailable_ConsumesFreeQuotaInstead()
    {
        await using var context = _fixture.CreateContext();
        var userId = await SeedAsync(context, 10_000, 7_200, quotaLimit: 1);

        var result = await CreateSut(context).TryConsumePushTopAsync(
            userId, Guid.NewGuid().ToString(), CoinExpectation(7_200));

        result.ChargedCoins.Should().Be(0);
        await AssertStateAsync(context, userId, 10_000, subLogs: 1, coinLogs: 0, debits: 0);
    }

    [PushTopPostgresFact]
    public async Task BILL_PG_06_WhenZeroPriceConfirmedAndConfigured_SucceedsWithZeroCharge()
    {
        await using var context = _fixture.CreateContext();
        var userId = await SeedAsync(context, 10_000, coinCost: 0);

        var result = await CreateSut(context).TryConsumePushTopAsync(
            userId, Guid.NewGuid().ToString(), CoinExpectation(0));

        result.ChargedCoins.Should().Be(0);
        await AssertStateAsync(context, userId, 10_000, subLogs: 0, coinLogs: 0, debits: 0);
    }

    private async Task<Guid> SeedAsync(
        ITHunterviewContext context,
        int walletBalance,
        int coinCost,
        int? quotaLimit = null,
        int quotaUsed = 0)
    {
        var seed = await _fixture.SeedGraphAsync();
        var now = DateTime.UtcNow;
        context.UserWallets.Add(new UserWallets
        {
            Id = Guid.NewGuid(), UserId = seed.RecruiterUserId, Balance = walletBalance, UpdatedAt = now
        });

        var feature = await context.CoinFeatures.SingleOrDefaultAsync(item => item.FeatureKey == FeatureKey);
        if (feature is null)
        {
            context.CoinFeatures.Add(new CoinFeatures
            {
                FeatureKey = FeatureKey,
                CoinCost = coinCost,
                Description = "Push Top integration-test price",
                UpdatedAt = now
            });
        }
        else
        {
            feature.CoinCost = coinCost;
            feature.UpdatedAt = now;
        }

        if (quotaLimit.HasValue)
        {
            var subscription = new Subscriptions
            {
                Name = $"PushTop test {Guid.NewGuid():N}",
                DurationDays = 30,
                FeaturesConfig = $"{{\"pushTopLimit\":{quotaLimit.Value}}}",
                Status = SubscriptionStatus.ACTIVE,
                CreatedAt = now
            };
            context.Subscriptions.Add(subscription);
            await context.SaveChangesAsync();
            context.UserSubscriptions.Add(new UserSubscriptions
            {
                Id = Guid.NewGuid(),
                UserId = seed.RecruiterUserId,
                SubId = subscription.Id,
                StartDate = now.AddDays(-1),
                EndDate = now.AddDays(29),
                Status = UserSubscriptionStatus.ACTIVE
            });
        }

        for (var index = 0; index < quotaUsed; index++)
        {
            context.UserActivityLogs.Add(UserActivityLogs.Create(
                seed.RecruiterUserId,
                "recruiter",
                ActivityLogCategory.DATA_MUTATION,
                "integration@test.invalid",
                "ConsumeFeature:PushTop:Sub",
                ActivityLogStatus.SUCCESS,
                "127.0.0.1",
                "PushTopBillingPostgresTests"));
        }

        await context.SaveChangesAsync();
        return seed.RecruiterUserId;
    }

    private static CandidateFeatureUsageUseCase CreateSut(ITHunterviewContext context) => new(
        context,
        Mock.Of<ISystemConfigRepository>(),
        Mock.Of<IFeatureUsageReservationRepository>());

    private static FeatureConsumptionExpectation QuotaExpectation() => new(
        FeatureConsumptionPaymentMethod.SUBSCRIPTION_QUOTA, null);

    private static FeatureConsumptionExpectation CoinExpectation(int cost) => new(
        FeatureConsumptionPaymentMethod.COIN, cost);

    private static async Task AssertStateAsync(
        ITHunterviewContext context,
        Guid userId,
        int balance,
        int subLogs,
        int coinLogs,
        int debits,
        int? debitAmount = null)
    {
        context.ChangeTracker.Clear();
        var wallet = await context.UserWallets.SingleAsync(item => item.UserId == userId);
        wallet.Balance.Should().Be(balance);
        (await context.UserActivityLogs.CountAsync(item =>
            item.UserId == userId && item.Action == "ConsumeFeature:PushTop:Sub")).Should().Be(subLogs);
        (await context.UserActivityLogs.CountAsync(item =>
            item.UserId == userId && item.Action == "ConsumeFeature:PushTop:Coin")).Should().Be(coinLogs);

        var persistedDebits = await context.CreditTransactions
            .Where(item => item.WalletId == wallet.Id && item.TransactionType == CreditTransactionType.DEDUCT)
            .ToListAsync();
        persistedDebits.Should().HaveCount(debits);
        if (debitAmount.HasValue)
        {
            persistedDebits.Should().ContainSingle().Which.Amount.Should().Be(debitAmount.Value);
        }
    }
}
