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
    }
}
