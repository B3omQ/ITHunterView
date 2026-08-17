using System.Collections.Concurrent;
using System.Data.Common;
using System.Text.RegularExpressions;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace ITHunterview.Service.Tests.Persistence;

[AttributeUsage(AttributeTargets.Method)]
internal sealed class Task6PostgresFactAttribute : FactAttribute
{
    public Task6PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable(MatchingScanPostgresFixture.AdminConnectionEnvironmentVariable)))
        {
            Skip = $"Set {MatchingScanPostgresFixture.AdminConnectionEnvironmentVariable} to run Task 6 PostgreSQL tests.";
        }
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class MatchingScanPostgresCollection : ICollectionFixture<MatchingScanPostgresFixture>
{
    public const string Name = "Task 6 matching scan PostgreSQL";
}

public sealed class MatchingScanPostgresFixture : IAsyncLifetime
{
    public const string AdminConnectionEnvironmentVariable = "ITHUNTERVIEW_TEST_POSTGRES_ADMIN_CONNECTION";

    private static readonly Regex DatabaseNamePattern = new(
        "^ithv_task6_[a-f0-9]{32}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    private string? _adminConnectionString;
    private string? _databaseConnectionString;
    private bool _databaseCreated;

    public string DatabaseName { get; } = $"ithv_task6_{Guid.NewGuid():N}";

    public async Task InitializeAsync()
    {
        ValidateDatabaseName(DatabaseName);

        var configuredConnection = Environment.GetEnvironmentVariable(AdminConnectionEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(configuredConnection))
        {
            return;
        }

        var adminBuilder = BuildConnectionString(configuredConnection);
        adminBuilder.Database = "postgres";

        var databaseBuilder = BuildConnectionString(configuredConnection);
        databaseBuilder.Database = DatabaseName;

        _adminConnectionString = adminBuilder.ConnectionString;
        _databaseConnectionString = databaseBuilder.ConnectionString;

        try
        {
            await using var adminConnection = new NpgsqlConnection(_adminConnectionString);
            await adminConnection.OpenAsync();

            if (await CountDatabaseAsync(adminConnection, DatabaseName) != 0)
            {
                throw new InvalidOperationException("The generated Task 6 disposable database name already exists; it will not be reused.");
            }

            await using (var createCommand = new NpgsqlCommand(
                             $"CREATE DATABASE \"{DatabaseName}\"",
                             adminConnection))
            {
                await createCommand.ExecuteNonQueryAsync();
            }

            _databaseCreated = true;

            await using var context = CreateContext();
            await context.Database.MigrateAsync();
        }
        catch
        {
            await DropCreatedDatabaseAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        await DropCreatedDatabaseAsync();
    }

    public ITHunterviewContext CreateContext(DbCommandInterceptor? commandInterceptor = null)
    {
        if (!_databaseCreated || string.IsNullOrWhiteSpace(_databaseConnectionString))
        {
            throw new InvalidOperationException("The Task 6 disposable PostgreSQL database is not initialized.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<ITHunterviewContext>()
            .UseNpgsql(_databaseConnectionString, options => options.UseVector());
        if (commandInterceptor is not null)
        {
            optionsBuilder.AddInterceptors(commandInterceptor);
        }

        return new ITHunterviewContext(optionsBuilder.Options);
    }

    public async Task<MatchingScanSeed> SeedGraphAsync(CancellationToken ct = default)
    {
        await using var context = CreateContext();
        var suffix = Guid.NewGuid().ToString("N");
        var now = new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc);

        var candidate = CreateUser($"candidate-{suffix}@task6.invalid", now);
        var otherCandidate = CreateUser($"candidate-other-{suffix}@task6.invalid", now);
        var thirdCandidate = CreateUser($"candidate-third-{suffix}@task6.invalid", now);
        var recruiter = CreateUser($"recruiter-{suffix}@task6.invalid", now);
        var company = new Companies
        {
            Id = Guid.NewGuid(),
            Name = $"Task 6 Company {suffix}",
            TaxCode = $"T6{suffix[..20]}",
            HeadquartersAddress = "Synthetic address",
            Industry = "Software",
            CompanySize = "1-10",
            Description = "Synthetic Task 6 test company",
            Website = "https://task6.invalid",
            LogoUrl = "https://task6.invalid/logo.png",
            VerificationMethod = CompanyVerificationMethod.BUSINESS_REGISTRATION,
            VerificationDocumentUrl = "https://task6.invalid/document.pdf",
            Status = CompanyStatus.VERIFIED,
            CreatedAt = now,
            UpdatedAt = now
        };
        var recruiterProfile = new RecruiterProfiles
        {
            Id = Guid.NewGuid(),
            UserId = recruiter.Id,
            CompanyId = company.Id,
            FullName = "Synthetic Recruiter"
        };
        var cv = CreateCv(candidate.Id, "candidate.pdf", now);
        var otherCv = CreateCv(otherCandidate.Id, "candidate-other.pdf", now);
        var thirdCv = CreateCv(thirdCandidate.Id, "candidate-third.pdf", now);
        var job = CreateJob(recruiterProfile.Id, company.Id, $"T6-{suffix[..12]}", "Backend Engineer", now);
        var otherJob = CreateJob(recruiterProfile.Id, company.Id, $"T6B-{suffix[..12]}", "Platform Engineer", now);
        var thirdJob = CreateJob(recruiterProfile.Id, company.Id, $"T6C-{suffix[..12]}", "Data Engineer", now);

        context.AddRange(
            candidate,
            otherCandidate,
            thirdCandidate,
            recruiter,
            company,
            recruiterProfile,
            cv,
            otherCv,
            thirdCv,
            job,
            otherJob,
            thirdJob);
        await context.SaveChangesAsync(ct);

        return new MatchingScanSeed(
            candidate.Id,
            cv.Id,
            otherCandidate.Id,
            otherCv.Id,
            thirdCandidate.Id,
            thirdCv.Id,
            recruiter.Id,
            recruiterProfile.Id,
            company.Id,
            job.Id,
            otherJob.Id,
            thirdJob.Id);
    }

    private static NpgsqlConnectionStringBuilder BuildConnectionString(string configuredConnection)
    {
        var builder = new NpgsqlConnectionStringBuilder(configuredConnection)
        {
            ApplicationName = "ITHunterview.Task6.RepositoryTests",
            IncludeErrorDetail = false,
            Pooling = false
        };

        return builder;
    }

    private async Task DropCreatedDatabaseAsync()
    {
        if (!_databaseCreated || string.IsNullOrWhiteSpace(_adminConnectionString))
        {
            return;
        }

        ValidateDatabaseName(DatabaseName);

        await using var adminConnection = new NpgsqlConnection(_adminConnectionString);
        await adminConnection.OpenAsync();
        await using (var dropCommand = new NpgsqlCommand(
                         $"DROP DATABASE \"{DatabaseName}\" WITH (FORCE)",
                         adminConnection))
        {
            await dropCommand.ExecuteNonQueryAsync();
        }

        var remaining = await CountDatabaseAsync(adminConnection, DatabaseName);
        _databaseCreated = false;
        if (remaining != 0)
        {
            throw new InvalidOperationException("Task 6 disposable PostgreSQL database cleanup did not reach remaining=0.");
        }
    }

    private static async Task<int> CountDatabaseAsync(NpgsqlConnection connection, string databaseName)
    {
        await using var command = new NpgsqlCommand(
            "SELECT COUNT(*) FROM pg_database WHERE datname = @database_name",
            connection);
        command.Parameters.AddWithValue("database_name", databaseName);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static void ValidateDatabaseName(string databaseName)
    {
        if (!DatabaseNamePattern.IsMatch(databaseName))
        {
            throw new InvalidOperationException("Refusing to create or drop an invalid Task 6 disposable database name.");
        }
    }

    private static User CreateUser(string email, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        Email = email,
        PasswordHash = "synthetic-task6-hash",
        Status = UserStatus.ACTIVE,
        CreatedAt = now
    };

    private static Cvs CreateCv(Guid userId, string fileName, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        FileUrl = $"https://task6.invalid/{fileName}",
        FileName = fileName,
        FileType = "application/pdf",
        ParsedData = "{}",
        ParseStatus = "SUCCESS",
        CreatedAt = now,
        UpdatedAt = now
    };

    private static JobPostings CreateJob(
        Guid recruiterProfileId,
        Guid companyId,
        string jobCode,
        string title,
        DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        RecruiterId = recruiterProfileId,
        CompanyId = companyId,
        JobCode = jobCode,
        Title = title,
        Description = "Synthetic description",
        Requirements = "Synthetic requirements",
        Benefits = string.Empty,
        IncomeText = string.Empty,
        WorkLocationText = string.Empty,
        Currency = "VND",
        Location = "Synthetic location",
        Status = JobStatus.PUBLISHED,
        CreatedAt = now,
        UpdatedAt = now
    };
}

internal sealed class MatchingScanUpdateBarrier
{
    private static readonly TimeSpan ArrivalTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ParticipantTimeout = TimeSpan.FromSeconds(15);
    private static readonly HashSet<string> AllowedTableNames =
    [
        "candidate_job_scan_runs",
        "recruiter_cv_scan_runs"
    ];

    private readonly Regex _targetUpdatePattern;
    private readonly ConcurrentDictionary<int, byte> _arrivedParticipants = new();
    private readonly TaskCompletionSource _bothParticipantsArrived =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _release =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _createdParticipantCount;
    private int _released;

    public MatchingScanUpdateBarrier(string exactTableName)
    {
        if (!AllowedTableNames.Contains(exactTableName))
        {
            throw new ArgumentOutOfRangeException(
                nameof(exactTableName),
                "The Task 6 race barrier accepts only an exact matching scan run table.");
        }

        _targetUpdatePattern = new Regex(
            $@"(?:^|;)\s*UPDATE\s+""?{Regex.Escape(exactTableName)}""?(?:\s|$)",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled,
            TimeSpan.FromMilliseconds(100));
    }

    public static TimeSpan RaceCompletionTimeout => ParticipantTimeout;

    public int ArrivedParticipantCount => _arrivedParticipants.Count;

    public bool IsReleased => Volatile.Read(ref _released) != 0;

    public DbCommandInterceptor CreateParticipant()
    {
        var participantId = Interlocked.Increment(ref _createdParticipantCount);
        if (participantId > 2)
        {
            throw new InvalidOperationException("The Task 6 race barrier supports exactly two participants.");
        }

        return new ParticipantCommandInterceptor(this, participantId);
    }

    public Task WaitForBothParticipantsAsync(CancellationToken ct = default) =>
        _bothParticipantsArrived.Task.WaitAsync(ArrivalTimeout, ct);

    public void Release()
    {
        Interlocked.Exchange(ref _released, 1);
        _release.TrySetResult();
    }

    private async ValueTask WaitAtTargetUpdateAsync(
        int participantId,
        DbCommand command,
        CancellationToken ct)
    {
        if (!_targetUpdatePattern.IsMatch(command.CommandText) ||
            !_arrivedParticipants.TryAdd(participantId, 0))
        {
            return;
        }

        if (_arrivedParticipants.Count == 2)
        {
            _bothParticipantsArrived.TrySetResult();
        }

        await _release.Task.WaitAsync(ParticipantTimeout, ct);
    }

    private sealed class ParticipantCommandInterceptor : DbCommandInterceptor
    {
        private readonly MatchingScanUpdateBarrier _barrier;
        private readonly int _participantId;

        public ParticipantCommandInterceptor(MatchingScanUpdateBarrier barrier, int participantId)
        {
            _barrier = barrier;
            _participantId = participantId;
        }

        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result)
        {
            _barrier.WaitAtTargetUpdateAsync(_participantId, command, CancellationToken.None)
                .AsTask()
                .GetAwaiter()
                .GetResult();
            return result;
        }

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            await _barrier.WaitAtTargetUpdateAsync(_participantId, command, cancellationToken);
            return result;
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            _barrier.WaitAtTargetUpdateAsync(_participantId, command, CancellationToken.None)
                .AsTask()
                .GetAwaiter()
                .GetResult();
            return result;
        }

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            await _barrier.WaitAtTargetUpdateAsync(_participantId, command, cancellationToken);
            return result;
        }

        public override InterceptionResult<object> ScalarExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result)
        {
            _barrier.WaitAtTargetUpdateAsync(_participantId, command, CancellationToken.None)
                .AsTask()
                .GetAwaiter()
                .GetResult();
            return result;
        }

        public override async ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
        {
            await _barrier.WaitAtTargetUpdateAsync(_participantId, command, cancellationToken);
            return result;
        }
    }
}

