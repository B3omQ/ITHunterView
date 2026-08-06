using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Notification;
using ITHunterview.Service.Hubs;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Infrastructure;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.UseCase;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ITHunterview.Service.Tests.UseCase
{
    public class NotificationUseCaseTests : IDisposable
    {
        private sealed class TestContext : ITHunterviewContext
        {
            public TestContext(DbContextOptions<ITHunterviewContext> options) : base(options) { }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                var allowed = new HashSet<Type> { typeof(Notifications) };

                foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                             .Where(t => !allowed.Contains(t.ClrType))
                             .Select(t => t.ClrType)
                             .Distinct()
                             .ToList())
                {
                    modelBuilder.Ignore(entityType);
                }

                modelBuilder.Entity<Notifications>(entity =>
                {
                    entity.HasKey(n => n.Id);
                });
            }
        }

        private readonly Mock<INotificationRepository> _notificationRepositoryMock;
        private readonly Mock<IHubContext<NotificationHub>> _hubContextMock;
        private readonly Mock<IHubClients> _hubClientsMock;
        private readonly Mock<IClientProxy> _clientProxyMock;
        private readonly Mock<INotificationQueue> _notificationQueueMock;
        private readonly TestContext _context;
        private readonly NotificationUseCase _sut;

        public NotificationUseCaseTests()
        {
            _notificationRepositoryMock = new Mock<INotificationRepository>();
            _hubContextMock = new Mock<IHubContext<NotificationHub>>();
            _hubClientsMock = new Mock<IHubClients>();
            _clientProxyMock = new Mock<IClientProxy>();
            _notificationQueueMock = new Mock<INotificationQueue>();

            _hubContextMock.Setup(h => h.Clients).Returns(_hubClientsMock.Object);
            _hubClientsMock.Setup(c => c.Groups(It.IsAny<IReadOnlyList<string>>())).Returns(_clientProxyMock.Object);
            _clientProxyMock.Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
                .Returns(Task.CompletedTask);

            var options = new DbContextOptionsBuilder<ITHunterviewContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new TestContext(options);

            _sut = new NotificationUseCase(
                _notificationRepositoryMock.Object,
                _context,
                _hubContextMock.Object,
                _notificationQueueMock.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task CreateSystemWideNotificationAsync_QueuesSystemNotification()
        {
            // Arrange
            var dto = new CreateSystemNotificationDto
            {
                Title = "System Maintenance",
                Message = "Scheduled downtime at midnight.",
                Type = NotificationType.SYSTEM
            };

            // Act
            var result = await _sut.CreateSystemWideNotificationAsync(dto);

            // Assert
            result.Should().BeTrue();
            _notificationQueueMock.Verify(q => q.QueueSystemNotificationAsync(dto), Times.Once);
        }

        [Fact]
        public async Task CreateNotificationAsync_SavesNotificationToRepository()
        {
            // Arrange
            var dto = new CreateNotificationDto
            {
                UserId = Guid.NewGuid(),
                Title = "Job Match Ready",
                Message = "Your match report is ready.",
                Type = NotificationType.APPLICATION
            };

            // Act
            var result = await _sut.CreateNotificationAsync(dto);

            // Assert
            result.Should().BeTrue();
            _notificationRepositoryMock.Verify(r => r.AddNotificationAsync(It.Is<Notifications>(n =>
                n.UserId == dto.UserId && n.Title == dto.Title)), Times.Once);
        }

        [Fact]
        public async Task MarkAsReadAsync_WhenFoundAndUserMatches_UpdatesIsReadToTrue()
        {
            // Arrange
            var notifId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var notification = new Notifications
            {
                Id = notifId,
                UserId = userId,
                Title = "Test",
                Message = "Test message",
                IsRead = false
            };

            _notificationRepositoryMock.Setup(r => r.GetNotificationByIdAsync(notifId))
                .ReturnsAsync(notification);

            // Act
            var result = await _sut.MarkAsReadAsync(notifId, userId);

            // Assert
            result.Should().BeTrue();
            notification.IsRead.Should().BeTrue();
            _notificationRepositoryMock.Verify(r => r.UpdateNotificationAsync(notification), Times.Once);
        }
    }
}
