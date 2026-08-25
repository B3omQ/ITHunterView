using ITHunterview.Domain.Enums;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;

namespace ITHunterview.WebAPI.Tests.Infrastructure;

public sealed class PushTopWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<IJobPostingsUseCase> JobPostingsUseCaseMock { get; } = new(MockBehavior.Strict);
    public Mock<IUserUseCase> UserUseCaseMock { get; } = new(MockBehavior.Strict);
    public Mock<IUserGovernanceUseCase> UserGovernanceUseCaseMock { get; } = new(MockBehavior.Strict);

    public PushTopWebApplicationFactory()
    {
        // Default active user status so UserStatusCheckMiddleware does not block valid test callers
        UserGovernanceUseCaseMock
            .Setup(u => u.GetUserStatusAsync(PushTopTestAuthenticationHandler.ValidRecruiterUserId))
            .ReturnsAsync(UserStatus.ACTIVE);

        UserGovernanceUseCaseMock
            .Setup(u => u.GetUserStatusAsync(PushTopTestAuthenticationHandler.ValidCandidateUserId))
            .ReturnsAsync(UserStatus.ACTIVE);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove background workers to prevent database interactions during testing
            var hostedServiceDescriptors = services.Where(d => d.ServiceType == typeof(IHostedService)).ToList();
            foreach (var descriptor in hostedServiceDescriptors)
            {
                services.Remove(descriptor);
            }

            // Replace UseCases with strict mocks
            services.RemoveAll<IJobPostingsUseCase>();
            services.AddSingleton(JobPostingsUseCaseMock.Object);

            services.RemoveAll<IUserUseCase>();
            services.AddSingleton(UserUseCaseMock.Object);

            services.RemoveAll<IUserGovernanceUseCase>();
            services.AddSingleton(UserGovernanceUseCaseMock.Object);

            // Replace Authentication with PushTopTestAuthenticationHandler
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = PushTopTestAuthenticationHandler.SchemeName;
                options.DefaultChallengeScheme = PushTopTestAuthenticationHandler.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, PushTopTestAuthenticationHandler>(
                PushTopTestAuthenticationHandler.SchemeName,
                _ => { });
        });
    }

    public HttpClient CreateClientForIdentity(string? testIdentityHeader = null)
    {
        var client = CreateClient();
        if (!string.IsNullOrEmpty(testIdentityHeader))
        {
            client.DefaultRequestHeaders.Add(PushTopTestAuthenticationHandler.IdentityHeader, testIdentityHeader);
        }
        return client;
    }
}
