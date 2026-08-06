using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Hubs;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.UseCase;
using Microsoft.AspNetCore.SignalR;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace ITHunterview.Service.Tests.UseCase
{
    public class SubscriptionAdminUseCaseTests
    {
        private readonly Mock<ISubscriptionRepository> _subRepoMock;
        private readonly Mock<IHubContext<NotificationHub>> _hubContextMock;
        private readonly SubscriptionAdminUseCase _useCase;

        public SubscriptionAdminUseCaseTests()
        {
            _subRepoMock = new Mock<ISubscriptionRepository>();
            _hubContextMock = new Mock<IHubContext<NotificationHub>>();
            _useCase = new SubscriptionAdminUseCase(_subRepoMock.Object, _hubContextMock.Object);
        }

        [Fact]
        public async Task GetPagedSubscriptionsAsync_ReturnsPagedResult()
        {
            // Arrange
            var page = 1;
            var pageSize = 10;
            var items = new List<Subscriptions>
            {
                new Subscriptions { Id = 1, Name = "Basic", Price = 100, DurationDays = 30, FeaturesConfig = "{}" }
            };

            _subRepoMock.Setup(repo => repo.GetPagedAsync(It.IsAny<string>(), It.IsAny<SubscriptionStatus?>(), page, pageSize))
                .ReturnsAsync((items, 1));
            
            _subRepoMock.Setup(repo => repo.IsSubscriptionUsedAsync(It.IsAny<int>()))
                .ReturnsAsync(false);

            // Act
            var result = await _useCase.GetPagedSubscriptionsAsync(null, null, page, pageSize);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Total.Should().Be(1);
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items[0].Name.Should().Be("Basic");
        }

        [Fact]
        public async Task GetSubscriptionByIdAsync_ValidId_ReturnsDto()
        {
            // Arrange
            var id = 1;
            var subscription = new Subscriptions { Id = id, Name = "Premium", Price = 500, DurationDays = 30, FeaturesConfig = "{}" };

            _subRepoMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(subscription);
            _subRepoMock.Setup(repo => repo.IsSubscriptionUsedAsync(id)).ReturnsAsync(true);

            // Act
            var result = await _useCase.GetSubscriptionByIdAsync(id);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be("Premium");
            result.Data.IsUsed.Should().BeTrue();
        }

        [Fact]
        public async Task GetSubscriptionByIdAsync_InvalidId_ReturnsError()
        {
            // Arrange
            var id = 999;
            _subRepoMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync((Subscriptions)null!);

            // Act
            var result = await _useCase.GetSubscriptionByIdAsync(id);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("không tồn tại");
        }

        [Fact]
        public async Task CreateSubscriptionAsync_ValidInput_ReturnsSuccess()
        {
            // Arrange
            var dto = new ITHunterview.Service.DTOs.Subscription.CreateSubscriptionDto
            {
                Name = "Basic",
                Price = 100,
                DurationDays = 30,
                FeaturesConfig = new ITHunterview.Service.DTOs.Subscription.FeaturesConfigDto()
            };
            var userId = Guid.NewGuid();

            _subRepoMock.Setup(repo => repo.CreateAsync(It.IsAny<Subscriptions>())).ReturnsAsync(new Subscriptions());

            // Act
            var result = await _useCase.CreateSubscriptionAsync(dto, userId);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be("Basic");
            result.Data.Status.Should().Be(SubscriptionStatus.INACTIVE); // Default INACTIVE
            _hubContextMock.Verify(h => h.Clients.All.SendCoreAsync("ReceivePricingUpdate", It.IsAny<object[]>(), default), Times.Once);
        }

        [Fact]
        public async Task UpdateSubscriptionAsync_IsUsed_CannotChangeCoreFields_ReturnsError()
        {
            // Arrange
            var id = 1;
            var userId = Guid.NewGuid();
            var dto = new ITHunterview.Service.DTOs.Subscription.UpdateSubscriptionDto
            {
                Name = "Basic V2",
                Price = 200, // Changed from 100
                DurationDays = 30,
                FeaturesConfig = new ITHunterview.Service.DTOs.Subscription.FeaturesConfigDto()
            };

            var existingSub = new Subscriptions { Id = id, Price = 100, DurationDays = 30, FeaturesConfig = "{}" };
            var transactionMock = new Mock<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>();

            _subRepoMock.Setup(repo => repo.BeginTransactionAsync()).ReturnsAsync(transactionMock.Object);
            _subRepoMock.Setup(repo => repo.GetByIdForUpdateAsync(id)).ReturnsAsync(existingSub);
            _subRepoMock.Setup(repo => repo.IsSubscriptionUsedAsync(id)).ReturnsAsync(true); // IsUsed = true

            // Act
            var result = await _useCase.UpdateSubscriptionAsync(id, dto, userId);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Không thể sửa đổi giá, thời hạn");
        }

        [Fact]
        public async Task UpdateSubscriptionAsync_NotUsed_ChangesAllowed_ReturnsSuccess()
        {
            // Arrange
            var id = 1;
            var userId = Guid.NewGuid();
            var dto = new ITHunterview.Service.DTOs.Subscription.UpdateSubscriptionDto
            {
                Name = "Basic V2",
                Price = 200, 
                DurationDays = 30,
                FeaturesConfig = new ITHunterview.Service.DTOs.Subscription.FeaturesConfigDto()
            };

            var existingSub = new Subscriptions { Id = id, Price = 100, DurationDays = 30, FeaturesConfig = "{}" };
            var transactionMock = new Mock<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>();

            _subRepoMock.Setup(repo => repo.BeginTransactionAsync()).ReturnsAsync(transactionMock.Object);
            _subRepoMock.Setup(repo => repo.GetByIdForUpdateAsync(id)).ReturnsAsync(existingSub);
            _subRepoMock.Setup(repo => repo.IsSubscriptionUsedAsync(id)).ReturnsAsync(false); // IsUsed = false

            // Act
            var result = await _useCase.UpdateSubscriptionAsync(id, dto, userId);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Price.Should().Be(200); // Should be updated
            transactionMock.Verify(t => t.CommitAsync(default), Times.Once);
            _hubContextMock.Verify(h => h.Clients.All.SendCoreAsync("ReceivePricingUpdate", It.IsAny<object[]>(), default), Times.Once);
        }
    }
}
