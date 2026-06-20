using ITHunterview.Domain.Entities;
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
        public DbSet<Roles> Roles { get; set; } = null!;
        public DbSet<Permissions> Permissions { get; set; } = null!;
        public DbSet<RolePermissions> RolePermissions { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<CandidateProfiles> CandidateProfiles { get; set; } = null!;
        public DbSet<RecruiterProfiles> RecruiterProfiles { get; set; } = null!;
        public DbSet<EmailVerificationTokens> EmailVerificationTokens { get; set; } = null!;
        public DbSet<PasswordResets> PasswordResets { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<UserActivityLogs> UserActivityLogs { get; set; } = null!;

        // Master data & Companies
        public DbSet<Companies> Companies { get; set; } = null!;
        public DbSet<CompanyReviews> CompanyReviews { get; set; } = null!;
        public DbSet<JobCategories> JobCategories { get; set; } = null!;
        public DbSet<Majors> Majors { get; set; } = null!;
        public DbSet<SkillCategories> SkillCategories { get; set; } = null!;
        public DbSet<Skills> Skills { get; set; } = null!;

        // Candidate Portfolio
        public DbSet<Cvs> Cvs { get; set; } = null!;
        public DbSet<UserSkills> UserSkills { get; set; } = null!;
        public DbSet<CandidateExperiences> CandidateExperiences { get; set; } = null!;
        public DbSet<CandidateEducations> CandidateEducations { get; set; } = null!;
        public DbSet<CandidateCertifications> CandidateCertifications { get; set; } = null!;

        // ATS & Jobs
        public DbSet<JobPostings> JobPostings { get; set; } = null!;
        public DbSet<JobSkillRequirements> JobSkillRequirements { get; set; } = null!;
        public DbSet<JobReviews> JobReviews { get; set; } = null!;
        public DbSet<UserSavedJobs> UserSavedJobs { get; set; } = null!;
        public DbSet<JobApplications> JobApplications { get; set; } = null!;
        public DbSet<ApplicationHistory> ApplicationHistory { get; set; } = null!;
        public DbSet<JobPromotions> JobPromotions { get; set; } = null!;

        // AI Engine
        public DbSet<CvJobMatchScores> CvJobMatchScores { get; set; } = null!;
        public DbSet<InterviewQuestionBank> InterviewQuestionBank { get; set; } = null!;
        public DbSet<InterviewSessions> InterviewSessions { get; set; } = null!;
        public DbSet<InterviewAnswers> InterviewAnswers { get; set; } = null!;
        public DbSet<InterviewReports> InterviewReports { get; set; } = null!;
        public DbSet<LearningPaths> LearningPaths { get; set; } = null!;
        public DbSet<AiApiUsageLogs> AiApiUsageLogs { get; set; } = null!;

        // Finance & Billing
        public DbSet<Subscriptions> Subscriptions { get; set; } = null!;
        public DbSet<UserSubscriptions> UserSubscriptions { get; set; } = null!;
        public DbSet<UserWallets> UserWallets { get; set; } = null!;
        public DbSet<Payments> Payments { get; set; } = null!;
        public DbSet<CreditTransactions> CreditTransactions { get; set; } = null!;

        // System Ops
        public DbSet<SystemConfigs> SystemConfigs { get; set; } = null!;
        public DbSet<SystemPrompts> SystemPrompts { get; set; } = null!;
        public DbSet<Notifications> Notifications { get; set; } = null!;
        public DbSet<SysEmailLogs> SysEmailLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
        }
    }
}
