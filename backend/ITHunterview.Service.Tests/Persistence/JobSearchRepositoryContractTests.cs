using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Entities.Cv;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.JobSearch;
using ITHunterview.Service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace ITHunterview.Service.Tests.Persistence;

public sealed class JobSearchRepositoryContractTests
{
    private sealed class JobSearchTestContext : ITHunterviewContext
    {
        public JobSearchTestContext(DbContextOptions<ITHunterviewContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var allowed = new HashSet<Type> { typeof(JobPostings), typeof(Companies), typeof(JobSkillRequirements), typeof(Skills), typeof(Cvs) };

            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                         .Where(t => !allowed.Contains(t.ClrType))
                         .Select(t => t.ClrType)
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

            modelBuilder.Entity<Companies>(entity =>
            {
                entity.HasKey(c => c.Id);
            });

            modelBuilder.Entity<JobSkillRequirements>(entity =>
            {
                entity.HasKey(r => new { r.JobId, r.SkillId });
            });

            modelBuilder.Entity<Skills>(entity =>
            {
                entity.HasKey(s => s.Id);
            });
        }
    }

    private static (JobSearchTestContext context, JobSearchRepository repository, Guid companyId) CreateHarness()
    {
        var dbOptions = new DbContextOptionsBuilder<ITHunterviewContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new JobSearchTestContext(dbOptions);
        var companyId = Guid.NewGuid();
        context.Companies.Add(new Companies
        {
            Id = companyId,
            Name = "Tech Corp",
            Industry = "Information Technology",
            CompanyType = "Product",
            CompanySize = "50-100",
            Description = "A great company",
            HeadquartersAddress = "123 Street",
            LogoUrl = "https://example.com/logo.png",
            TaxCode = "0123456789",
            VerificationDocumentUrl = "https://example.com/doc.pdf",
            Website = "https://example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        context.SaveChanges();

        var repo = new JobSearchRepository(context);
        return (context, repo, companyId);
    }

    private static JobPostings CreateJob(
        Guid id,
        Guid companyId,
        string title,
        string? expertise = null,
        string? level = null,
        string? workingModel = null,
        decimal? minSalary = null,
        decimal? maxSalary = null,
        DateTime? publishedAt = null,
        DateTime? pushedTopUntil = null)
    {
        return new JobPostings
        {
            Id = id,
            JobCode = $"JOB-{id:N}".Substring(0, 10),
            CompanyId = companyId,
            RecruiterId = Guid.NewGuid(),
            Title = title,
            Description = "Standard job description",
            Requirements = "Standard requirements",
            Benefits = "Standard benefits",
            Location = "Ho Chi Minh",
            Currency = "USD",
            JobExpertise = expertise,
            Level = level,
            WorkingModel = workingModel,
            MinSalary = minSalary,
            MaxSalary = maxSalary,
            Status = JobStatus.PUBLISHED,
            IsBanned = false,
            PublishedAt = publishedAt ?? DateTime.UtcNow.AddDays(-1),
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            PushedTopUntil = pushedTopUntil,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
    }

    [Fact]
    public async Task FILTER04_SearchJobsAsync_AppliesOrInsideExpertiseAndAndAcrossGroups()
    {
        var (context, repo, companyId) = CreateHarness();

        var jobA = CreateJob(Guid.NewGuid(), companyId, "Job A", expertise: "Backend", level: "Senior", workingModel: "Remote");
        var jobB = CreateJob(Guid.NewGuid(), companyId, "Job B", expertise: "Data", level: "Senior", workingModel: "Remote");
        var jobC = CreateJob(Guid.NewGuid(), companyId, "Job C", expertise: "Frontend", level: "Senior", workingModel: "Remote");
        var jobD = CreateJob(Guid.NewGuid(), companyId, "Job D", expertise: "Backend", level: "Junior", workingModel: "Remote");
        var jobE = CreateJob(Guid.NewGuid(), companyId, "Job E", expertise: "Backend", level: "Senior", workingModel: "Onsite");

        context.JobPostings.AddRange(jobA, jobB, jobC, jobD, jobE);
        await context.SaveChangesAsync();

        var query = new JobSearchQueryDto
        {
            JobExpertises = new List<string> { "Backend", "Data" },
            Levels = new List<string> { "Senior" },
            WorkingModels = new List<string> { "Remote" },
            Page = 1,
            PageSize = 10
        };

        var result = await repo.SearchJobsAsync(query);

        result.Meta.TotalItems.Should().Be(2);
        result.Data.Select(x => x.Id).Should().BeEquivalentTo(new[] { jobA.Id, jobB.Id });
    }

    [Fact]
    public async Task FILTER05_SearchJobsAsync_PreservesSalaryIntervalOverlap()
    {
        var (context, repo, companyId) = CreateHarness();

        var exact = CreateJob(Guid.NewGuid(), companyId, "exact", minSalary: 100, maxSalary: 150);
        var lowPartial = CreateJob(Guid.NewGuid(), companyId, "low partial", minSalary: 80, maxSalary: 120);
        var highPartial = CreateJob(Guid.NewGuid(), companyId, "high partial", minSalary: 140, maxSalary: 180);
        var enclosesFilter = CreateJob(Guid.NewGuid(), companyId, "encloses filter", minSalary: 50, maxSalary: 200);
        var boundaryBelow = CreateJob(Guid.NewGuid(), companyId, "boundary below", minSalary: 50, maxSalary: 100);
        var boundaryAbove = CreateJob(Guid.NewGuid(), companyId, "boundary above", minSalary: 150, maxSalary: 200);
        var fullyBelow = CreateJob(Guid.NewGuid(), companyId, "fully below", minSalary: 50, maxSalary: 99);
        var fullyAbove = CreateJob(Guid.NewGuid(), companyId, "fully above", minSalary: 151, maxSalary: 200);
        var openLower = CreateJob(Guid.NewGuid(), companyId, "open lower", minSalary: null, maxSalary: 120);
        var openUpper = CreateJob(Guid.NewGuid(), companyId, "open upper", minSalary: 130, maxSalary: null);
        var negotiable = CreateJob(Guid.NewGuid(), companyId, "negotiable", minSalary: null, maxSalary: null);

        context.JobPostings.AddRange(exact, lowPartial, highPartial, enclosesFilter, boundaryBelow, boundaryAbove, fullyBelow, fullyAbove, openLower, openUpper, negotiable);
        await context.SaveChangesAsync();

        var query = new JobSearchQueryDto
        {
            MinSalary = 100,
            MaxSalary = 150,
            IncludeNegotiable = false,
            Page = 1,
            PageSize = 20
        };

        var result = await repo.SearchJobsAsync(query);

        var expectedIds = new[] { exact.Id, lowPartial.Id, highPartial.Id, enclosesFilter.Id, boundaryBelow.Id, boundaryAbove.Id, openLower.Id, openUpper.Id };
        result.Meta.TotalItems.Should().Be(expectedIds.Length);
        result.Data.Select(x => x.Id).Should().BeEquivalentTo(expectedIds);
    }

    [Fact]
    public async Task FILTER06_SearchJobsAsync_WhenIncludeNegotiableTrue_AddsOnlyBothNullJobs()
    {
        var (context, repo, companyId) = CreateHarness();

        var exact = CreateJob(Guid.NewGuid(), companyId, "exact", minSalary: 100, maxSalary: 150);
        var negotiable = CreateJob(Guid.NewGuid(), companyId, "negotiable", minSalary: null, maxSalary: null);
        var fullyBelow = CreateJob(Guid.NewGuid(), companyId, "fully below", minSalary: 50, maxSalary: 99);

        context.JobPostings.AddRange(exact, negotiable, fullyBelow);
        await context.SaveChangesAsync();

        var query = new JobSearchQueryDto
        {
            MinSalary = 100,
            MaxSalary = 150,
            IncludeNegotiable = true,
            Page = 1,
            PageSize = 20
        };

        var result = await repo.SearchJobsAsync(query);

        result.Meta.TotalItems.Should().Be(2);
        result.Data.Select(x => x.Id).Should().BeEquivalentTo(new[] { exact.Id, negotiable.Id });
    }

    [Fact]
    public async Task FILTER07_SearchJobsAsync_WhenNoSalaryBoundsSpecified_IgnoresIncludeNegotiableFlag()
    {
        var (context, repo, companyId) = CreateHarness();

        var jobA = CreateJob(Guid.NewGuid(), companyId, "job A", minSalary: 100, maxSalary: 150);
        var negotiable = CreateJob(Guid.NewGuid(), companyId, "negotiable", minSalary: null, maxSalary: null);

        context.JobPostings.AddRange(jobA, negotiable);
        await context.SaveChangesAsync();

        var queryFalse = new JobSearchQueryDto { IncludeNegotiable = false, Page = 1, PageSize = 10 };
        var queryTrue = new JobSearchQueryDto { IncludeNegotiable = true, Page = 1, PageSize = 10 };

        var resultFalse = await repo.SearchJobsAsync(queryFalse);
        var resultTrue = await repo.SearchJobsAsync(queryTrue);

        resultFalse.Meta.TotalItems.Should().Be(2);
        resultTrue.Meta.TotalItems.Should().Be(2);
        resultFalse.Data.Select(x => x.Id).Should().BeEquivalentTo(resultTrue.Data.Select(x => x.Id));
    }

    [Fact]
    public async Task FILTER08_SearchJobsAsync_AppliesFilterFirstAndPushTopSortSecond()
    {
        var (context, repo, companyId) = CreateHarness();

        var now = DateTime.UtcNow;
        var matchingPushed = CreateJob(Guid.NewGuid(), companyId, "matching-pushed", expertise: "Backend", publishedAt: now.AddDays(-10), pushedTopUntil: now.AddHours(12));
        var matchingNormal = CreateJob(Guid.NewGuid(), companyId, "matching-normal", expertise: "Backend", publishedAt: now.AddDays(-1), pushedTopUntil: null);
        var nonMatchingPushed = CreateJob(Guid.NewGuid(), companyId, "nonmatching-pushed", expertise: "Frontend", publishedAt: now.AddDays(-5), pushedTopUntil: now.AddHours(12));

        context.JobPostings.AddRange(matchingPushed, matchingNormal, nonMatchingPushed);
        await context.SaveChangesAsync();

        var query = new JobSearchQueryDto
        {
            JobExpertises = new List<string> { "Backend" },
            Page = 1,
            PageSize = 10
        };

        var result = await repo.SearchJobsAsync(query);

        result.Meta.TotalItems.Should().Be(2);
        result.Data.Select(x => x.Id).Should().ContainInOrder(matchingPushed.Id, matchingNormal.Id);
    }
}
