using ITHunterview.Service.Config;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.Service;
using ITHunterview.Service.Service.AiProviders;
using ITHunterview.Service.Service.Matching;
using ITHunterview.Service.Interface.Service.Matching;
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

            services.AddScoped<IPromptAdminRepository, PromptAdminRepository>();

            services.AddScoped<INotificationRepository, NotificationRepository>();


            // Repositories — Candidate Profile
            services.AddScoped<ICandidateProfileRepository, CandidateProfileRepository>();
            services.AddScoped<ICandidateSkillRepository, CandidateSkillRepository>();
            services.AddScoped<ICandidateExperienceRepository, CandidateExperienceRepository>();
            services.AddScoped<ICandidateEducationRepository, CandidateEducationRepository>();
            services.AddScoped<ICandidateCertificationRepository, CandidateCertificationRepository>();

            // Repositories — Interview & AI
            services.AddScoped<IInterviewQuestionBankRepository, InterviewQuestionBankRepository>();
            services.AddScoped<IInterviewSessionRepository, InterviewSessionRepository>();
            services.AddScoped<IInterviewAnswerRepository, InterviewAnswerRepository>();
            services.AddScoped<ILearningPathRepository, LearningPathRepository>();
            services.AddScoped<IOptimizeSessionRepository, OptimizeSessionRepository>();

            // Application Services
            services.AddHttpClient();
            services.Configure<AiSettings>(configuration.GetSection("AiSettings"));
            services.AddScoped<IAiProvider, OpenAiProvider>();
            services.AddScoped<IAiProvider, GeminiProvider>();
            services.AddScoped<IAiProvider, ClaudeProvider>();
            services.AddScoped<IAiProvider, GroqProvider>();
            services.AddScoped<IAiProviderFactory, AiProviderFactory>();
            services.AddScoped<IAiService, AiService>();
            services.AddScoped<IPromptManagementService, PromptManagementService>();
            services.AddScoped<ISpeechToTextService, AssemblyAiService>();

            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IGoogleAuthService, GoogleAuthService>();
            services.AddScoped<IFileUploadService, CloudinaryService>();
            services.AddHttpClient<IAiEmbeddingService, GeminiEmbeddingService>();

            // Matching AI Services
            services.AddScoped<ICvTextExtractorService, CvTextExtractorService>();
            services.AddScoped<IJdExtractionService, JdExtractionService>();
            services.AddScoped<IVectorEmbeddingService, VectorEmbeddingService>();
            services.AddScoped<IVectorSearchService, VectorSearchService>();
            services.AddScoped<IJdFitScoringService, JdFitScoringService>();
            services.AddScoped<ICvQualityScoringService, CvQualityScoringService>();
            services.AddScoped<IScoringAggregatorService, ScoringAggregatorService>();
            services.AddScoped<ISummarizerService, SummarizerService>();

            services.AddScoped<PdfCvExtractor>();
            services.AddScoped<DocxCvExtractor>();
            services.AddScoped<PdfCvRenderer>();
            services.AddScoped<DocxCvRenderer>();

            // Use Cases — Auth
            services.AddScoped<IAuthUseCase, AuthUseCase>();
            services.AddScoped<IJobPostingsUseCase, JobPostingsUseCase>();
            services.AddScoped<IJobCategoriesUseCase, JobCategoriesUseCase>();
            services.AddScoped<ISkillsUseCase, SkillsUseCase>();
            services.AddScoped<IUserUseCase, UserUseCase>();
            services.AddScoped<IJobApplicationUseCase, JobApplicationUseCase>();

            services.AddScoped<IAiConfigUseCase, AiConfigUseCase>();
            services.AddScoped<ICvUseCase, CvUseCase>();
            services.AddScoped<ICompanyUseCase, CompanyUseCase>();
            services.AddScoped<ISkillUseCase, SkillUseCase>();
            services.AddScoped<ICvJobMatchingUseCase, CvJobMatchingUseCase>();
            services.AddScoped<IHardcodeCvJobMatchingUseCase, HardcodeCvJobMatchingUseCase>();
            services.AddScoped<IMajorUseCase, MajorUseCase>();
            services.AddScoped<IUserGovernanceUseCase, UserGovernanceUseCase>();
            services.AddScoped<IAuditLogUseCase, AuditLogUseCase>();
            services.AddScoped<ISubscriptionAdminUseCase, SubscriptionAdminUseCase>();
            services.AddScoped<ICoinConfigUseCase, CoinConfigUseCase>();
            services.AddScoped<ICandidateFeatureUsageUseCase, CandidateFeatureUsageUseCase>();
            services.AddScoped<IWalletUseCase, WalletUseCase>();
            services.AddScoped<IInterviewQuestionBankUseCase, InterviewQuestionBankUseCase>();
            services.AddScoped<IPromptAdminUseCase, PromptAdminUseCase>();
            services.AddScoped<INotificationUseCase, NotificationUseCase>();
            services.AddScoped<IOptimizeUseCase, OptimizeUseCase>();



            // Use Cases — Candidate Profile
            services.AddScoped<ICandidateProfileUseCase, CandidateProfileUseCase>();
            services.AddScoped<ICandidateSkillUseCase, CandidateSkillUseCase>();
            services.AddScoped<ICandidateExperienceUseCase, CandidateExperienceUseCase>();
            services.AddScoped<ICandidateEducationUseCase, CandidateEducationUseCase>();
            services.AddScoped<ICandidateCertificationUseCase, CandidateCertificationUseCase>();
            services.AddScoped<IInterviewUseCase, InterviewUseCase>();
            services.AddScoped<ILearningPathUseCase, LearningPathUseCase>();
            services.AddScoped<ITargetRoleUseCase, TargetRoleUseCase>();
            services.AddScoped<ISfiaSkillUseCase, SfiaSkillUseCase>();

            // Job Search & Saved Jobs
            services.AddScoped<IJobSearchRepository, JobSearchRepository>();
            services.AddScoped<IUserSavedJobRepository, UserSavedJobRepository>();
            services.AddScoped<IPublicJobUseCase, PublicJobUseCase>();
            services.AddScoped<ICandidateJobUseCase, CandidateJobUseCase>();


            return services;
        }
    }
}
