using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using ITHunterview.Service.DTOs.Auth;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ITHunterview.WebAPI.Tests.Integration
{
    public class AuthIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly Mock<IAuthUseCase> _authUseCaseMock;

        public AuthIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _authUseCaseMock = new Mock<IAuthUseCase>();
            
            // Cấu hình lại Test Server, thay thế IAuthUseCase thật bằng Mock
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => _authUseCaseMock.Object);
                });
            });

            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task AUTH_IT_01_GoogleAuth_AuthorizedEmail_ReturnsOk()
        {
            // Arrange
            var request = new GoogleAuthRequestDto { IdToken = "valid_google_token" };
            var expectedResponse = ResponseBase<LoginResponseDto>.SuccessResult(new LoginResponseDto 
            { 
                AccessToken = "access_token",
                RefreshToken = "refresh_token"
            });

            _authUseCaseMock.Setup(u => u.GoogleAuthAsync(It.IsAny<GoogleAuthRequestDto>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/google", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ResponseBase<LoginResponseDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.AccessToken.Should().Be("access_token");
        }

        [Fact]
        public async Task AUTH_IT_02_GoogleAuth_UnauthorizedEmail_ReturnsBadRequest()
        {
            // Arrange
            var request = new GoogleAuthRequestDto { IdToken = "unauthorized_token" };
            var expectedResponse = ResponseBase<LoginResponseDto>.Fail("Account not authorized");

            _authUseCaseMock.Setup(u => u.GoogleAuthAsync(It.IsAny<GoogleAuthRequestDto>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/google", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var result = await response.Content.ReadFromJsonAsync<ResponseBase<LoginResponseDto>>();
            result!.Success.Should().BeFalse();
            result.Message.Should().Be("Account not authorized");
        }

        [Fact]
        public async Task AUTH_IT_05_RefreshToken_ValidToken_ReturnsOk()
        {
            // Arrange
            var request = new RefreshTokenRequestDto { RefreshToken = "valid_refresh_token" };
            var expectedResponse = ResponseBase<LoginResponseDto>.SuccessResult(new LoginResponseDto 
            { 
                AccessToken = "new_access_token",
                RefreshToken = "new_refresh_token"
            });

            _authUseCaseMock.Setup(u => u.RefreshTokenAsync(It.IsAny<RefreshTokenRequestDto>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/refresh-token", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ResponseBase<LoginResponseDto>>();
            result!.Success.Should().BeTrue();
            result.Data.AccessToken.Should().Be("new_access_token");
        }

        [Fact]
        public async Task AUTH_IT_07_RefreshToken_ExpiredToken_ReturnsUnauthorized()
        {
            // Arrange
            var request = new RefreshTokenRequestDto { RefreshToken = "expired_refresh_token" };
            var expectedResponse = ResponseBase<LoginResponseDto>.Fail("Refresh token expired");

            _authUseCaseMock.Setup(u => u.RefreshTokenAsync(It.IsAny<RefreshTokenRequestDto>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/refresh-token", request);

            // Assert
            // Trong controller: return result.Success ? Ok(result) : Unauthorized(result);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task AUTH_IT_08_Logout_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            var request = new LogoutRequestDto { RefreshToken = "refresh_token" };

            // Act - Không set Authorization header
            var response = await _client.PostAsJsonAsync("/api/auth/logout", request);

            // Assert
            // Controller yêu cầu [Authorize]
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
