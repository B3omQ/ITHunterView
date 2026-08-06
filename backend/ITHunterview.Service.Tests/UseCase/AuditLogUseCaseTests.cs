using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.AuditLogs;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.UseCase;
using Moq;
using Xunit;

namespace ITHunterview.Service.Tests.UseCase
{
    public class AuditLogUseCaseTests
    {
        private readonly Mock<IAuditLogRepository> _auditLogRepositoryMock;
        private readonly AuditLogUseCase _sut;

        public AuditLogUseCaseTests()
        {
            _auditLogRepositoryMock = new Mock<IAuditLogRepository>();
            _sut = new AuditLogUseCase(_auditLogRepositoryMock.Object);
        }

        [Fact]
        public async Task GetPagedAuditLogsAsync_NormalizesPaginationAndQueriesRepo()
        {
            // Arrange
            var logs = new List<UserActivityLogs>
            {
                new UserActivityLogs
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    ActorEmail = "test@example.com",
                    ActionCategory = ActivityLogCategory.SYSTEM,
                    OperationType = "CREATE",
                    Status = ActivityLogStatus.SUCCESS,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _auditLogRepositoryMock.Setup(r => r.GetPagedActivityLogsAsync(
                    1, 10, null, null, null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null, null))
                .ReturnsAsync((logs, 1));

            // Act
            var result = await _sut.GetPagedAuditLogsAsync(0, -5, null, null, null, null, null, null, null);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items[0].ActorEmail.Should().Be("test@example.com");
        }

        [Fact]
        public async Task PurgeAuditLogsAsync_WhenDaysLessThanOne_ReturnsErrorResponse()
        {
            // Act
            var result = await _sut.PurgeAuditLogsAsync(0);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("minimum retention days must be 1 day");
        }

        [Fact]
        public async Task PurgeAuditLogsAsync_WhenValid_PurgesLogsOlderThanCutoff()
        {
            // Arrange
            _auditLogRepositoryMock.Setup(r => r.PurgeActivityLogsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(15);

            // Act
            var result = await _sut.PurgeAuditLogsAsync(30);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().Be(15);
            result.Message.Should().Contain("Successfully purged 15 audit log records");
        }
    }
}
