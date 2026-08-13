using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.FeatureUsage;
using ITHunterview.Service.DTOs.JobAnalysis;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ITHunterview.Service.Tests.JobAnalysis;

public sealed class JobAnalysisRepositoryEffectiveAnalysisTests : IDisposable
{
    private const string EffectiveAnalysis =
        "{\"schema_version\":\"jd-analysis-effective/v1\",\"analysis_quality\":\"COMPLETE\",\"matching_metrics\":{\"skills_normalized\":[\"Java\",\"Spring Boot\"],\"requirement_groups\":[{\"id\":\"grp-001\",\"source_requirement_id\":\"req-001\",\"intent\":\"qualification\",\"operator\":\"all_of\",\"min_satisfied\":2,\"importance\":\"must_have\",\"source_section\":\"requirements\",\"requirement_verbatim\":\"Thành thạo Java và Spring Boot.\",\"items\":[{\"id\":\"item-001\",\"category\":\"tech_skill\",\"skill_name\":\"Java\",\"raw_mention\":\"Java\"},{\"id\":\"item-002\",\"category\":\"tech_skill\",\"skill_name\":\"Spring Boot\",\"raw_mention\":\"Spring Boot\"}]}]}}";

    private readonly TestContext _context;
    private readonly Mock<ICandidateFeatureUsageUseCase> _featureUsage = new();
    private readonly JobAnalysisRepository _sut;

    public JobAnalysisRepositoryEffectiveAnalysisTests()
    {
        var options = new DbContextOptionsBuilder<ITHunterviewContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        _context = new TestContext(options);
        _featureUsage
            .Setup(service => service.TryConsumeFeatureAsync(It.IsAny<Guid>(), "PostJob", It.IsAny<string?>()))
            .ReturnsAsync(new FeatureConsumptionResult());
        _sut = new JobAnalysisRepository(_context, _featureUsage.Object);
    }

    [Fact]
    public async Task ApplyDecisionsAsync_DoesNotRewriteEffectiveAnalysisJson()
    {
        var seed = await SeedAsync(JdAnalysisQuality.COMPLETE, includeAcceptedSkill: true);
        var beforeHash = Hash(EffectiveAnalysis);

        var result = await _sut.ApplyDecisionsAsync(
            seed.Job.Id,
            seed.Run.Id,
            seed.RecruiterId,
            seed.Job.AnalysisRevision,
            seed.Run.DecisionVersion,
            new[]
            {
                new JobSkillDecisionInputDto
                {
                    DecisionId = seed.Decision!.Id,
                    Decision = SkillDecisionStatus.REJECTED
                }
            });

        result.Success.Should().BeTrue();
        _context.ChangeTracker.Clear();
        var persisted = await _context.JobAnalysisRuns.SingleAsync(run => run.Id == seed.Run.Id);
        persisted.EffectiveAnalysisJson.Should().Be(EffectiveAnalysis);
        Hash(persisted.EffectiveAnalysisJson!).Should().Be(beforeHash);
    }

    [Fact]
    public async Task FinalizeAsync_WithAcceptedDictionarySkill_CopiesEffectiveAnalysisUnchanged()
    {
        var seed = await SeedAsync(JdAnalysisQuality.COMPLETE, includeAcceptedSkill: true);
        var beforeHash = Hash(EffectiveAnalysis);

        var result = await _sut.FinalizeAsync(
            seed.Job.Id,
            seed.Run.Id,
            seed.RecruiterId,
            seed.Job.AnalysisRevision,
            seed.Run.DecisionVersion,
            confirmNoStandardSkills: false,
            reviewRequired: false);

        result.Success.Should().BeTrue();
        _context.ChangeTracker.Clear();
        var persistedJob = await _context.JobPostings.SingleAsync(job => job.Id == seed.Job.Id);
        var persistedRun = await _context.JobAnalysisRuns.SingleAsync(run => run.Id == seed.Run.Id);
        persistedJob.ParsedData.Should().Be(EffectiveAnalysis);
        persistedRun.EffectiveAnalysisJson.Should().Be(EffectiveAnalysis);
        Hash(persistedJob.ParsedData!).Should().Be(beforeHash);
        (await _context.JobSkillRequirements.CountAsync(item => item.JobId == seed.Job.Id)).Should().Be(1);
    }

