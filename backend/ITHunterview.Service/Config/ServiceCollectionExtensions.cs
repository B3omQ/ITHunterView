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
            // Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ITokenRepository, TokenRepository>();
            services.AddScoped<IJobPostingRepository, JobPostingRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IEmailVerificationRepository, EmailVerificationRepository>();
            services.AddScoped<IPasswordResetRepository, PasswordResetRepository>();
            services.AddScoped<IJobCategoryRepository, JobCategoryRepository>();
            services.AddScoped<ISkillRepository, SkillRepository>();

            // Application Services
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IGoogleAuthService, GoogleAuthService>();

            // Use Cases
            services.AddScoped<IAuthUseCase, AuthUseCase>();
            services.AddScoped<IJobPostingsUseCase, JobPostingsUseCase>();
            services.AddScoped<IJobCategoriesUseCase, JobCategoriesUseCase>();
            services.AddScoped<ISkillsUseCase, SkillsUseCase>();
            services.AddScoped<IUserUseCase, UserUseCase>();

            return services;
        }
    }
}
