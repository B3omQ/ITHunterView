using FluentAssertions;
using ITHunterview.Service.DTOs.Auth;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace ITHunterview.WebAPI.Tests.Controllers
{
    public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly Mock<IAuthUseCase> _authUseCaseMock;

        public AuthControllerTests(WebApplicationFactory<Program> factory)
        {
            _authUseCaseMock = new Mock<IAuthUseCase>();

            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing IAuthUseCase if it exists and add the mocked one
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAuthUseCase));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }
                    services.AddSingleton(_authUseCaseMock.Object);
                });
            });
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOk()
        {
            // Arrange
            var request = new LoginRequestDto { Email = "test@example.com", Password = "Password123!" };
            var responseData = new LoginResponseDto
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                Email = "test@example.com",
                Role = "candidate"
            };
            var expectedResponse = new ResponseBase<LoginResponseDto>(responseData, "Logged in successfully.");

            _authUseCaseMock.Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>()))
                .ReturnsAsync(expectedResponse);

            var client = _factory.CreateClient();

            // Act
            var response = await client.PostAsJsonAsync("/api/auth/login", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ResponseBase<LoginResponseDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.AccessToken.Should().Be("access-token");
        }

        [Fact]
        public async Task Login_MissingFields_ReturnsBadRequest()
        {
            // Arrange
            var request = new { Email = "", Password = "" }; // Invalid format
            var client = _factory.CreateClient();

            // Act
            var response = await client.PostAsJsonAsync("/api/auth/login", request);

            // Assert
            var content = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest, $"Because API returned: {content}");
            // Validation errors are handled by ASP.NET Core MVC ModelState, not AuthUseCase
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsBadRequestWithErrorMessage()
        {
            // Arrange
            var request = new LoginRequestDto { Email = "wrong@example.com", Password = "Password123!" };
            var expectedResponse = new ResponseBase<LoginResponseDto>("Invalid email or password.");

            _authUseCaseMock.Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>()))
                .ReturnsAsync(expectedResponse);

            var client = _factory.CreateClient();

            // Act
            var response = await client.PostAsJsonAsync("/api/auth/login", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var result = await response.Content.ReadFromJsonAsync<ResponseBase<LoginResponseDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().Be("Invalid email or password.");
        }
    }
}
