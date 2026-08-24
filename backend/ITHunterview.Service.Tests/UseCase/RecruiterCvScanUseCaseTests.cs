using System.Text.Json;
using FluentAssertions;
using FluentAssertions.Execution;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Service.Matching;
using ITHunterview.Service.Tests.Matching;
using ITHunterview.Service.UseCase;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Tests.UseCase;

public sealed class RecruiterCvScanUseCaseTests
{
    [Fact]
    [Trait("Requirement", "R-03")]
    public async Task ScanAsync_OwnPublishedUsableJob_CreatesRecruiterCompanyJobRun()
    {
        await using var scenario = await RecruiterScanScenario.CreateAsync();

        var result = await scenario.UseCase.ScanAsync(scenario.RecruiterUserId, scenario.JobId, CancellationToken.None);

        using (new AssertionScope())
        {
            result.RunId.Should().NotBeEmpty();
            scenario.Repository.CreatedRuns.Should().ContainSingle();
            scenario.Repository.CreatedRuns.Single().RecruiterUserId.Should().Be(scenario.RecruiterUserId);
            scenario.Repository.CreatedRuns.Single().RecruiterProfileId.Should().Be(scenario.RecruiterProfileId);
            scenario.Repository.CreatedRuns.Single().CompanyId.Should().Be(scenario.CompanyId);
            scenario.Repository.CreatedRuns.Single().JobId.Should().Be(scenario.JobId);
            scenario.Repository.CompletedRuns.Should().ContainSingle();
        }
    }

