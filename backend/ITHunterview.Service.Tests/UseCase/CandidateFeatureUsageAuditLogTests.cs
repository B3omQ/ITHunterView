using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.UseCase;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ITHunterview.Service.Tests.UseCase;

public sealed class CandidateFeatureUsageAuditLogTests
{
    [Theory]
    [InlineData("ExtendJob", true, "ConsumeFeature:ExtendJob:Sub")]
    [InlineData("ExtendJob", false, "ConsumeFeature:ExtendJob:Coin")]
    [InlineData("PushTop", true, "ConsumeFeature:PushTop:Sub")]
    [InlineData("PushTop", false, "ConsumeFeature:PushTop:Coin")]
    [InlineData("UnlockCv", true, "ConsumeFeature:UnlockCv:Sub")]
    [InlineData("UnlockCv", false, "ConsumeFeature:UnlockCv:Coin")]
    public async Task RecordFeatureUsageLogAsync_WithReference_WritesValidJsonSnapshot(
        string featureKey,
        bool fromSubscription,
        string expectedAction)
    {
        var (sut, addedLogs) = CreateSut();

        var method = typeof(CandidateFeatureUsageUseCase).GetMethod(
            "RecordFeatureUsageLogAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();

        var userId = Guid.NewGuid();
        var referenceId = Guid.NewGuid().ToString();
        var invocation = method!.Invoke(
            sut,
            new object?[] { userId, featureKey, referenceId, fromSubscription });
        invocation.Should().BeAssignableTo<Task<Guid?>>();
        var logId = await (Task<Guid?>)invocation!;

        var log = addedLogs.Should().ContainSingle().Which;
        logId.Should().Be(log.Id);
        log.UserId.Should().Be(userId);
        log.ActorRole.Should().Be("recruiter");
        log.ActionCategory.Should().Be(ActivityLogCategory.DATA_MUTATION);
        log.ActorEmail.Should().Be("recruiter@ithunterview.com");
        log.Action.Should().Be(expectedAction);
        log.Status.Should().Be(ActivityLogStatus.SUCCESS);
        log.IpAddress.Should().Be("127.0.0.1");
        log.UserAgent.Should().Be("System/FeatureUsage");
        log.TableName.Should().Be("JobPostings");
        log.OperationType.Should().Be(featureKey);
        log.SnapshotDiff.Should().NotBeNullOrWhiteSpace();

        using var snapshot = JsonDocument.Parse(log.SnapshotDiff!);
        snapshot.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
        snapshot.RootElement.EnumerateObject()
            .Select(property => property.Name)
            .Should().Equal("referenceId");
        snapshot.RootElement.GetProperty("referenceId").GetString()
            .Should().Be(referenceId);
    }

    [Fact]
    public async Task RecordFeatureUsageLogAsync_WithoutReference_LeavesSnapshotNull()
    {
        var (sut, addedLogs) = CreateSut();

        var method = typeof(CandidateFeatureUsageUseCase).GetMethod(
            "RecordFeatureUsageLogAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();

        var invocation = method!.Invoke(
            sut,
            new object?[] { Guid.NewGuid(), "ExtendJob", null, true });
        invocation.Should().BeAssignableTo<Task<Guid?>>();
        await (Task<Guid?>)invocation!;

        addedLogs.Should().ContainSingle()
            .Which.SnapshotDiff.Should().BeNull();
    }

    private static (CandidateFeatureUsageUseCase Sut, List<UserActivityLogs> AddedLogs) CreateSut()
    {
        var addedLogs = new List<UserActivityLogs>();
        var activityLogs = new Mock<DbSet<UserActivityLogs>>();
        activityLogs
            .Setup(set => set.Add(It.IsAny<UserActivityLogs>()))
            .Callback<UserActivityLogs>(addedLogs.Add);

        var context = new Mock<ITHunterviewContext>(new DbContextOptions<ITHunterviewContext>());
        context.SetupGet(db => db.UserActivityLogs).Returns(activityLogs.Object);

        var sut = new CandidateFeatureUsageUseCase(
            context.Object,
            Mock.Of<ISystemConfigRepository>(),
            Mock.Of<IFeatureUsageReservationRepository>());
        return (sut, addedLogs);
    }
}
