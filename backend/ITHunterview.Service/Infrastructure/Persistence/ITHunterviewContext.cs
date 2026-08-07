using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Entities.Cv;
using ITHunterview.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class ITHunterviewContext : DbContext
    {
        public ITHunterviewContext(DbContextOptions<ITHunterviewContext> options) : base(options)
        {
        }

        // IAM
        public virtual DbSet<Roles> Roles { get; set; } = null!;
        public virtual DbSet<Permissions> Permissions { get; set; } = null!;
        public virtual DbSet<RolePermissions> RolePermissions { get; set; } = null!;
        public virtual DbSet<User> Users { get; set; } = null!;
        public virtual DbSet<CandidateProfiles> CandidateProfiles { get; set; } = null!;
        public virtual DbSet<RecruiterProfiles> RecruiterProfiles { get; set; } = null!;
        public virtual DbSet<EmailVerificationTokens> EmailVerificationTokens { get; set; } = null!;
        public virtual DbSet<PasswordResets> PasswordResets { get; set; } = null!;
        public virtual DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public virtual DbSet<UserActivityLogs> UserActivityLogs { get; set; } = null!;

        // Master data & Companies
        public virtual DbSet<Companies> Companies { get; set; } = null!;
        public virtual DbSet<CompanyReviews> CompanyReviews { get; set; } = null!;
        public virtual DbSet<JobCategories> JobCategories { get; set; } = null!;
        public virtual DbSet<Majors> Majors { get; set; } = null!;
        public virtual DbSet<SkillCategories> SkillCategories { get; set; } = null!;
        public virtual DbSet<Skills> Skills { get; set; } = null!;
        public virtual DbSet<SkillAliases> SkillAliases { get; set; } = null!;

        // Candidate Portfolio
        public virtual DbSet<Cvs> Cvs { get; set; } = null!;
        public virtual DbSet<UserSkills> UserSkills { get; set; } = null!;
        public virtual DbSet<CandidateExperiences> CandidateExperiences { get; set; } = null!;
        public virtual DbSet<CandidateEducations> CandidateEducations { get; set; } = null!;
        public virtual DbSet<CandidateCertifications> CandidateCertifications { get; set; } = null!;

        // ATS & Jobs
        public virtual DbSet<JobPostings> JobPostings { get; set; } = null!;
        public virtual DbSet<JobSkillRequirements> JobSkillRequirements { get; set; } = null!;
        public virtual DbSet<JobAnalysisRuns> JobAnalysisRuns { get; set; } = null!;
        public virtual DbSet<JobSkillDecisions> JobSkillDecisions { get; set; } = null!;
        public virtual DbSet<JobReviews> JobReviews { get; set; } = null!;

        public virtual DbSet<UserSavedJobs> UserSavedJobs { get; set; } = null!;
        public virtual DbSet<JobApplications> JobApplications { get; set; } = null!;
        public virtual DbSet<ApplicationHistory> ApplicationHistory { get; set; } = null!;
        public virtual DbSet<JobPromotions> JobPromotions { get; set; } = null!;

        // AI Engine
        public virtual DbSet<CvJobMatchScores> CvJobMatchScores { get; set; } = null!;
        public virtual DbSet<InterviewQuestionBank> InterviewQuestionBank { get; set; } = null!;
        public virtual DbSet<InterviewSessions> InterviewSessions { get; set; } = null!;
        public virtual DbSet<InterviewAnswers> InterviewAnswers { get; set; } = null!;
        public virtual DbSet<InterviewReports> InterviewReports { get; set; } = null!;
        public virtual DbSet<LearningPaths> LearningPaths { get; set; } = null!;
        public virtual DbSet<AiApiUsageLogs> AiApiUsageLogs { get; set; } = null!;
        public virtual DbSet<OptimizeSession> OptimizeSessions { get; set; } = null!;

        // SFIA & Learning Paths
        public virtual DbSet<SfiaSkill> SfiaSkills { get; set; } = null!;
        public virtual DbSet<SfiaSkillLevel> SfiaSkillLevels { get; set; } = null!;
        public virtual DbSet<TargetRoleTemplate> TargetRoleTemplates { get; set; } = null!;
        public virtual DbSet<TargetRoleSkill> TargetRoleSkills { get; set; } = null!;

        // Finance & Billing
        public virtual DbSet<Subscriptions> Subscriptions { get; set; } = null!;
        public virtual DbSet<UserSubscriptions> UserSubscriptions { get; set; } = null!;
        public virtual DbSet<UserWallets> UserWallets { get; set; } = null!;
        public virtual DbSet<Payments> Payments { get; set; } = null!;
        public virtual DbSet<CreditTransactions> CreditTransactions { get; set; } = null!;
        public virtual DbSet<CoinFeatures> CoinFeatures { get; set; } = null!;
        public virtual DbSet<CoinPackages> CoinPackages { get; set; } = null!;
        public virtual DbSet<RecruiterUnlockedCvs> RecruiterUnlockedCvs { get; set; } = null!;

        // System Ops
        public virtual DbSet<SystemConfigs> SystemConfigs { get; set; } = null!;
        public virtual DbSet<Prompts> Prompts { get; set; } = null!;
        public virtual DbSet<PromptVersions> PromptVersions { get; set; } = null!;
        public virtual DbSet<Notifications> Notifications { get; set; } = null!;
        public virtual DbSet<SysEmailLogs> SysEmailLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasPostgresExtension("vector");

            // Postgres Enums
            modelBuilder.HasPostgresEnum<UserStatus>();
            modelBuilder.HasPostgresEnum<CompanyVerificationMethod>();
            modelBuilder.HasPostgresEnum<CompanyStatus>();
            modelBuilder.HasPostgresEnum<ReviewStatus>();
            modelBuilder.HasPostgresEnum<SkillStatus>();
            modelBuilder.HasPostgresEnum<JobType>();
            modelBuilder.HasPostgresEnum<JobStatus>();
            modelBuilder.HasPostgresEnum<ApplicationStatus>();
            modelBuilder.HasPostgresEnum<PromotionStatus>();
            modelBuilder.HasPostgresEnum<DifficultyLevel>();
            modelBuilder.HasPostgresEnum<InterviewSessionStatus>();
            modelBuilder.HasPostgresEnum<SubscriptionStatus>();
            modelBuilder.HasPostgresEnum<UserSubscriptionStatus>();
            modelBuilder.HasPostgresEnum<PaymentGateway>();
            modelBuilder.HasPostgresEnum<PaymentTargetType>();
            modelBuilder.HasPostgresEnum<PaymentStatus>();
            modelBuilder.HasPostgresEnum<CreditTransactionType>();
            modelBuilder.HasPostgresEnum<EmploymentType>();
            modelBuilder.HasPostgresEnum<NotificationType>();
            modelBuilder.HasPostgresEnum<EmailLogStatus>();
            modelBuilder.HasPostgresEnum<ActivityLogCategory>();
            modelBuilder.HasPostgresEnum<ActivityLogStatus>();

            // User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.PasswordHash).IsRequired();

                entity.HasOne(u => u.Role)
                      .WithMany(r => r.Users)
                      .HasForeignKey(u => u.RoleId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // RefreshToken
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Token).IsRequired();
                entity.HasIndex(e => e.Token).IsUnique();

                entity.HasOne(d => d.User)
                      .WithMany(p => p.RefreshTokens)
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Cvs
            modelBuilder.Entity<Cvs>(entity =>
            {
                entity.HasOne(c => c.User)
                      .WithMany(u => u.Cvs)
                      .HasForeignKey(c => c.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Ensure only 1 primary CV per user
                entity.HasIndex(e => new { e.UserId, e.IsPrimary })
                      .IsUnique()
                      .HasFilter("\"is_primary\" = true AND \"deleted_at\" IS NULL");
            });

            // CandidateProfiles
            modelBuilder.Entity<CandidateProfiles>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId).IsUnique();

                entity.HasOne(cp => cp.User)
                      .WithOne(u => u.CandidateProfile)
                      .HasForeignKey<CandidateProfiles>(cp => cp.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // RecruiterProfiles
            modelBuilder.Entity<RecruiterProfiles>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId).IsUnique();

                entity.HasOne(rp => rp.User)
                      .WithOne(u => u.RecruiterProfile)
                      .HasForeignKey<RecruiterProfiles>(rp => rp.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rp => rp.Company)
                      .WithMany()
                      .HasForeignKey(rp => rp.CompanyId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // EmailVerificationTokens
            modelBuilder.Entity<EmailVerificationTokens>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // PasswordResets
            modelBuilder.Entity<PasswordResets>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Composite PKs
            modelBuilder.Entity<RolePermissions>().HasKey(rp => new { rp.RoleId, rp.PermissionId });
            modelBuilder.Entity<UserSkills>().HasKey(us => new { us.UserId, us.SkillId });
            modelBuilder.Entity<JobSkillRequirements>().HasKey(jsr => new { jsr.JobId, jsr.SkillId });
            modelBuilder.Entity<UserSavedJobs>().HasKey(usj => new { usj.UserId, usj.JobId });

            // UserSkills navigation -> Skills
            modelBuilder.Entity<UserSkills>(entity =>
            {
                entity.HasOne(us => us.Skill)
                      .WithMany()
                      .HasForeignKey(us => us.SkillId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // CandidateExperiences
            modelBuilder.Entity<CandidateExperiences>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);

                entity.HasOne<User>()
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Companies>()
                      .WithMany()
                      .HasForeignKey(e => e.CompanyId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // CandidateEducations
            modelBuilder.Entity<CandidateEducations>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne<User>()
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Majors>()
                      .WithMany()
                      .HasForeignKey(e => e.MajorId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // CandidateCertifications
            modelBuilder.Entity<CandidateCertifications>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);

                entity.HasOne<User>()
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            // Majors
            modelBuilder.Entity<Majors>(entity =>
            {
                entity.HasIndex(e => e.Code)
                      .IsUnique()
                      .HasFilter("deleted_at IS NULL");

                entity.HasIndex(e => e.NormalizedName);

                entity.HasOne(m => m.Parent)
                      .WithMany(m => m.Children)
                      .HasForeignKey(m => m.ParentId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Skills
            modelBuilder.Entity<Skills>(entity =>
            {
                entity.HasIndex(e => e.NormalizedName);

                entity.HasOne(s => s.Category)
                      .WithMany(c => c.Skills)
                      .HasForeignKey(s => s.CategoryId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // SkillAliases
            modelBuilder.Entity<SkillAliases>(entity =>
            {
                entity.HasIndex(e => e.NormalizedAliasName);

                entity.HasOne(sa => sa.Skill)
                      .WithMany(s => s.Aliases)
                      .HasForeignKey(sa => sa.SkillId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Global Query Filters for Soft Delete
            modelBuilder.Entity<Majors>().HasQueryFilter(m => m.DeletedAt == null);

            // UserActivityLogs Indexes
            modelBuilder.Entity<UserActivityLogs>(entity =>
            {
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.TableName);
                entity.HasIndex(e => e.OperationType);
                entity.HasIndex(e => e.CreatedAt);

                // GIN Trigram indexes for high-performance ILike queries
                entity.HasIndex(e => e.IpAddress)
                      .HasMethod("gin")
                      .HasOperators("gin_trgm_ops");

                entity.HasIndex(e => e.TableName)
                      .HasMethod("gin")
                      .HasOperators("gin_trgm_ops");
            });


            // Prompts
            modelBuilder.Entity<Prompts>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.PromptKey).IsUnique();
            });

            // PromptVersions
            modelBuilder.Entity<PromptVersions>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // Partial Unique Index: only one active version per prompt
                entity.HasIndex(e => new { e.PromptId, e.IsActive })
                      .IsUnique()
                      .HasFilter("\"IsActive\" = true");
                      
                entity.HasOne(e => e.Prompt)
                      .WithMany(p => p.Versions)
                      .HasForeignKey(e => e.PromptId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // TargetRoleSkills
            modelBuilder.Entity<TargetRoleSkill>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasOne(e => e.RoleTemplate)
                      .WithMany(rt => rt.RequiredSkills)
                      .HasForeignKey(e => e.RoleTemplateId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.SfiaSkill)
                      .WithMany(s => s.TargetRoleSkills)
                      .HasForeignKey(e => e.SfiaSkillId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // SfiaSkillLevels
            modelBuilder.Entity<SfiaSkillLevel>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasOne(e => e.SfiaSkill)
                      .WithMany(s => s.Levels)
                      .HasForeignKey(e => e.SfiaSkillId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // OptimizeSession
            modelBuilder.Entity<OptimizeSession>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            // UserWallets
            modelBuilder.Entity<UserWallets>(entity =>
            {
                entity.HasIndex(e => e.UserId).IsUnique();
            });

            // Payments
            modelBuilder.Entity<Payments>(entity =>
            {
                entity.HasIndex(e => e.OrderCode).IsUnique();
            });

            // JobAnalysisRuns
            modelBuilder.Entity<JobAnalysisRuns>(entity =>
            {
                entity.ToTable("job_analysis_runs");
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => new { e.JobId, e.InputRevision, e.AttemptNumber }).IsUnique();
                entity.HasIndex(e => new { e.JobId, e.InputRevision, e.InputHash });
                entity.HasIndex(e => new { e.Status, e.CreatedAt });

                entity.HasIndex(e => new { e.JobId, e.InputRevision })
                      .IsUnique()
                      .HasFilter("status IN ('PENDING', 'PROCESSING')");

                entity.HasIndex(e => new { e.JobId, e.IdempotencyKey })
                      .IsUnique()
                      .HasFilter("idempotency_key IS NOT NULL");

                entity.Property(e => e.Status)
                      .HasConversion<string>();

                entity.Property(e => e.RawInputSnapshot).HasColumnType("jsonb");
                entity.Property(e => e.RawAnalysisJson).HasColumnType("jsonb");
                entity.Property(e => e.EffectiveAnalysisJson).HasColumnType("jsonb");
                entity.Property(e => e.ValidationErrorsJson).HasColumnType("jsonb");

                entity.HasOne(e => e.Job)
                      .WithMany()
                      .HasForeignKey(e => e.JobId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.SystemPromptVersion)
                      .WithMany()
                      .HasForeignKey(e => e.SystemPromptVersionId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.UserPromptVersion)
                      .WithMany()
                      .HasForeignKey(e => e.UserPromptVersionId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // JobSkillDecisions
            modelBuilder.Entity<JobSkillDecisions>(entity =>
            {
                entity.ToTable("job_skill_decisions");
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.JobAnalysisRunId);

                entity.Property(e => e.ResolutionStatus)
                      .HasConversion<string>();

                entity.Property(e => e.DecisionStatus)
                      .HasConversion<string>();

                entity.HasOne(e => e.JobAnalysisRun)
                      .WithMany()
                      .HasForeignKey(e => e.JobAnalysisRunId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.SuggestedSkill)
                      .WithMany()
                      .HasForeignKey(e => e.SuggestedSkillId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ResolvedSkill)
                      .WithMany()
                      .HasForeignKey(e => e.ResolvedSkillId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // JobPostings ActiveAnalysisRun and EffectiveAnalysisRun
            modelBuilder.Entity<JobPostings>(entity =>
            {
                entity.HasOne(e => e.ActiveAnalysisRun)
                      .WithMany()
                      .HasForeignKey(e => e.ActiveAnalysisRunId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.EffectiveAnalysisRun)
                      .WithMany()
                      .HasForeignKey(e => e.EffectiveAnalysisRunId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // RecruiterUnlockedCvs
            modelBuilder.Entity<RecruiterUnlockedCvs>(entity =>
            {
                entity.HasIndex(e => new { e.RecruiterId, e.CvId }).IsUnique();
            });

            // OptimizeSession
            modelBuilder.Entity<OptimizeSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CvDocument)
                      .HasConversion(
                          v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                          v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<CvDocument>(v, (System.Text.Json.JsonSerializerOptions?)null)
                      )
                      .HasColumnType("jsonb");
            });
        }
    }
}

