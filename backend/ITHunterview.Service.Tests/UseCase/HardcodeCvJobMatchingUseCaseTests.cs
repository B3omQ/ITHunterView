using System.Text.Json.Nodes;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Service.Matching;
using ITHunterview.Service.Tests.Matching;
using ITHunterview.Service.UseCase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ITHunterview.Service.Tests.UseCase;

public sealed class HardcodeCvJobMatchingUseCaseTests
{
    [Fact]
    public async Task MatchCvWithAllJobs_MissingDomainMetric_RenormalizesAvailableWeights()
    {
        await using var context = CreateContext();
        var (cv, job) = CreateEntities(includeCvDomains: false);
        context.Cvs.Add(cv);
        context.JobPostings.Add(job);
        await context.SaveChangesAsync();

        var useCase = CreateUseCase(context);
        await useCase.MatchCvWithAllJobsHardcodeAsync(cv.Id, cv.UserId);

        var score = await context.CvJobMatchScores.SingleAsync();
        score.Status.Should().Be("Completed");
        score.MatchScore.Should().Be(1m);
        score.MatchDetails.Should().Contain("available_cv_metrics");
        score.CvAnalysisQuality.Should().Be(CvAnalysisQuality.PARTIAL);
    }

    [Fact]
    public async Task MatchCvWithAllJobs_PresentEmptyDomain_RemainsAnAvailableZeroScore()
    {
        await using var context = CreateContext();
        var (cv, job) = CreateEntities(includeCvDomains: true);
        context.Cvs.Add(cv);
        context.JobPostings.Add(job);
        await context.SaveChangesAsync();

        var useCase = CreateUseCase(context);
        await useCase.MatchCvWithAllJobsHardcodeAsync(cv.Id, cv.UserId);

        var score = await context.CvJobMatchScores.SingleAsync();
        score.Status.Should().Be("Completed");
        score.MatchScore.Should().Be(.9m);
        score.MatchDetails.Should().Contain("complete_cv_metrics");
    }

    private static HardcodeCvJobMatchingUseCase CreateUseCase(ITHunterviewContext context)
    {
        var extractor = new Mock<ICvTextExtractorService>(MockBehavior.Strict);
        return new HardcodeCvJobMatchingUseCase(
            context,
            extractor.Object,
            NullLogger<HardcodeCvJobMatchingUseCase>.Instance,
            new HardcodeJdRequirementScoringService(
                new JdRequirementProjector(),
                new JdHardcodeRequirementEvaluator()),
            new CvAnalysisResponseValidator());
    }

    private static (Cvs Cv, JobPostings Job) CreateEntities(bool includeCvDomains)
    {
        var cvJson = JsonNode.Parse(CvAnalysisResponseValidatorTests.CreateValidDocument())!.AsObject();
        if (!includeCvDomains)
        {
            cvJson["matching_metrics"]!.AsObject().Remove("domains");
        }

        var userId = Guid.NewGuid();
        var cv = new Cvs
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FileUrl = "https://example.test/cv.pdf",
            FileName = "cv.pdf",
            FileType = "application/pdf",
            ParsedData = cvJson.ToJsonString(),
            ParseStatus = "SUCCESS",
            RawText = "immutable CV source",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var job = new JobPostings
        {
            Id = Guid.NewGuid(),
            JobCode = Guid.NewGuid().ToString("N"),
            RecruiterId = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            Title = "Backend Developer",
            Description = "Build APIs",
            Requirements = "C#, three years, fintech",
            Benefits = string.Empty,
            Currency = "VND",
            Location = "Remote",
            Status = JobStatus.PUBLISHED,
            ParseStatus = "SUCCESS",
            ParsedData = """{"matching_metrics":{"job_titles_normalized":["Backend Developer"],"skills_normalized":["C#"],"total_years_exp":3,"domains":["fintech"]}}""",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        return (cv, job);
    }

    private static ITHunterviewContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ITHunterviewContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new HardcodeTestContext(options);
    }

    private sealed class HardcodeTestContext : ITHunterviewContext
    {
        public HardcodeTestContext(DbContextOptions<ITHunterviewContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                         .Where(type => type.ClrType != typeof(Cvs)
                                        && type.ClrType != typeof(JobPostings)
                                        && type.ClrType != typeof(CvJobMatchScores))
                         .Select(type => type.ClrType)
                         .Distinct()
                         .ToList())
            {
                modelBuilder.Ignore(entityType);
            }
            modelBuilder.Entity<Cvs>().Ignore(value => value.User);
            modelBuilder.Entity<Cvs>().Ignore(value => value.TitleEmbedding);
            modelBuilder.Entity<Cvs>().Ignore(value => value.SkillsEmbedding);
            modelBuilder.Entity<Cvs>().Ignore(value => value.ExperienceEmbedding);
            modelBuilder.Entity<Cvs>().Ignore(value => value.DomainEmbedding);
            modelBuilder.Entity<JobPostings>().Ignore(value => value.TitleEmbedding);
            modelBuilder.Entity<JobPostings>().Ignore(value => value.SkillsEmbedding);
            modelBuilder.Entity<JobPostings>().Ignore(value => value.ExperienceEmbedding);
            modelBuilder.Entity<JobPostings>().Ignore(value => value.DomainEmbedding);
        }
    }
}
