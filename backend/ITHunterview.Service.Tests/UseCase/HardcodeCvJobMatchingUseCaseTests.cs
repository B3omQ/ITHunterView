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
        score.MatchScore.Should().Be(100m);
        score.MatchDetails.Should().Contain("available_cv_metrics");
        score.CvAnalysisQuality.Should().Be(CvAnalysisQuality.PARTIAL);
    }

    [Fact]
    public async Task MatchCvWithAllJobs_PastedJdHistory_DoesNotAbortSavedJobMatching()
    {
        await using var context = CreateContext();
        var (cv, job) = CreateEntities(includeCvDomains: true);
        var pastedJdMatch = new CvJobMatchScores
        {
            Id = Guid.NewGuid(),
            UserId = cv.UserId,
            CvId = cv.Id,
            JobId = null,
            RawJdText = "Pasted job description",
            MatchScore = 73m,
            MatchDetails = "pasted-jd-result",
            Status = "Completed",
            MatchType = "AI",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Cvs.Add(cv);
        context.JobPostings.Add(job);
        context.CvJobMatchScores.Add(pastedJdMatch);
        await context.SaveChangesAsync();

        await CreateUseCase(context).MatchCvWithAllJobsHardcodeAsync(cv.Id, cv.UserId);

        var scores = await context.CvJobMatchScores.ToListAsync();
        scores.Should().HaveCount(2);
        scores.Single(score => score.JobId == job.Id).Status.Should().Be("Completed");
        var preservedPastedJdMatch = scores.Single(score => score.Id == pastedJdMatch.Id);
        preservedPastedJdMatch.JobId.Should().BeNull();
        preservedPastedJdMatch.MatchScore.Should().Be(73m);
        preservedPastedJdMatch.MatchDetails.Should().Be("pasted-jd-result");
    }

    [Fact]
    public async Task MatchCvWithAllJobs_PresentButEmptyDomain_IsExcludedInsteadOfCreatingNeutralOrZeroScore()
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
        score.MatchScore.Should().Be(100m);
        score.MatchDetails.Should().Contain("available_cv_metrics");
    }

    [Fact]
    public async Task MatchCvWithAllJobs_NoSafeJdDimension_CompletesUnscoredWithoutTechnicalError()
    {
        await using var context = CreateContext();
        var (cv, job) = CreateEntities(includeCvDomains: true);
        job.ParsedData = """{"matching_metrics":{"job_titles_normalized":[],"skills_normalized":[],"total_years_exp":0,"domains":[]}}""";
        context.Cvs.Add(cv);
        context.JobPostings.Add(job);
        await context.SaveChangesAsync();

        await CreateUseCase(context).MatchCvWithAllJobsHardcodeAsync(cv.Id, cv.UserId);

        var score = await context.CvJobMatchScores.SingleAsync();
        score.Status.Should().Be("Completed");
        score.MatchScore.Should().BeNull();
        score.ErrorCode.Should().BeNull();
        score.ErrorMessage.Should().BeNull();
        score.MatchDetails.Should().Contain("SCORE_UNAVAILABLE");
    }

    [Fact]
    public async Task MatchCvWithAllJobs_PartialStructuredRequirements_PreservesOutcomesButDoesNotInventOverallScore()
    {
        await using var context = CreateContext();
        var (cv, job) = CreateEntities(includeCvDomains: true);
        job.ParsedData = """
            {"schema_version":"jd-analysis-effective/v1","analysis_quality":"PARTIAL","analysis_coverage":{"input_group_count":2,"accepted_group_count":1,"discarded_group_count":1,"input_item_count":2,"accepted_item_count":1,"discarded_item_count":1,"requirement_set_complete":false},"matching_metrics":{"job_titles_normalized":[],"skills_normalized":[],"total_years_exp":0,"domains":[],"requirement_groups":[{"group_id":"grp-001","source_requirement_id":"req-001","intent":"qualification","operator":"all_of","min_satisfied":1,"importance":"must_have","source_section":"requirements","requirement_verbatim":"C# required.","items":[{"item_id":"grp-001:item-001","category":"tech_skill","skill_name":"C#","raw_mention":"C#","min_years":null,"max_years":null}]}]}}
            """;
        context.Cvs.Add(cv);
        context.JobPostings.Add(job);
        await context.SaveChangesAsync();

        await CreateUseCase(context).MatchCvWithAllJobsHardcodeAsync(cv.Id, cv.UserId);

        var score = await context.CvJobMatchScores.SingleAsync();
        score.Status.Should().Be("Completed");
        score.MatchScore.Should().BeNull();
        score.MatchDetails.Should().Contain("PARTIAL_REQUIREMENT_SET");
        score.MatchDetails.Should().Contain("GroupOutcomes");
    }

    [Fact]
    public async Task MatchJobWithAllCvs_EligibleVisibleCandidate_WithNoSafeJdDimension_CompletesUnscored()
    {
        await using var context = CreateContext();
        var (cv, job) = CreateEntities(includeCvDomains: true);
        job.ParsedData = """{"matching_metrics":{"job_titles_normalized":[],"skills_normalized":[],"total_years_exp":0,"domains":[]}}""";
        cv.IsPrimary = true;
        var user = new User
        {
            Id = cv.UserId,
            Email = "candidate@example.test",
            Status = UserStatus.ACTIVE
        };
        var profile = new CandidateProfiles
        {
            UserId = user.Id,
            IsVisibleToRecruiters = true,
            User = user
        };
        user.CandidateProfile = profile;
        cv.User = user;
        user.Cvs.Add(cv);

        context.Users.Add(user);
        context.CandidateProfiles.Add(profile);
        context.Cvs.Add(cv);
        context.JobPostings.Add(job);
        await context.SaveChangesAsync();

        await CreateUseCase(context).MatchJobWithAllCvsHardcodeAsync(job.Id, job.RecruiterId);

        var score = await context.CvJobMatchScores.SingleAsync();
        score.Status.Should().Be("Completed");
        score.MatchScore.Should().BeNull();
        score.ErrorCode.Should().BeNull();
        score.MatchDetails.Should().Contain("SCORE_UNAVAILABLE");
    }

    [Fact]
    public async Task MatchJobWithAllCvs_PastedCvHistory_DoesNotAbortSavedCvMatching()
    {
        await using var context = CreateContext();
        var (cv, job) = CreateEntities(includeCvDomains: true);
        cv.IsPrimary = true;
        var user = new User
        {
            Id = cv.UserId,
            Email = "candidate@example.test",
            Status = UserStatus.ACTIVE
        };
        var profile = new CandidateProfiles
        {
            UserId = user.Id,
            IsVisibleToRecruiters = true,
            User = user
        };
        user.CandidateProfile = profile;
        cv.User = user;
        user.Cvs.Add(cv);
        var pastedCvMatch = new CvJobMatchScores
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CvId = null,
            JobId = job.Id,
            RawJdText = job.Requirements,
            MatchScore = 64m,
            MatchDetails = "pasted-cv-result",
            Status = "Completed",
            MatchType = "AI",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        context.CandidateProfiles.Add(profile);
        context.Cvs.Add(cv);
        context.JobPostings.Add(job);
        context.CvJobMatchScores.Add(pastedCvMatch);
        await context.SaveChangesAsync();

        await CreateUseCase(context).MatchJobWithAllCvsHardcodeAsync(job.Id, job.RecruiterId);

        var scores = await context.CvJobMatchScores.ToListAsync();
        scores.Should().HaveCount(2);
        scores.Single(score => score.CvId == cv.Id).Status.Should().Be("Completed");
        var preservedPastedCvMatch = scores.Single(score => score.Id == pastedCvMatch.Id);
        preservedPastedCvMatch.CvId.Should().BeNull();
        preservedPastedCvMatch.MatchScore.Should().Be(64m);
        preservedPastedCvMatch.MatchDetails.Should().Be("pasted-cv-result");
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
                                        && type.ClrType != typeof(CvJobMatchScores)
                                        && type.ClrType != typeof(JobSkillRequirements)
                                        && type.ClrType != typeof(Skills)
                                        && type.ClrType != typeof(User)
                                        && type.ClrType != typeof(CandidateProfiles))
                         .Select(type => type.ClrType)
                         .Distinct()
                         .ToList())
            {
                modelBuilder.Ignore(entityType);
            }
            modelBuilder.Entity<Cvs>().Ignore(value => value.TitleEmbedding);
            modelBuilder.Entity<Cvs>().Ignore(value => value.SkillsEmbedding);
            modelBuilder.Entity<Cvs>().Ignore(value => value.ExperienceEmbedding);
            modelBuilder.Entity<Cvs>().Ignore(value => value.DomainEmbedding);
            modelBuilder.Entity<JobPostings>().Ignore(value => value.TitleEmbedding);
            modelBuilder.Entity<JobPostings>().Ignore(value => value.SkillsEmbedding);
            modelBuilder.Entity<JobPostings>().Ignore(value => value.ExperienceEmbedding);
            modelBuilder.Entity<JobPostings>().Ignore(value => value.DomainEmbedding);
            modelBuilder.Entity<Skills>().Ignore(value => value.Category);
            modelBuilder.Entity<Skills>().Ignore(value => value.Aliases);
        }
    }
}
