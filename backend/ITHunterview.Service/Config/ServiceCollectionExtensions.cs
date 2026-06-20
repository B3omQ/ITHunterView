using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.Service;
using ITHunterview.Service.UseCase;
using Microsoft.Extensions.DependencyInjection;

namespace ITHunterview.Service.Config
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Repositories — Auth
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ITokenRepository, TokenRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IEmailVerificationRepository, EmailVerificationRepository>();
            services.AddScoped<IPasswordResetRepository, PasswordResetRepository>();

            // Repositories — Candidate Profile
            services.AddScoped<ICandidateProfileRepository, CandidateProfileRepository>();
            services.AddScoped<ICandidateSkillRepository, CandidateSkillRepository>();
            services.AddScoped<ICandidateExperienceRepository, CandidateExperienceRepository>();
            services.AddScoped<ICandidateEducationRepository, CandidateEducationRepository>();
            services.AddScoped<ICandidateCertificationRepository, CandidateCertificationRepository>();

            // Application Services
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IGoogleAuthService, GoogleAuthService>();

            // Use Cases — Auth
            services.AddScoped<IAuthUseCase, AuthUseCase>();

            // Use Cases — Candidate Profile
            services.AddScoped<ICandidateProfileUseCase, CandidateProfileUseCase>();
            services.AddScoped<ICandidateSkillUseCase, CandidateSkillUseCase>();
            services.AddScoped<ICandidateExperienceUseCase, CandidateExperienceUseCase>();
            services.AddScoped<ICandidateEducationUseCase, CandidateEducationUseCase>();
            services.AddScoped<ICandidateCertificationUseCase, CandidateCertificationUseCase>();

            return services;
        }
    }
}