    [Fact]
    public async Task FinalizeAsync_WithNoAcceptedSkillAndExplicitConfirmation_PreservesStructuredAnalysis()
    {
        var seed = await SeedAsync(JdAnalysisQuality.PARTIAL, includeAcceptedSkill: false);

        var result = await _sut.FinalizeAsync(
            seed.Job.Id,
            seed.Run.Id,
            seed.RecruiterId,
            seed.Job.AnalysisRevision,
            seed.Run.DecisionVersion,
            confirmNoStandardSkills: true,
            reviewRequired: false);

        result.Success.Should().BeTrue();
        _context.ChangeTracker.Clear();
        var persistedJob = await _context.JobPostings.SingleAsync(job => job.Id == seed.Job.Id);
        persistedJob.ParsedData.Should().Be(EffectiveAnalysis);
        (await _context.JobSkillRequirements.CountAsync(item => item.JobId == seed.Job.Id)).Should().Be(0);
    }

    [Fact]
    public async Task FinalizeAsync_WhenPublishedEditIsReady_AppliesAnalysisWithoutRepublishingOrChargingQuota()
    {
        var seed = await SeedAsync(JdAnalysisQuality.COMPLETE, includeAcceptedSkill: true, publishedEdit: true);
        var publishedAt = seed.Job.PublishedAt;
        var expiresAt = seed.Job.ExpiresAt;

        var result = await _sut.FinalizeAsync(
            seed.Job.Id,
            seed.Run.Id,
            seed.RecruiterId,
            seed.Job.AnalysisRevision,
            seed.Run.DecisionVersion,
            confirmNoStandardSkills: false,
            reviewRequired: false);

        result.Success.Should().BeTrue();
        _context.ChangeTracker.Clear();
        var persisted = await _context.JobPostings.SingleAsync(job => job.Id == seed.Job.Id);
        persisted.Status.Should().Be(JobStatus.PUBLISHED);
        persisted.PublishedAt.Should().Be(publishedAt);
        persisted.ExpiresAt.Should().Be(expiresAt);
        persisted.ApplicationCount.Should().Be(12);
        persisted.ViewCount.Should().Be(34);
        persisted.ParsedData.Should().Be(EffectiveAnalysis);
        persisted.EffectiveAnalysisRevision.Should().Be(seed.Job.AnalysisRevision);
        persisted.EffectiveAnalysisRunId.Should().Be(seed.Run.Id);
        (await _context.JobSkillRequirements.SingleAsync(item => item.JobId == seed.Job.Id)).SkillId.Should().Be(101);
        _featureUsage.Verify(
            service => service.TryConsumeFeatureAsync(It.IsAny<Guid>(), "PostJob", It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task FinalizeAsync_WhenPublishedEditWasAlreadyApplied_IsIdempotent()
    {
        var seed = await SeedAsync(JdAnalysisQuality.COMPLETE, includeAcceptedSkill: true, publishedEdit: true);

        var first = await _sut.FinalizeAsync(
            seed.Job.Id,
            seed.Run.Id,
            seed.RecruiterId,
            seed.Job.AnalysisRevision,
            seed.Run.DecisionVersion,
            confirmNoStandardSkills: false,
            reviewRequired: false);
        var second = await _sut.FinalizeAsync(
            seed.Job.Id,
            seed.Run.Id,
            seed.RecruiterId,
            seed.Job.AnalysisRevision,
            seed.Run.DecisionVersion,
            confirmNoStandardSkills: false,
            reviewRequired: false);

        first.Success.Should().BeTrue();
        second.Success.Should().BeTrue();
        (await _context.JobSkillRequirements.CountAsync(item => item.JobId == seed.Job.Id)).Should().Be(1);
        _featureUsage.Verify(
            service => service.TryConsumeFeatureAsync(It.IsAny<Guid>(), "PostJob", It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task FinalizeAsync_WhenLegacyPublishedJobNeedsNoAnalysis_CompletesAsNoOp()
    {
        var seed = await SeedAsync(JdAnalysisQuality.COMPLETE, includeAcceptedSkill: true, publishedEdit: true);
        var persisted = await _context.JobPostings.SingleAsync(job => job.Id == seed.Job.Id);
        persisted.EffectiveAnalysisRevision = null;
        persisted.ActiveAnalysisRunId = null;
        persisted.ParseStatus = "SUCCESS";
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.FinalizeAsync(
            seed.Job.Id,
            Guid.Empty,
            seed.RecruiterId,
            seed.Job.AnalysisRevision,
            expectedDecisionVersion: 0,
            confirmNoStandardSkills: false,
            reviewRequired: false);

        result.Success.Should().BeTrue();
        (await _context.JobPostings.SingleAsync(job => job.Id == seed.Job.Id)).ParsedData
            .Should().Be("{\"skills\":[\"Legacy\"]}");
        _featureUsage.Verify(
            service => service.TryConsumeFeatureAsync(It.IsAny<Guid>(), "PostJob", It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task FinalizeAsync_WhenPublishedEditWasBanned_PreservesEffectiveAnalysisAndSkills()
    {
        var seed = await SeedAsync(JdAnalysisQuality.COMPLETE, includeAcceptedSkill: true, publishedEdit: true);
        var persisted = await _context.JobPostings.SingleAsync(job => job.Id == seed.Job.Id);
        persisted.IsBanned = true;
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.FinalizeAsync(
            seed.Job.Id,
            seed.Run.Id,
            seed.RecruiterId,
            seed.Job.AnalysisRevision,
            seed.Run.DecisionVersion,
            confirmNoStandardSkills: false,
            reviewRequired: false);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("JOB_BANNED");
        (await _context.JobPostings.SingleAsync(job => job.Id == seed.Job.Id)).ParsedData
            .Should().Be("{\"skills\":[\"Legacy\"]}");
        (await _context.JobSkillRequirements.SingleAsync(item => item.JobId == seed.Job.Id)).SkillId
            .Should().Be(102);
        _featureUsage.Verify(
            service => service.TryConsumeFeatureAsync(It.IsAny<Guid>(), "PostJob", It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task JobPostingRepository_UpdateTrackedEntity_DoesNotOverwriteConcurrentCounters()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<ITHunterviewContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        var jobId = Guid.NewGuid();

        await using (var seedContext = new TestContext(options))
        {
            seedContext.JobPostings.Add(new JobPostings
            {
                Id = jobId,
                JobCode = "JB-CONCURRENCY",
                RecruiterId = Guid.NewGuid(),
                CompanyId = Guid.NewGuid(),
                Title = "Original title",
                Description = "Description",
                Requirements = "Requirements",
                Benefits = string.Empty,
                Currency = "VND",
                Location = "HCMC",
                Status = JobStatus.PUBLISHED,
                ApplicationCount = 1,
                ViewCount = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await seedContext.SaveChangesAsync();
        }

        await using var editContext = new TestContext(options);
        var repository = new JobPostingRepository(editContext);
        var editingJob = await repository.GetByIdAsync(jobId);

        await using (var counterContext = new TestContext(options))
        {
            var counterJob = await counterContext.JobPostings.SingleAsync(job => job.Id == jobId);
            counterJob.ApplicationCount = 7;
            counterJob.ViewCount = 11;
            await counterContext.SaveChangesAsync();
        }

        editingJob!.Title = "Edited title";
        await repository.UpdateAsync(editingJob);

        await using var verifyContext = new TestContext(options);
        var persisted = await verifyContext.JobPostings.SingleAsync(job => job.Id == jobId);
        persisted.Title.Should().Be("Edited title");
        persisted.ApplicationCount.Should().Be(7);
        persisted.ViewCount.Should().Be(11);
    }

    private async Task<SeedResult> SeedAsync(
        JdAnalysisQuality quality,
        bool includeAcceptedSkill,
        bool publishedEdit = false)
    {
        var recruiterId = Guid.NewGuid();
        var company = new Companies
        {
            Id = Guid.NewGuid(),
            Name = "Test Company",
            TaxCode = Guid.NewGuid().ToString("N"),
            HeadquartersAddress = "HCMC",
            Industry = "Technology",
            CompanySize = "1-10",
            Description = "Test",
            Website = "https://example.test",
            LogoUrl = "https://example.test/logo.png",
            VerificationDocumentUrl = "https://example.test/document.pdf",
            Status = CompanyStatus.VERIFIED,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var job = new JobPostings
        {
            Id = Guid.NewGuid(),
            JobCode = Guid.NewGuid().ToString("N"),
            RecruiterId = recruiterId,
            CompanyId = company.Id,
            Title = "Backend Developer",
            Description = "Build backend services.",
            Requirements = "Java and Spring Boot.",
            Benefits = string.Empty,
            Currency = "VND",
            Location = "HCMC",
            Status = publishedEdit ? JobStatus.PUBLISHED : JobStatus.DRAFT,
            AnalysisRevision = 3,
            ParseStatus = "READY",
            EffectiveAnalysisRevision = publishedEdit ? 2 : null,
            EffectiveAnalysisRunId = publishedEdit ? Guid.NewGuid() : null,
            ParsedData = publishedEdit ? "{\"skills\":[\"Legacy\"]}" : null,
            PublishedAt = publishedEdit ? DateTime.UtcNow.AddDays(-10) : null,
            ExpiresAt = publishedEdit ? DateTime.UtcNow.AddDays(20) : null,
            ApplicationCount = publishedEdit ? 12 : 0,
            ViewCount = publishedEdit ? 34 : 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var run = new JobAnalysisRuns
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            InputRevision = job.AnalysisRevision,
            InputHash = "input-hash",
            Status = JobAnalysisStatus.READY,
            SchemaVersion = "jd-analysis/v5",
            RawInputSnapshot = "{}",
            RawAnalysisJson = "{}",
            EffectiveAnalysisJson = EffectiveAnalysis,
            AnalysisQuality = quality,
            RequestedBy = recruiterId,
            DecisionVersion = includeAcceptedSkill ? 1 : 0,
            CreatedAt = DateTime.UtcNow
        };
        job.ActiveAnalysisRunId = run.Id;

        _context.Companies.Add(company);
        _context.JobPostings.Add(job);
        _context.JobAnalysisRuns.Add(run);

        JobSkillDecisions? decision = null;
        if (includeAcceptedSkill)
        {
            var skill = new Skills
            {
                Id = 101,
                Name = "Java",
                NormalizedName = "java",
                Status = SkillStatus.ACTIVE
            };
            decision = new JobSkillDecisions
            {
                Id = Guid.NewGuid(),
                JobAnalysisRunId = run.Id,
                RawMention = "Java",
                NormalizedMention = "java",
                Category = "tech_skill",
                Importance = "must_have",
                SourceSection = "requirements",
                EvidenceText = "Thành thạo Java và Spring Boot.",
                ResolvedSkillId = skill.Id,
                ResolutionStatus = SkillResolutionStatus.EXACT_CANONICAL,
                DecisionStatus = SkillDecisionStatus.ACCEPTED,
                DecisionVersion = run.DecisionVersion,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Skills.Add(skill);
            _context.JobSkillDecisions.Add(decision);
        }

        if (publishedEdit)
        {
            var legacySkill = new Skills
            {
                Id = 102,
                Name = "Legacy",
                NormalizedName = "legacy",
                Status = SkillStatus.ACTIVE
            };
            _context.Skills.Add(legacySkill);
            _context.JobSkillRequirements.Add(new JobSkillRequirements
            {
                JobId = job.Id,
                SkillId = legacySkill.Id,
                IsMandatory = true
            });
        }

        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return new SeedResult(job, run, recruiterId, decision);
    }

    private static string Hash(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private sealed record SeedResult(
        JobPostings Job,
        JobAnalysisRuns Run,
        Guid RecruiterId,
        JobSkillDecisions? Decision);

    private sealed class TestContext : ITHunterviewContext
    {
        public TestContext(DbContextOptions<ITHunterviewContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var allowed = new HashSet<Type>
            {
                typeof(Companies),
                typeof(JobPostings),
                typeof(JobAnalysisRuns),
                typeof(JobSkillDecisions),
                typeof(JobSkillRequirements),
                typeof(Skills)
            };

            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                         .Where(type => !allowed.Contains(type.ClrType))
                         .Select(type => type.ClrType)
                         .Distinct()
                         .ToList())
            {
                modelBuilder.Ignore(entityType);
            }

            modelBuilder.Entity<JobPostings>(entity =>
            {
                entity.Ignore(job => job.TitleEmbedding);
                entity.Ignore(job => job.SkillsEmbedding);
                entity.Ignore(job => job.ExperienceEmbedding);
                entity.Ignore(job => job.DomainEmbedding);
            });
            modelBuilder.Entity<Skills>(entity =>
            {
                entity.Ignore(skill => skill.Category);
                entity.Ignore(skill => skill.Aliases);
            });
        }
    }
}