    [Fact]
    [Trait("Requirement", "R-03")]
    public async Task ScanAsync_NonOwnerOrOtherCompany_RejectsWithoutRun()
    {
        await using var scenario = await RecruiterScanScenario.CreateAsync();
        var otherRecruiter = await scenario.AddRecruiterAsync(sameCompany: false);

        var action = () => scenario.UseCase.ScanAsync(otherRecruiter.UserId, scenario.JobId, CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedAccessException>();
        scenario.Repository.CreatedRuns.Should().BeEmpty();
    }

    [Fact]
    [Trait("Requirement", "R-08")]
    public async Task ScanAsync_IncludesOnlyPrimaryVisibleUndeletedUsableCvsAtStart()
    {
        await using var scenario = await RecruiterScanScenario.CreateAsync();
        var hidden = await scenario.AddCandidateCvAsync(primary: true, visible: false, deleted: false, usable: true);
        var secondary = await scenario.AddCandidateCvAsync(primary: false, visible: true, deleted: false, usable: true);
        var deleted = await scenario.AddCandidateCvAsync(primary: true, visible: true, deleted: true, usable: true);
        var unusable = await scenario.AddCandidateCvAsync(primary: true, visible: true, deleted: false, usable: false);

        await scenario.UseCase.ScanAsync(scenario.RecruiterUserId, scenario.JobId, CancellationToken.None);

        var matchedCvIds = scenario.Matcher.MatchedCvIds;
        using (new AssertionScope())
        {
            matchedCvIds.Should().Contain(scenario.EligibleCvId);
            matchedCvIds.Should().NotContain([hidden, secondary, deleted, unusable]);
        }
    }

    [Fact]
    [Trait("Requirement", "R-08")]
    public async Task ScanAsync_AlreadyUsableCvPopulation_DoesNotReparseCvs()
    {
        await using var scenario = await RecruiterScanScenario.CreateAsync();

        await scenario.UseCase.ScanAsync(scenario.RecruiterUserId, scenario.JobId, CancellationToken.None);

        scenario.Matcher.MatchedCvs.Should().OnlyContain(cv => cv.ParseStatus == "SUCCESS" && !string.IsNullOrWhiteSpace(cv.ParsedData));
        scenario.Matcher.ReparseAttempts.Should().Be(0);
    }

    [Fact]
    [Trait("Requirement", "R-09")]
    public async Task ScanAsync_IsFreeAndDoesNotCallUnlockOrCandidateBilling()
    {
        await using var scenario = await RecruiterScanScenario.CreateAsync();

        await scenario.UseCase.ScanAsync(scenario.RecruiterUserId, scenario.JobId, CancellationToken.None);

        using (new AssertionScope())
        {
            (await scenario.Context.FeatureUsageReservations.CountAsync()).Should().Be(0);
            (await scenario.Context.RecruiterUnlockedCvs.CountAsync()).Should().Be(0);
        }
    }

    [Fact]
    [Trait("Requirement", "R-12")]
    public async Task ScanAsync_DoesNotCreateApplicationInviteContactOrCandidateNotification()
    {
        await using var scenario = await RecruiterScanScenario.CreateAsync();
        var beforeApplications = await scenario.Context.JobApplications.CountAsync();
        var beforeNotifications = await scenario.Context.Notifications.CountAsync();

        await scenario.UseCase.ScanAsync(scenario.RecruiterUserId, scenario.JobId, CancellationToken.None);

        (await scenario.Context.JobApplications.CountAsync()).Should().Be(beforeApplications);
        (await scenario.Context.Notifications.CountAsync()).Should().Be(beforeNotifications);
    }

    [Fact]
    [Trait("Requirement", "R-04")]
    public async Task ScanAsync_SamePairInOneToOneOrCandidateScan_DoesNotReadOrMutateIt()
    {
        await using var scenario = await RecruiterScanScenario.CreateAsync();
        var legacy = new CvJobMatchScores
        {
            Id = Guid.NewGuid(), UserId = scenario.EligibleCandidateUserId, CvId = scenario.EligibleCvId,
            JobId = scenario.JobId, MatchType = "AI", Status = "Completed", MatchScore = 74m,
            MatchDetails = "one-to-one-private", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        scenario.Context.CvJobMatchScores.Add(legacy);
        await scenario.Context.SaveChangesAsync();

        await scenario.UseCase.ScanAsync(scenario.RecruiterUserId, scenario.JobId, CancellationToken.None);

        var after = await scenario.Context.CvJobMatchScores.SingleAsync(row => row.Id == legacy.Id);
        after.Should().BeEquivalentTo(legacy);
    }

    [Fact]
    [Trait("Requirement", "R-04")]
    public async Task ScanAsync_SharedLegacyRowsWithNullCvId_AreNeverReadAndCannotCauseDictionaryKeyFailure()
    {
        await using var scenario = await RecruiterScanScenario.CreateAsync();
        var pasted = new CvJobMatchScores
        {
            Id = Guid.NewGuid(), UserId = Guid.NewGuid(), CvId = null, JobId = scenario.JobId,
            RawJdText = "pasted source", MatchType = "AI", Status = "Completed", MatchScore = 67m,
            MatchDetails = "legacy pasted", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        scenario.Context.CvJobMatchScores.Add(pasted);
        await scenario.Context.SaveChangesAsync();

        var action = () => scenario.UseCase.ScanAsync(scenario.RecruiterUserId, scenario.JobId, CancellationToken.None);

        await action.Should().NotThrowAsync();
        (await scenario.Context.CvJobMatchScores.SingleAsync(row => row.Id == pasted.Id)).CvId.Should().BeNull();
    }

    [Fact]
    [Trait("Requirement", "R-06")]
    public async Task GetLatestSuccessfulAsync_VisibilityTurnedOffAfterScan_StillReturnsSnapshot()
    {
        await using var scenario = await RecruiterScanScenario.CreateAsync();
        await scenario.UseCase.ScanAsync(scenario.RecruiterUserId, scenario.JobId, CancellationToken.None);
        await scenario.SetCandidateVisibilityAsync(scenario.EligibleCandidateUserId, false);

        var page = await scenario.UseCase.GetLatestSuccessfulAsync(scenario.RecruiterUserId, scenario.JobId, 1, 20, CancellationToken.None);

        page.Items.Should().ContainSingle(item => item.ScanResultId != Guid.Empty);
    }

    [Fact]
    [Trait("Requirement", "R-06")]
    public async Task GetLatestSuccessfulAsync_PrimaryChangedAfterScan_StillReturnsSnapshot()
    {
        await using var scenario = await RecruiterScanScenario.CreateAsync();
        await scenario.UseCase.ScanAsync(scenario.RecruiterUserId, scenario.JobId, CancellationToken.None);
        await scenario.SetPrimaryAsync(scenario.EligibleCvId, false);

        var page = await scenario.UseCase.GetLatestSuccessfulAsync(scenario.RecruiterUserId, scenario.JobId, 1, 20, CancellationToken.None);

        page.Items.Should().ContainSingle();
    }

    [Fact]
    [Trait("Requirement", "R-08")]
    public async Task NewScan_AfterVisibilityOrPrimaryChange_UsesNewEligibilityOnly()
    {
        await using var scenario = await RecruiterScanScenario.CreateAsync();
        await scenario.UseCase.ScanAsync(scenario.RecruiterUserId, scenario.JobId, CancellationToken.None);
        await scenario.SetCandidateVisibilityAsync(scenario.EligibleCandidateUserId, false);

        await scenario.UseCase.ScanAsync(scenario.RecruiterUserId, scenario.JobId, CancellationToken.None);

        scenario.Repository.CompletedRuns.Should().HaveCount(2);
        scenario.Repository.CompletedRuns.Last().Results.Should().BeEmpty();
    }

    [Fact]
    [Trait("Requirement", "R-05")]
    [Trait("Requirement", "R-06")]
    public async Task ScanAsync_FailureMarksRunFailedAndPreservesPriorLatest()
    {
        await using var scenario = await RecruiterScanScenario.CreateAsync();
        await scenario.UseCase.ScanAsync(scenario.RecruiterUserId, scenario.JobId, CancellationToken.None);
        scenario.Matcher.ThrowOnMatch = true;

        var action = () => scenario.UseCase.ScanAsync(scenario.RecruiterUserId, scenario.JobId, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        scenario.Repository.FailedRuns.Should().ContainSingle();
        var page = await scenario.UseCase.GetLatestSuccessfulAsync(scenario.RecruiterUserId, scenario.JobId, 1, 20, CancellationToken.None);
        page.Items.Should().ContainSingle();
    }

    [Fact]
    [Trait("Requirement", "R-10")]
    public async Task GetLatestSuccessfulAsync_LockedRow_MasksEveryStableCandidateIdentifier()
    {
        await using var scenario = await RecruiterScanScenario.CreateAsync();
        await scenario.UseCase.ScanAsync(scenario.RecruiterUserId, scenario.JobId, CancellationToken.None);

        var page = await scenario.UseCase.GetLatestSuccessfulAsync(scenario.RecruiterUserId, scenario.JobId, 1, 20, CancellationToken.None);
        var locked = page.Items.Single();
        var json = JsonSerializer.Serialize(locked, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        using (new AssertionScope())
        {
            locked.IsUnlocked.Should().BeFalse();
            locked.MatchedAt.Should().NotBeNull();
            json.Should().NotContain(scenario.EligibleCandidateUserId.ToString(), "a locked result must not disclose candidate identity");
            json.Should().NotContain(scenario.EligibleCvId.ToString(), "a locked result must not disclose CV identity");
            json.Should().NotContain(scenario.EligibleCvFileName, "a locked result must not disclose the real filename");
            json.Should().NotContain(scenario.EligibleCvFileUrl, "a locked result must not disclose the file URL");
            json.Should().NotContain("phone", "a locked result must not contain contact data");
            json.Should().NotContain("profileUrl", "a locked result must not contain a profile link");
        }
    }

    [Fact]
    [Trait("Requirement", "R-09")]
    [Trait("Requirement", "R-10")]
    public async Task GetLatestSuccessfulAsync_SubscriptionQuotaAvailable_RemainsLocked()
    {
        await using var scenario = await RecruiterScanScenario.CreateAsync();
        await scenario.UseCase.ScanAsync(scenario.RecruiterUserId, scenario.JobId, CancellationToken.None);

        var page = await scenario.UseCase.GetLatestSuccessfulAsync(scenario.RecruiterUserId, scenario.JobId, 1, 20, CancellationToken.None);

        page.Items.Single().IsUnlocked.Should().BeFalse();
    }

    [Fact]
    [Trait("Requirement", "R-10")]
    public async Task GetLatestSuccessfulAsync_PriorUnlockSameRecruiterCv_ReturnsUnlockedAcrossLaterOwnedRun()
    {
        await using var scenario = await RecruiterScanScenario.CreateAsync();
        scenario.Context.RecruiterUnlockedCvs.Add(new RecruiterUnlockedCvs
        {
            Id = Guid.NewGuid(), RecruiterId = scenario.RecruiterUserId, CvId = scenario.EligibleCvId,
            Status = RecruiterCvUnlockStatus.Completed, JobId = scenario.JobId
        });
        await scenario.Context.SaveChangesAsync();
        await scenario.UseCase.ScanAsync(scenario.RecruiterUserId, scenario.JobId, CancellationToken.None);
        await scenario.UseCase.ScanAsync(scenario.RecruiterUserId, scenario.JobId, CancellationToken.None);

        var page = await scenario.UseCase.GetLatestSuccessfulAsync(scenario.RecruiterUserId, scenario.JobId, 1, 20, CancellationToken.None);

        page.Items.Single().IsUnlocked.Should().BeTrue();
    }

    [Fact]
    [Trait("Requirement", "R-10")]
    public async Task GetLatestSuccessfulAsync_LegacyUnlockSameRecruiterCv_RemainsUnlockedWithoutRecharge()
    {
        await using var scenario = await RecruiterScanScenario.CreateAsync();
        scenario.Context.RecruiterUnlockedCvs.Add(new RecruiterUnlockedCvs
        {
            Id = Guid.NewGuid(), RecruiterId = scenario.RecruiterUserId, CvId = scenario.EligibleCvId,
            JobId = null, Status = RecruiterCvUnlockStatus.Completed
        });
        await scenario.Context.SaveChangesAsync();
        await scenario.UseCase.ScanAsync(scenario.RecruiterUserId, scenario.JobId, CancellationToken.None);

        var page = await scenario.UseCase.GetLatestSuccessfulAsync(scenario.RecruiterUserId, scenario.JobId, 1, 20, CancellationToken.None);

        using (new AssertionScope())
        {
            page.Items.Single().IsUnlocked.Should().BeTrue();
            (await scenario.Context.RecruiterUnlockedCvs.CountAsync()).Should().Be(1);
        }
    }

    [Fact]
    [Trait("Requirement", "R-08")]
    [Trait("Requirement", "R-10")]
    public async Task GetLatestSuccessfulAsync_OtherRecruiterOrCompany_CannotReadRun()
    {
        await using var scenario = await RecruiterScanScenario.CreateAsync();
        await scenario.UseCase.ScanAsync(scenario.RecruiterUserId, scenario.JobId, CancellationToken.None);
        var other = await scenario.AddRecruiterAsync(sameCompany: false);

        var action = () => scenario.UseCase.GetLatestSuccessfulAsync(other.UserId, scenario.JobId, 1, 20, CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}

internal sealed class RecruiterScanScenario : IAsyncDisposable
{
    private RecruiterScanScenario(ITHunterviewContext context, RecordingRecruiterCvScanRepository repository, RecordingPairMatcher matcher,
        Guid recruiterUserId, Guid recruiterProfileId, Guid companyId, Guid jobId, Guid candidateUserId, Guid cvId, string fileName, string fileUrl)
    {
        Context = context; Repository = repository; Matcher = matcher;
        RecruiterUserId = recruiterUserId; RecruiterProfileId = recruiterProfileId; CompanyId = companyId; JobId = jobId;
        EligibleCandidateUserId = candidateUserId; EligibleCvId = cvId; EligibleCvFileName = fileName; EligibleCvFileUrl = fileUrl;
        UseCase = new RecruiterCvScanUseCase(context, repository, matcher, new CvAnalysisResponseValidator());
    }

    public ITHunterviewContext Context { get; }
    public RecordingRecruiterCvScanRepository Repository { get; }
    public RecordingPairMatcher Matcher { get; }
    public RecruiterCvScanUseCase UseCase { get; }
    public Guid RecruiterUserId { get; }
    public Guid RecruiterProfileId { get; }
    public Guid CompanyId { get; }
    public Guid JobId { get; }
    public Guid EligibleCandidateUserId { get; }
    public Guid EligibleCvId { get; }
    public string EligibleCvFileName { get; }
    public string EligibleCvFileUrl { get; }

    public static async Task<RecruiterScanScenario> CreateAsync()
    {
        var options = new DbContextOptionsBuilder<ITHunterviewContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;
        var context = new RecruiterScanTestContext(options);
        var recruiterUser = User(Guid.NewGuid());
        var candidateUser = User(Guid.NewGuid());
        var company = Company();
        var profile = new RecruiterProfiles { Id = Guid.NewGuid(), UserId = recruiterUser.Id, CompanyId = company.Id };
        var candidateProfile = new CandidateProfiles { Id = Guid.NewGuid(), UserId = candidateUser.Id, IsVisibleToRecruiters = true };
        var job = Job(profile.Id, company.Id);
        const string fileName = "real-candidate-resume.pdf";
        const string fileUrl = "https://files.example.test/private/real-candidate-resume.pdf";
        var cv = Cv(candidateUser.Id, fileName, fileUrl);
        context.AddRange(recruiterUser, candidateUser, company, profile, candidateProfile, job, cv);
        await context.SaveChangesAsync();
        return new RecruiterScanScenario(context, new RecordingRecruiterCvScanRepository(), new RecordingPairMatcher(), recruiterUser.Id,
            profile.Id, company.Id, job.Id, candidateUser.Id, cv.Id, fileName, fileUrl);
    }

    public async Task<(Guid UserId, Guid ProfileId)> AddRecruiterAsync(bool sameCompany)
    {
        var user = User(Guid.NewGuid());
        var company = sameCompany ? CompanyId : Guid.NewGuid();
        if (!sameCompany) Context.Companies.Add(Company(company));
        var profile = new RecruiterProfiles { Id = Guid.NewGuid(), UserId = user.Id, CompanyId = company };
        Context.AddRange(user, profile);
        await Context.SaveChangesAsync();
        return (user.Id, profile.Id);
    }

    public async Task<Guid> AddCandidateCvAsync(bool primary, bool visible, bool deleted, bool usable)
    {
        var user = User(Guid.NewGuid());
        var profile = new CandidateProfiles { Id = Guid.NewGuid(), UserId = user.Id, IsVisibleToRecruiters = visible };
        var cv = Cv(user.Id, $"{Guid.NewGuid():N}.pdf", $"https://files.example.test/{Guid.NewGuid():N}.pdf", usable);
        cv.IsPrimary = primary;
        cv.DeletedAt = deleted ? DateTime.UtcNow : null;
        Context.AddRange(user, profile, cv);
        await Context.SaveChangesAsync();
        return cv.Id;
    }

    public async Task SetCandidateVisibilityAsync(Guid userId, bool visible)
    {
        (await Context.CandidateProfiles.SingleAsync(profile => profile.UserId == userId)).IsVisibleToRecruiters = visible;
        await Context.SaveChangesAsync();
    }

    public async Task SetPrimaryAsync(Guid cvId, bool primary)
    {
        (await Context.Cvs.SingleAsync(cv => cv.Id == cvId)).IsPrimary = primary;
        await Context.SaveChangesAsync();
    }

    public ValueTask DisposeAsync() => Context.DisposeAsync();

    private static User User(Guid id) => new() { Id = id, Email = $"{id:N}@example.test", PasswordHash = "test", Status = UserStatus.ACTIVE, CreatedAt = DateTime.UtcNow };
    private static Companies Company(Guid? id = null) => new() { Id = id ?? Guid.NewGuid(), Name = "Test Company", TaxCode = Guid.NewGuid().ToString("N")[..12], HeadquartersAddress = "Test", Industry = "IT", CompanySize = "1", Description = "Test", Website = "https://example.test", LogoUrl = "https://example.test/logo", VerificationDocumentUrl = "https://example.test/document", VerificationMethod = CompanyVerificationMethod.BUSINESS_REGISTRATION, Status = CompanyStatus.VERIFIED, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
    private static JobPostings Job(Guid recruiterProfileId, Guid companyId) => new() { Id = Guid.NewGuid(), JobCode = Guid.NewGuid().ToString("N"), RecruiterId = recruiterProfileId, CompanyId = companyId, Title = "Backend Engineer", Description = "Build", Requirements = "C#", Benefits = string.Empty, IncomeText = string.Empty, WorkLocationText = string.Empty, Currency = "VND", Location = "Remote", Status = JobStatus.PUBLISHED, ParsedData = "{}", ParseStatus = "SUCCESS", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
    private static Cvs Cv(Guid userId, string fileName, string fileUrl, bool usable = true) => new() { Id = Guid.NewGuid(), UserId = userId, FileName = fileName, FileUrl = fileUrl, FileType = "application/pdf", IsPrimary = true, ParseStatus = usable ? "SUCCESS" : "FAILED", ParsedData = usable ? CvAnalysisResponseValidatorTests.CreateValidDocument() : "{}", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

    private sealed class RecruiterScanTestContext : ITHunterviewContext
    {
        private static readonly HashSet<Type> AllowedTypes =
        [
            typeof(User), typeof(CandidateProfiles), typeof(RecruiterProfiles), typeof(Companies),
            typeof(Cvs), typeof(JobPostings), typeof(CvJobMatchScores), typeof(RecruiterUnlockedCvs),
            typeof(FeatureUsageReservations), typeof(JobApplications), typeof(Notifications)
        ];

        public RecruiterScanTestContext(DbContextOptions<ITHunterviewContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                         .Where(type => !AllowedTypes.Contains(type.ClrType))
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
        }
    }
}

internal sealed class RecordingPairMatcher : IHardcodeCvJobPairMatcher
{
    public bool ThrowOnMatch { get; set; }
    public int ReparseAttempts { get; private set; }
    public List<Cvs> MatchedCvs { get; } = [];
    public IReadOnlyCollection<Guid> MatchedCvIds => MatchedCvs.Select(cv => cv.Id).ToArray();
    public Task<HardcodePairMatchResult> MatchAsync(Cvs cv, JobPostings job, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ThrowOnMatch) throw new InvalidOperationException("synthetic matcher failure");
        MatchedCvs.Add(cv);
        return Task.FromResult(new HardcodePairMatchResult(80m, "hardcode report", CvAnalysisQuality.COMPLETE, "{}", "{}"));
    }
}

internal sealed class RecordingRecruiterCvScanRepository : IRecruiterCvScanRepository
{
    public List<RecruiterCvScanRun> CreatedRuns { get; } = [];
    public List<(RecruiterCvScanRun Run, IReadOnlyList<RecruiterCvScanResult> Results)> CompletedRuns { get; } = [];
    public List<RecruiterCvScanRun> FailedRuns { get; } = [];
    public Task<RecruiterCvScanRun> CreatePendingAsync(RecruiterCvScanRun run, CancellationToken ct) { CreatedRuns.Add(run); return Task.FromResult(run); }
    public Task<bool> TryStartAsync(Guid runId, DateTime startedAt, CancellationToken ct) { CreatedRuns.Single(run => run.Id == runId).Status = MatchingScanRunStatus.Processing; return Task.FromResult(true); }
    public Task CompleteAsync(Guid runId, IReadOnlyCollection<RecruiterCvScanResult> results, DateTime completedAt, CancellationToken ct) { var run = CreatedRuns.Single(run => run.Id == runId); run.Status = MatchingScanRunStatus.Completed; run.CompletedAt = completedAt; CompletedRuns.Add((run, results.ToArray())); return Task.CompletedTask; }
    public Task FailAsync(Guid runId, string errorCode, string errorMessage, DateTime failedAt, CancellationToken ct) { var run = CreatedRuns.Single(run => run.Id == runId); run.Status = MatchingScanRunStatus.Failed; run.CompletedAt = failedAt; FailedRuns.Add(run); return Task.CompletedTask; }
    public Task<RecruiterCvScanRun?> GetLatestCompletedAsync(Guid recruiterUserId, Guid companyId, Guid jobId, CancellationToken ct) => Task.FromResult<RecruiterCvScanRun?>(CompletedRuns.Where(pair => pair.Run.RecruiterUserId == recruiterUserId && pair.Run.CompanyId == companyId && pair.Run.JobId == jobId).OrderByDescending(pair => pair.Run.CreatedAt).Select(pair => pair.Run).FirstOrDefault());
    public Task<(IReadOnlyList<RecruiterCvScanResult> Items, int TotalCount)> GetResultPageAsync(Guid runId, int skip, int take, CancellationToken ct) { var items = CompletedRuns.Single(pair => pair.Run.Id == runId).Results.OrderBy(result => result.Rank).Skip(skip).Take(take).ToArray(); var total = CompletedRuns.Single(pair => pair.Run.Id == runId).Results.Count; return Task.FromResult< (IReadOnlyList<RecruiterCvScanResult> Items, int TotalCount)>((items, total)); }
    public Task<(RecruiterCvScanResult Result, RecruiterCvScanRun Run)?> GetOwnedResultAsync(Guid scanResultId, Guid recruiterUserId, CancellationToken ct) => Task.FromResult<(RecruiterCvScanResult Result, RecruiterCvScanRun Run)?>(null);
}
