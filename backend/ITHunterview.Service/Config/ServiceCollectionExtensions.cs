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
            services.AddScoped<IJobPostingRepository, JobPostingRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IEmailVerificationRepository, EmailVerificationRepository>();
            services.AddScoped<IPasswordResetRepository, PasswordResetRepository>();
            services.AddScoped<IJobCategoryRepository, JobCategoryRepository>();
            services.AddScoped<ISkillRepository, SkillRepository>();
            services.AddScoped<IJobApplicationRepository, JobApplicationRepository>();

            services.AddScoped<ICvRepository, CvRepository>();
            services.AddScoped<ICompanyRepository, CompanyRepository>();
            services.AddScoped<ISkillRepository, SkillRepository>();
            services.AddScoped<ISkillCategoryRepository, SkillCategoryRepository>();
            services.AddScoped<IMajorRepository, MajorRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
            services.AddScoped<ISystemConfigRepository, SystemConfigRepository>();

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
            services.AddScoped<IJobPostingsUseCase, JobPostingsUseCase>();
            services.AddScoped<IJobCategoriesUseCase, JobCategoriesUseCase>();
            services.AddScoped<ISkillsUseCase, SkillsUseCase>();
            services.AddScoped<IUserUseCase, UserUseCase>();
            services.AddScoped<IJobApplicationUseCase, JobApplicationUseCase>();

            services.AddScoped<ICvUseCase, CvUseCase>();
            services.AddScoped<ICompanyUseCase, CompanyUseCase>();
            services.AddScoped<ISkillUseCase, SkillUseCase>();
            services.AddScoped<IMajorUseCase, MajorUseCase>();
            services.AddScoped<IUserGovernanceUseCase, UserGovernanceUseCase>();
            services.AddScoped<IAuditLogUseCase, AuditLogUseCase>();
            services.AddScoped<ISubscriptionAdminUseCase, SubscriptionAdminUseCase>();
            services.AddScoped<ICoinConfigUseCase, CoinConfigUseCase>();
            services.AddScoped<ICandidateFeatureUsageUseCase, CandidateFeatureUsageUseCase>();
            services.AddScoped<IWalletUseCase, WalletUseCase>();


            // Use Cases — Candidate Profile
            services.AddScoped<ICandidateProfileUseCase, CandidateProfileUseCase>();
            services.AddScoped<ICandidateSkillUseCase, CandidateSkillUseCase>();
            services.AddScoped<ICandidateExperienceUseCase, CandidateExperienceUseCase>();
            services.AddScoped<ICandidateEducationUseCase, CandidateEducationUseCase>();
            services.AddScoped<ICandidateCertificationUseCase, CandidateCertificationUseCase>();

            // Job Search & Saved Jobs
            services.AddScoped<IJobSearchRepository, JobSearchRepository>();
            services.AddScoped<IUserSavedJobRepository, UserSavedJobRepository>();
            services.AddScoped<IPublicJobUseCase, PublicJobUseCase>();
            services.AddScoped<ICandidateJobUseCase, CandidateJobUseCase>();


            return services;
        }
    }
}
