using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Auth;
using ITHunterview.Service.Interface.Infrastructure;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.UseCase;
using ITHunterview.Service.Utils;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ITHunterview.Service.Tests.UseCase
{
    public class AuthUseCaseTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<ITokenRepository> _tokenRepositoryMock;
        private readonly Mock<IRoleRepository> _roleRepositoryMock;
        private readonly Mock<IEmailVerificationRepository> _emailVerificationRepositoryMock;
        private readonly Mock<IPasswordResetRepository> _passwordResetRepositoryMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<IGoogleAuthService> _googleAuthServiceMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<IAuditLogQueue> _auditLogQueueMock;
        private readonly Mock<IActorProvider> _actorProviderMock;

        private readonly AuthUseCase _authUseCase;

        public AuthUseCaseTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _tokenRepositoryMock = new Mock<ITokenRepository>();
            _roleRepositoryMock = new Mock<IRoleRepository>();
            _emailVerificationRepositoryMock = new Mock<IEmailVerificationRepository>();
            _passwordResetRepositoryMock = new Mock<IPasswordResetRepository>();
            _emailServiceMock = new Mock<IEmailService>();
            _googleAuthServiceMock = new Mock<IGoogleAuthService>();
            _configurationMock = new Mock<IConfiguration>();
            _auditLogQueueMock = new Mock<IAuditLogQueue>();
            _actorProviderMock = new Mock<IActorProvider>();

            // Setup common mocks
            _actorProviderMock.Setup(x => x.IpAddress).Returns("127.0.0.1");
            _actorProviderMock.Setup(x => x.UserAgent).Returns("TestAgent");

            // Setup Configuration for Tokens
            var jwtConfigMock = new Mock<IConfigurationSection>();
            jwtConfigMock.Setup(x => x["Issuer"]).Returns("TestIssuer");
            jwtConfigMock.Setup(x => x["Audience"]).Returns("TestAudience");
            jwtConfigMock.Setup(x => x["Secret"]).Returns("ThisIsAVerySecretKeyThatIsAtLeast32BytesLong");
            jwtConfigMock.Setup(x => x["AccessTokenExpirationMinutes"]).Returns("60");
            jwtConfigMock.Setup(x => x["RefreshTokenExpirationDays"]).Returns("7");
            _configurationMock.Setup(x => x.GetSection("JwtSettings")).Returns(jwtConfigMock.Object);

            _authUseCase = new AuthUseCase(
                _userRepositoryMock.Object,
                _tokenRepositoryMock.Object,
                _roleRepositoryMock.Object,
                _emailVerificationRepositoryMock.Object,
                _passwordResetRepositoryMock.Object,
                _emailServiceMock.Object,
                _googleAuthServiceMock.Object,
                _configurationMock.Object,
                _auditLogQueueMock.Object,
                _actorProviderMock.Object
            );
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsSuccessWithTokens()
        {
            // Arrange
            var request = new LoginRequestDto { Email = "test@example.com", Password = "Password123!" };
            var role = new Roles { Id = 1, Name = "candidate" };
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                PasswordHash = PasswordHasher.HashPassword("Password123!"),
                Status = UserStatus.ACTIVE,
                Role = role
            };

            _userRepositoryMock.Setup(x => x.GetUserWithRoleByEmailAsync(request.Email))
                .ReturnsAsync(user);

            // Act
            var result = await _authUseCase.LoginAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Logged in successfully.");
            result.Data.Should().NotBeNull();
            result.Data.AccessToken.Should().NotBeNullOrEmpty();
            result.Data.RefreshToken.Should().NotBeNullOrEmpty();
            result.Data.Email.Should().Be("test@example.com");
            result.Data.Role.Should().Be("candidate");
        }

        [Fact]
        public async Task LoginAsync_InvalidEmail_ReturnsError()
        {
            // Arrange
            var request = new LoginRequestDto { Email = "wrong@example.com", Password = "Password123!" };
            _userRepositoryMock.Setup(x => x.GetUserWithRoleByEmailAsync(request.Email))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _authUseCase.LoginAsync(request);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Invalid email or password.");
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword_ReturnsError()
        {
            // Arrange
            var request = new LoginRequestDto { Email = "test@example.com", Password = "WrongPassword!" };
            var user = new User
            {
                Email = "test@example.com",
                PasswordHash = PasswordHasher.HashPassword("Password123!"),
                Status = UserStatus.ACTIVE
            };

            _userRepositoryMock.Setup(x => x.GetUserWithRoleByEmailAsync(request.Email))
                .ReturnsAsync(user);

            // Act
            var result = await _authUseCase.LoginAsync(request);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Invalid email or password.");
        }

        [Fact]
        public async Task LoginAsync_BannedUser_ReturnsError()
        {
            // Arrange
            var request = new LoginRequestDto { Email = "test@example.com", Password = "Password123!" };
            var user = new User
            {
                Email = "test@example.com",
                PasswordHash = PasswordHasher.HashPassword("Password123!"),
                Status = UserStatus.BANNED
            };

            _userRepositoryMock.Setup(x => x.GetUserWithRoleByEmailAsync(request.Email))
                .ReturnsAsync(user);

            // Act
            var result = await _authUseCase.LoginAsync(request);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Your account has been banned.");
        }

        [Fact]
        public async Task LoginAsync_PendingVerification_ReturnsError()
        {
            // Arrange
            var request = new LoginRequestDto { Email = "test@example.com", Password = "Password123!" };
            var user = new User
            {
                Email = "test@example.com",
                PasswordHash = PasswordHasher.HashPassword("Password123!"),
                Status = UserStatus.PENDING_VERIFICATION
            };

            _userRepositoryMock.Setup(x => x.GetUserWithRoleByEmailAsync(request.Email))
                .ReturnsAsync(user);

            // Act
            var result = await _authUseCase.LoginAsync(request);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Please verify your email before logging in.");
        }

        // ─── REGISTER ASYNC TESTS ────────────────────────────────────────────────

        [Fact]
        public async Task RegisterAsync_ValidCandidate_ReturnsSuccessAndSendsEmail()
        {
            // Arrange
            var request = new RegisterRequestDto { Email = "new@example.com", Password = "Password123!", RoleType = "candidate" };
            var role = new Roles { Id = 1, Name = "candidate" };

            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(request.Email))
                .ReturnsAsync((User?)null);
            _roleRepositoryMock.Setup(x => x.GetByNameAsync("candidate"))
                .ReturnsAsync(role);

            // Act
            var result = await _authUseCase.RegisterAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Contain("Registration successful");
            
            _userRepositoryMock.Verify(x => x.AddUserAsync(It.Is<User>(u => 
                u.Email == "new@example.com" && 
                u.RoleId == 1 && 
                u.Status == UserStatus.PENDING_VERIFICATION &&
                u.CandidateProfile != null)), Times.Once);
            
            _emailVerificationRepositoryMock.Verify(x => x.AddTokenAsync(It.IsAny<EmailVerificationTokens>()), Times.Once);
            _emailServiceMock.Verify(x => x.SendVerificationEmailAsync(request.Email, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_InvalidRole_ReturnsError()
        {
            // Arrange
            var request = new RegisterRequestDto { Email = "new@example.com", Password = "Password123!", RoleType = "admin" };

            // Act
            var result = await _authUseCase.RegisterAsync(request);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Invalid role");
        }

        [Fact]
        public async Task RegisterAsync_ExistingEmailPendingVerification_ResendsEmailAndReturnsSuccess()
        {
            // Arrange
            var request = new RegisterRequestDto { Email = "existing@example.com", Password = "Password123!", RoleType = "candidate" };
            var existingUser = new User { Id = Guid.NewGuid(), Email = "existing@example.com", Status = UserStatus.PENDING_VERIFICATION };

            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(request.Email))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _authUseCase.RegisterAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().StartWith("PENDING_VERIFICATION|");
            
            _emailVerificationRepositoryMock.Verify(x => x.AddTokenAsync(It.IsAny<EmailVerificationTokens>()), Times.Once);
            _emailServiceMock.Verify(x => x.SendVerificationEmailAsync(request.Email, It.IsAny<string>()), Times.Once);
            _userRepositoryMock.Verify(x => x.AddUserAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_ExistingEmailActive_ReturnsError()
        {
            // Arrange
            var request = new RegisterRequestDto { Email = "existing@example.com", Password = "Password123!", RoleType = "candidate" };
            var existingUser = new User { Id = Guid.NewGuid(), Email = "existing@example.com", Status = UserStatus.ACTIVE };

            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(request.Email))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _authUseCase.RegisterAsync(request);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Email is already in use.");
            
            _userRepositoryMock.Verify(x => x.AddUserAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_RoleNotFound_ReturnsError()
        {
            // Arrange
            var request = new RegisterRequestDto { Email = "new@example.com", Password = "Password123!", RoleType = "candidate" };

            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(request.Email))
                .ReturnsAsync((User?)null);
            _roleRepositoryMock.Setup(x => x.GetByNameAsync("candidate"))
                .ReturnsAsync((Roles?)null);

            // Act
            var result = await _authUseCase.RegisterAsync(request);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("The system has not configured this role");
        }

        // ─── CHANGE PASSWORD ASYNC TESTS ─────────────────────────────────────────

        [Fact]
        public async Task ChangePasswordAsync_UserExistsAndValidPassword_ReturnsSuccessAndRevokesTokens()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new ChangePasswordRequestDto { CurrentPassword = "OldPassword123!", NewPassword = "NewPassword123!" };
            var user = new User { Id = userId, PasswordHash = PasswordHasher.HashPassword("OldPassword123!") };

            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _authUseCase.ChangePasswordAsync(userId, request);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Changed password successfully. Please login again.");
            
            _userRepositoryMock.Verify(x => x.UpdateUserAsync(It.Is<User>(u => u.Id == userId)), Times.Once);
            _tokenRepositoryMock.Verify(x => x.RevokeAllUserRefreshTokensAsync(userId), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_UserDoesNotExist_ReturnsError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new ChangePasswordRequestDto { CurrentPassword = "OldPassword123!", NewPassword = "NewPassword123!" };

            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);

            // Act
            var result = await _authUseCase.ChangePasswordAsync(userId, request);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("User does not exist.");
            
            _userRepositoryMock.Verify(x => x.UpdateUserAsync(It.IsAny<User>()), Times.Never);
            _tokenRepositoryMock.Verify(x => x.RevokeAllUserRefreshTokensAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task ChangePasswordAsync_InvalidCurrentPassword_ReturnsError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new ChangePasswordRequestDto { CurrentPassword = "WrongPassword!", NewPassword = "NewPassword123!" };
            var user = new User { Id = userId, PasswordHash = PasswordHasher.HashPassword("OldPassword123!") };

            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _authUseCase.ChangePasswordAsync(userId, request);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Current password is incorrect.");
            
            _userRepositoryMock.Verify(x => x.UpdateUserAsync(It.IsAny<User>()), Times.Never);
            _tokenRepositoryMock.Verify(x => x.RevokeAllUserRefreshTokensAsync(It.IsAny<Guid>()), Times.Never);
        }
    }
}
