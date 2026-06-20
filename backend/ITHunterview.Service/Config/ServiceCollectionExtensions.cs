using ITHunterview.Service.Config;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.Service;
using ITHunterview.Service.UseCase;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ITHunterview.Service.Config
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Repositories — Auth
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ITokenRepository, TokenRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IEmailVerificationRepository, EmailVerificationRepository>();
            services.AddScoped<IPasswordResetRepository, PasswordResetRepository>();

            services.AddScoped<ICvRepository, CvRepository>();
            services.AddScoped<ICompanyRepository, CompanyRepository>();
            services.AddScoped<ISkillRepository, SkillRepository>();
            services.AddScoped<ISkillCategoryRepository, SkillCategoryRepository>();
            services.AddScoped<IMajorRepository, MajorRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();

            // Repositories — Candidate Profile
            services.AddScoped<ICandidateProfileRepository, CandidateProfileRepository>();
            services.AddScoped<ICandidateSkillRepository, CandidateSkillRepository>();
            services.AddScoped<ICandidateExperienceRepository, CandidateExperienceRepository>();
            services.AddScoped<ICandidateEducationRepository, CandidateEducationRepository>();
            services.AddScoped<ICandidateCertificationRepository, CandidateCertificationRepository>();


            // Application Services
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IGoogleAuthService, GoogleAuthService>();
            services.AddScoped<IFileUploadService, CloudinaryService>();

            // Use Cases — Auth
            services.AddScoped<IAuthUseCase, AuthUseCase>();

            services.AddScoped<ICvUseCase, CvUseCase>();
            services.AddScoped<ICompanyUseCase, CompanyUseCase>();
            services.AddScoped<ISkillUseCase, SkillUseCase>();
            services.AddScoped<IMajorUseCase, MajorUseCase>();
            services.AddScoped<IUserGovernanceUseCase, UserGovernanceUseCase>();
            services.AddScoped<IAuditLogUseCase, AuditLogUseCase>();


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
