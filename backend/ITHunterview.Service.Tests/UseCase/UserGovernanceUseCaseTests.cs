using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.UserGovernance;
using ITHunterview.Service.Interface.Infrastructure;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.UseCase;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace ITHunterview.Service.Tests.UseCase
{
    public class UserGovernanceUseCaseTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<ITokenRepository> _tokenRepositoryMock;
        private readonly Mock<IMemoryCache> _cacheMock;
        private readonly Mock<IAuditLogQueue> _auditLogQueueMock;
        private readonly Mock<IActorProvider> _actorProviderMock;
        private readonly UserGovernanceUseCase _sut;

        public UserGovernanceUseCaseTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _tokenRepositoryMock = new Mock<ITokenRepository>();
            _cacheMock = new Mock<IMemoryCache>();
            _auditLogQueueMock = new Mock<IAuditLogQueue>();
            _actorProviderMock = new Mock<IActorProvider>();

            _cacheMock.Setup(m => m.Remove(It.IsAny<object>()));

            _sut = new UserGovernanceUseCase(
                _userRepositoryMock.Object,
                _tokenRepositoryMock.Object,
                _cacheMock.Object,
                _auditLogQueueMock.Object,
                _actorProviderMock.Object);
        }

        // --- UpdateUserStatusAsync Tests ---

        [Fact]
        public async Task UpdateUserStatusAsync_WhenTargetUserDoesNotExist_ReturnsErrorResponse()
        {
            // Arrange
            var targetUserId = Guid.NewGuid();
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(targetUserId))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _sut.UpdateUserStatusAsync(targetUserId, new UpdateUserStatusDto());

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("User does not exist.");
        }

        [Fact]
        public async Task UpdateUserStatusAsync_WhenTargetIsAdmin_ReturnsErrorResponseAndLogsSecurityFail()
        {
            // Arrange
            var targetUserId = Guid.NewGuid();
            var adminUser = new User { Id = targetUserId, RoleId = (int)SystemRole.Admin, Email = "admin@test.com" };
            
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(targetUserId))
                .ReturnsAsync(adminUser);

            // Act
            var result = await _sut.UpdateUserStatusAsync(targetUserId, new UpdateUserStatusDto());

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Administrator (Admin) accounts cannot have their active status modified.");
            _auditLogQueueMock.Verify(x => x.TryEnqueue(It.Is<UserActivityLogs>(l => l.ActionCategory == ActivityLogCategory.SECURITY && l.Status == ActivityLogStatus.FAIL)), Times.Once);
        }

        [Fact]
        public async Task UpdateUserStatusAsync_WhenActorAttemptsSelfLockout_ReturnsErrorResponse()
        {
            // Arrange
            var targetUserId = Guid.NewGuid();
            var targetUser = new User { Id = targetUserId, RoleId = (int)SystemRole.Candidate };
            
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(targetUserId))
                .ReturnsAsync(targetUser);
            _actorProviderMock.Setup(x => x.ActorUserId).Returns(targetUserId);

            // Act
            var result = await _sut.UpdateUserStatusAsync(targetUserId, new UpdateUserStatusDto());

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("You cannot update your own active status.");
        }

        [Fact]
        public async Task UpdateUserStatusAsync_WhenNewStatusIsSameAsOld_ReturnsErrorResponse()
        {
            // Arrange
            var targetUserId = Guid.NewGuid();
            var targetUser = new User { Id = targetUserId, RoleId = (int)SystemRole.Candidate, Status = UserStatus.ACTIVE };
            
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(targetUserId))
                .ReturnsAsync(targetUser);
            _actorProviderMock.Setup(x => x.ActorUserId).Returns(Guid.NewGuid());

            var dto = new UpdateUserStatusDto { Status = UserStatus.ACTIVE };

            // Act
            var result = await _sut.UpdateUserStatusAsync(targetUserId, dto);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("User is already in this status.");
        }

        [Fact]
        public async Task UpdateUserStatusAsync_WhenTargetIsBanned_UpdatesStatusRevokesTokensAndLogsSuccess()
        {
            // Arrange
            var targetUserId = Guid.NewGuid();
            var targetUser = new User { Id = targetUserId, RoleId = (int)SystemRole.Candidate, Status = UserStatus.ACTIVE };
            
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(targetUserId))
                .ReturnsAsync(targetUser);
            _actorProviderMock.Setup(x => x.ActorUserId).Returns(Guid.NewGuid());

            var dto = new UpdateUserStatusDto { Status = UserStatus.BANNED, Reason = "Vi phạm nội quy" };

            // Act
            var result = await _sut.UpdateUserStatusAsync(targetUserId, dto);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Updated active status to BANNED successfully.");
            targetUser.Status.Should().Be(UserStatus.BANNED);
            targetUser.DeactiveAt.Should().NotBeNull();
            
            _userRepositoryMock.Verify(x => x.UpdateUserAsync(targetUser), Times.Once);
            _cacheMock.Verify(x => x.Remove($"user-status-{targetUserId}"), Times.Once);
            _tokenRepositoryMock.Verify(x => x.RevokeAllUserRefreshTokensAsync(targetUserId), Times.Once);
            _auditLogQueueMock.Verify(x => x.TryEnqueue(It.Is<UserActivityLogs>(l => l.ActionCategory == ActivityLogCategory.SECURITY && l.Status == ActivityLogStatus.SUCCESS)), Times.Once);
        }

        [Fact]
        public async Task UpdateUserStatusAsync_WhenTargetIsActivated_UpdatesStatusWithoutRevokingTokens()
        {
            // Arrange
            var targetUserId = Guid.NewGuid();
            var targetUser = new User { Id = targetUserId, RoleId = (int)SystemRole.Candidate, Status = UserStatus.BANNED, DeactiveAt = DateTime.UtcNow };
            
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(targetUserId))
                .ReturnsAsync(targetUser);
            _actorProviderMock.Setup(x => x.ActorUserId).Returns(Guid.NewGuid());

            var dto = new UpdateUserStatusDto { Status = UserStatus.ACTIVE, Reason = "Khôi phục" };

            // Act
            var result = await _sut.UpdateUserStatusAsync(targetUserId, dto);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Updated active status to ACTIVE successfully.");
            targetUser.Status.Should().Be(UserStatus.ACTIVE);
            targetUser.DeactiveAt.Should().BeNull();
            
            _userRepositoryMock.Verify(x => x.UpdateUserAsync(targetUser), Times.Once);
            _tokenRepositoryMock.Verify(x => x.RevokeAllUserRefreshTokensAsync(It.IsAny<Guid>()), Times.Never);
            _auditLogQueueMock.Verify(x => x.TryEnqueue(It.Is<UserActivityLogs>(l => l.ActionCategory == ActivityLogCategory.DATA_MUTATION && l.Status == ActivityLogStatus.SUCCESS)), Times.Once);
        }

        // --- CreateStaffAccountAsync Tests ---

        [Fact]
        public async Task CreateStaffAccountAsync_WhenActorIsNotAdmin_ReturnsErrorResponse()
        {
            // Arrange
            _actorProviderMock.Setup(x => x.ActorRole).Returns("staff");

            // Act
            var result = await _sut.CreateStaffAccountAsync(new CreateStaffDto());

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Only Administrator (Admin) can create staff accounts.");
            result.Data.Should().Be(Guid.Empty);
        }

        [Fact]
        public async Task CreateStaffAccountAsync_WhenEmailAlreadyExists_ReturnsErrorResponseAndLogsSecurityFail()
        {
            // Arrange
            _actorProviderMock.Setup(x => x.ActorRole).Returns("admin");
            var dto = new CreateStaffDto { Email = "exist@mail.com", Password = "ValidPass123!" };

            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(dto.Email))
                .ReturnsAsync(new User());

            // Act
            var result = await _sut.CreateStaffAccountAsync(dto);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Email already exists in the system.");
            result.Data.Should().Be(Guid.Empty);
            _auditLogQueueMock.Verify(x => x.TryEnqueue(It.Is<UserActivityLogs>(l => l.ActionCategory == ActivityLogCategory.SECURITY && l.Status == ActivityLogStatus.FAIL)), Times.Once);
        }

        [Fact]
        public async Task CreateStaffAccountAsync_WhenDtoIsNull_ReturnsErrorResponse()
        {
            // Arrange
            _actorProviderMock.Setup(x => x.ActorRole).Returns("admin");

            // Act
            var result = await _sut.CreateStaffAccountAsync(null!);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Invalid data.");
        }

        [Fact]
        public async Task CreateStaffAccountAsync_WhenValidRequest_CreatesUserAndReturnsUserId()
        {
            // Arrange
            _actorProviderMock.Setup(x => x.ActorRole).Returns("admin");
            var dto = new CreateStaffDto { Email = "newstaff@mail.com", Password = "ValidPass123!" };

            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(dto.Email))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _sut.CreateStaffAccountAsync(dto);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().BeNull();
            result.Data.Should().NotBe(Guid.Empty);

            _userRepositoryMock.Verify(x => x.AddUserAsync(It.Is<User>(u => 
                u.Email == dto.Email && 
                u.RoleId == (int)SystemRole.Staff && 
                u.Status == UserStatus.ACTIVE && 
                u.PasswordHash != null)), Times.Once);
            
            _auditLogQueueMock.Verify(x => x.TryEnqueue(It.Is<UserActivityLogs>(l => l.ActionCategory == ActivityLogCategory.DATA_MUTATION && l.Status == ActivityLogStatus.SUCCESS)), Times.Once);
        }

        // --- GetPagedUsersAsync Tests ---

        [Fact]
        public async Task GetPagedUsersAsync_WhenPageAndSizeAreZeroOrNegative_UsesDefaultValues()
        {
            // Arrange
            int page = 0;
            int pageSize = 0;

            _userRepositoryMock.Setup(x => x.GetPagedUsersAsync(1, 10, null, null, null))
                .ReturnsAsync((new List<User>(), 0));

            // Act
            var result = await _sut.GetPagedUsersAsync(page, pageSize, null, null, null);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Page.Should().Be(1);
            result.Data.PageSize.Should().Be(10);
            result.Data.Total.Should().Be(0);
            result.Data.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task GetPagedUsersAsync_WhenValidParameters_ReturnsPagedResultCorrectly()
        {
            // Arrange
            var users = new List<User>
            {
                new User { Id = Guid.NewGuid(), Email = "admin@mail.com", RoleId = (int)SystemRole.Admin, Status = UserStatus.ACTIVE },
                new User { Id = Guid.NewGuid(), Email = "staff@mail.com", RoleId = (int)SystemRole.Staff, Status = UserStatus.ACTIVE }
            };

            _userRepositoryMock.Setup(x => x.GetPagedUsersAsync(2, 10, null, null, null))
                .ReturnsAsync((users, 25));

            // Act
            var result = await _sut.GetPagedUsersAsync(2, 10, null, null, null);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Page.Should().Be(2);
            result.Data.PageSize.Should().Be(10);
            result.Data.Total.Should().Be(25);
            result.Data.Items.Count.Should().Be(2);
            result.Data.Items[0].RoleName.Should().Be("admin");
            result.Data.Items[0].FullName.Should().Be("Admin Account");
            result.Data.Items[1].RoleName.Should().Be("staff");
            result.Data.Items[1].FullName.Should().Be("Staff Account");
        }

        [Fact]
        public async Task GetPagedUsersAsync_WhenListContainsCandidate_MapsRoleAndFullNameCorrectly()
        {
            // Arrange
            var candidateId = Guid.NewGuid();
            var users = new List<User>
            {
                new User 
                { 
                    Id = candidateId, 
                    Email = "cand@mail.com", 
                    RoleId = (int)SystemRole.Candidate, 
                    CandidateProfile = new CandidateProfiles { FirstName = "John", LastName = "Doe" }
                }
            };

            _userRepositoryMock.Setup(x => x.GetPagedUsersAsync(1, 10, null, (int)SystemRole.Candidate, null))
                .ReturnsAsync((users, 1));

            // Act
            var result = await _sut.GetPagedUsersAsync(1, 10, null, (int)SystemRole.Candidate, null);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Items.Count.Should().Be(1);
            result.Data.Items[0].RoleName.Should().Be("candidate");
            result.Data.Items[0].FullName.Should().Be("John Doe");
        }

        [Fact]
        public async Task GetPagedUsersAsync_WhenListContainsRecruiter_MapsRoleAndFullNameCorrectly()
        {
            // Arrange
            var recruiterId = Guid.NewGuid();
            var users = new List<User>
            {
                new User 
                { 
                    Id = recruiterId, 
                    Email = "rec@mail.com", 
                    RoleId = (int)SystemRole.Recruiter, 
                    Status = UserStatus.BANNED,
                    RecruiterProfile = new RecruiterProfiles { FullName = "Jane Smith" }
                }
            };

            _userRepositoryMock.Setup(x => x.GetPagedUsersAsync(1, 10, null, null, UserStatus.BANNED))
                .ReturnsAsync((users, 1));

            // Act
            var result = await _sut.GetPagedUsersAsync(1, 10, null, null, UserStatus.BANNED);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Items.Count.Should().Be(1);
            result.Data.Items[0].RoleName.Should().Be("recruiter");
            result.Data.Items[0].FullName.Should().Be("Jane Smith");
            result.Data.Items[0].Status.Should().Be(UserStatus.BANNED);
        }
    }
}