internal static class MatchingScanRepositoryFactory
{
    public static ICandidateJobScanRepository Candidate(ITHunterviewContext context) =>
        new CandidateJobScanRepository(context);

    public static IRecruiterCvScanRepository Recruiter(ITHunterviewContext context) =>
        new RecruiterCvScanRepository(context);
}

internal static class MatchingScanInMemoryContextFactory
{
    public static ITHunterviewContext Create()
    {
        var options = new DbContextOptionsBuilder<ITHunterviewContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new MatchingScanRepositoryTestContext(options);
    }

    private sealed class MatchingScanRepositoryTestContext : ITHunterviewContext
    {
        private static readonly HashSet<Type> AllowedTypes =
        [
            typeof(CandidateJobScanRun),
            typeof(CandidateJobScanResult),
            typeof(RecruiterCvScanRun),
            typeof(RecruiterCvScanResult)
        ];

        public MatchingScanRepositoryTestContext(DbContextOptions<ITHunterviewContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                         .Where(type => !AllowedTypes.Contains(type.ClrType))
                         .Select(type => type.ClrType)
                         .Distinct()
                         .ToList())
            {
                modelBuilder.Ignore(entityType);
            }

            modelBuilder.Entity<CandidateJobScanRun>().HasKey(run => run.Id);
            modelBuilder.Entity<CandidateJobScanResult>().HasKey(result => result.Id);
            modelBuilder.Entity<RecruiterCvScanRun>().HasKey(run => run.Id);
            modelBuilder.Entity<RecruiterCvScanResult>().HasKey(result => result.Id);
        }
    }
}

public sealed record MatchingScanSeed(
    Guid CandidateUserId,
    Guid CvId,
    Guid OtherCandidateUserId,
    Guid OtherCvId,
    Guid ThirdCandidateUserId,
    Guid ThirdCvId,
    Guid RecruiterUserId,
    Guid RecruiterProfileId,
    Guid CompanyId,
    Guid JobId,
    Guid OtherJobId,
    Guid ThirdJobId);
