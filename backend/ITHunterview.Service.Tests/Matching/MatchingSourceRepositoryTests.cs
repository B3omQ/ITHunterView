using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Tests.Matching;

public class MatchingSourceRepositoryTests
{
    [Fact]
    public async Task GetOwnedCvAsync_ReturnsOnlyAnActiveCvOwnedByTheCandidate()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var owned = CreateCv(ownerId);
        var foreign = CreateCv(Guid.NewGuid());
        var deleted = CreateCv(ownerId, deletedAt: DateTime.UtcNow);
        context.Cvs.AddRange(owned, foreign, deleted);
        await context.SaveChangesAsync();
        var repository = new MatchingSourceRepository(context);

        var ownedResult = await repository.GetOwnedCvAsync(owned.Id, ownerId);
        var foreignResult = await repository.GetOwnedCvAsync(foreign.Id, ownerId);
        var deletedResult = await repository.GetOwnedCvAsync(deleted.Id, ownerId);

        ownedResult!.Id.Should().Be(owned.Id);
        foreignResult.Should().BeNull();
        deletedResult.Should().BeNull();
        context.Entry(ownedResult).State.Should().Be(EntityState.Detached);
    }

    [Theory]
    [InlineData(JobStatus.DRAFT, false, false, false)]
    [InlineData(JobStatus.CLOSED, false, false, false)]
    [InlineData(JobStatus.PUBLISHED, true, false, false)]
    [InlineData(JobStatus.PUBLISHED, false, true, false)]
    [InlineData(JobStatus.PUBLISHED, false, false, true)]
    public async Task GetAccessiblePublishedJobAsync_RejectsEveryNonPublicJob(
        JobStatus status,
        bool isBanned,
        bool isExpired,
        bool expectedAccessible)
    {
        await using var context = CreateContext();
        var job = CreateJob(status, isBanned, isExpired ? DateTime.UtcNow.AddMinutes(-1) : null);
        context.JobPostings.Add(job);
        await context.SaveChangesAsync();
        var repository = new MatchingSourceRepository(context);

        var result = await repository.GetAccessiblePublishedJobAsync(job.Id, DateTime.UtcNow);

        if (expectedAccessible)
        {
            result!.Id.Should().Be(job.Id);
        }
        else
        {
            result.Should().BeNull();
        }
    }

    [Fact]
    public async Task GetAccessiblePublishedJobAsync_ReturnsPublishedJobWhoseExpiryIsInTheFuture()
    {
        await using var context = CreateContext();
        var job = CreateJob(JobStatus.PUBLISHED, false, DateTime.UtcNow.AddMinutes(1));
        context.JobPostings.Add(job);
        await context.SaveChangesAsync();
        var repository = new MatchingSourceRepository(context);

        var result = await repository.GetAccessiblePublishedJobAsync(job.Id, DateTime.UtcNow);

        result!.Id.Should().Be(job.Id);
        context.Entry(result).State.Should().Be(EntityState.Detached);
    }

    [Fact]
    public async Task GetAccessibleJobAsync_ReturnsNonPublicJobOnlyWhenCandidateSavedIt()
    {
        await using var context = CreateContext();
        var candidateId = Guid.NewGuid();
        var job = CreateJob(JobStatus.CLOSED, isBanned: true, expiresAt: DateTime.UtcNow.AddDays(-1));
        context.JobPostings.Add(job);
        context.UserSavedJobs.Add(new UserSavedJobs
        {
            UserId = candidateId,
            JobId = job.Id,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        var repository = new MatchingSourceRepository(context);

        var savedResult = await repository.GetAccessibleJobAsync(job.Id, candidateId, DateTime.UtcNow);
        var foreignResult = await repository.GetAccessibleJobAsync(job.Id, Guid.NewGuid(), DateTime.UtcNow);

        savedResult!.Id.Should().Be(job.Id);
        foreignResult.Should().BeNull();
    }

    private static MatchingSourceTestContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ITHunterviewContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new MatchingSourceTestContext(options);
    }

    private static Cvs CreateCv(Guid userId, DateTime? deletedAt = null) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        FileUrl = "https://storage.example/cv.pdf",
        FileName = "cv.pdf",
        FileType = "application/pdf",
        ParsedData = string.Empty,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        DeletedAt = deletedAt
    };

    private static JobPostings CreateJob(JobStatus status, bool isBanned, DateTime? expiresAt) => new()
    {
        Id = Guid.NewGuid(),
        RecruiterId = Guid.NewGuid(),
        CompanyId = Guid.NewGuid(),
        JobCode = "MATCH-001",
        Title = "Backend Engineer",
        Description = "Description",
        Requirements = "Requirements",
        Benefits = "Benefits",
        IncomeText = string.Empty,
        WorkLocationText = string.Empty,
        Currency = "VND",
        Location = "Ho Chi Minh City",
        Status = status,
        IsBanned = isBanned,
        ExpiresAt = expiresAt,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private sealed class MatchingSourceTestContext : ITHunterviewContext
    {
        public MatchingSourceTestContext(DbContextOptions<ITHunterviewContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Keep this test model intentionally small. The production model uses
            // PostgreSQL/pgvector and contains keyless document types, neither of
            // which is supported by EF's in-memory provider.
            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                         .Where(type => type.ClrType != typeof(Cvs) &&
                                        type.ClrType != typeof(JobPostings) &&
                                        type.ClrType != typeof(UserSavedJobs))
                         .Select(type => type.ClrType)
                         .Distinct()
                         .ToList())
            {
                modelBuilder.Ignore(entityType);
            }

            modelBuilder.Entity<Cvs>(entity =>
            {
                entity.HasKey(cv => cv.Id);
                entity.Ignore(cv => cv.User);
                entity.Ignore(cv => cv.TitleEmbedding);
                entity.Ignore(cv => cv.SkillsEmbedding);
                entity.Ignore(cv => cv.ExperienceEmbedding);
                entity.Ignore(cv => cv.DomainEmbedding);
            });

            modelBuilder.Entity<JobPostings>(entity =>
            {
                entity.HasKey(job => job.Id);
                entity.Ignore(job => job.ActiveAnalysisRun);
                entity.Ignore(job => job.EffectiveAnalysisRun);
                entity.Ignore(job => job.TitleEmbedding);
                entity.Ignore(job => job.SkillsEmbedding);
                entity.Ignore(job => job.ExperienceEmbedding);
                entity.Ignore(job => job.DomainEmbedding);
            });

            modelBuilder.Entity<UserSavedJobs>().HasKey(saved => new { saved.UserId, saved.JobId });
        }
    }
}
