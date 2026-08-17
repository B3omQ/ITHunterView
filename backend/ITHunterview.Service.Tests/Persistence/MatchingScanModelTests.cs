using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ITHunterview.Service.Tests.Persistence;

public sealed class MatchingScanModelTests
{
    [Fact]
    [Trait("Requirement", "R-01")]
    public void Model_HasSeparateTablesForCandidateAndRecruiterScans()
    {
        using var context = CreateContext();

        var expectedTables = new Dictionary<Type, string>
        {
            [typeof(CandidateJobScanRun)] = "candidate_job_scan_runs",
            [typeof(CandidateJobScanResult)] = "candidate_job_scan_results",
            [typeof(RecruiterCvScanRun)] = "recruiter_cv_scan_runs",
            [typeof(RecruiterCvScanResult)] = "recruiter_cv_scan_results"
        };

        foreach (var (clrType, tableName) in expectedTables)
        {
            var entity = GetModel(context).FindEntityType(clrType);
            entity.Should().NotBeNull();
            entity!.GetTableName().Should().Be(tableName);
        }

        typeof(ITHunterviewContext).GetProperty(nameof(ITHunterviewContext.CandidateJobScanRuns))
            .Should().NotBeNull();
        typeof(ITHunterviewContext).GetProperty(nameof(ITHunterviewContext.CandidateJobScanResults))
            .Should().NotBeNull();
        typeof(ITHunterviewContext).GetProperty(nameof(ITHunterviewContext.RecruiterCvScanRuns))
            .Should().NotBeNull();
        typeof(ITHunterviewContext).GetProperty(nameof(ITHunterviewContext.RecruiterCvScanResults))
            .Should().NotBeNull();

        AssertColumns(context, typeof(CandidateJobScanRun),
            ("Id", "id", false),
            ("CandidateUserId", "candidate_user_id", false),
            ("CvId", "cv_id", false),
            ("CvFileNameSnapshot", "cv_file_name_snapshot", false),
            ("Status", "status", false),
            ("CreatedAt", "created_at", false),
            ("StartedAt", "started_at", true),
            ("CompletedAt", "completed_at", true),
            ("ErrorCode", "error_code", true),
            ("ErrorMessage", "error_message", true));
        AssertColumns(context, typeof(CandidateJobScanResult),
            ("Id", "id", false),
            ("RunId", "run_id", false),
            ("JobId", "job_id", false),
            ("JobTitleSnapshot", "job_title_snapshot", false),
            ("MatchScore", "match_score", true),
            ("MatchDetails", "match_details", false),
            ("CvAnalysisQuality", "cv_analysis_quality", true),
            ("CvAnalysisCoverageJson", "cv_analysis_coverage_json", true),
            ("CvAnalysisDiagnosticsJson", "cv_analysis_diagnostics_json", true),
            ("Rank", "rank", false));
        AssertColumns(context, typeof(RecruiterCvScanRun),
            ("Id", "id", false),
            ("RecruiterUserId", "recruiter_user_id", false),
            ("RecruiterProfileId", "recruiter_profile_id", false),
            ("CompanyId", "company_id", false),
            ("JobId", "job_id", false),
            ("JobTitleSnapshot", "job_title_snapshot", false),
            ("Status", "status", false),
            ("CreatedAt", "created_at", false),
            ("StartedAt", "started_at", true),
            ("CompletedAt", "completed_at", true),
            ("ErrorCode", "error_code", true),
            ("ErrorMessage", "error_message", true));
        AssertColumns(context, typeof(RecruiterCvScanResult),
            ("Id", "id", false),
            ("RunId", "run_id", false),
            ("CvId", "cv_id", false),
            ("CandidateUserId", "candidate_user_id", false),
            ("MatchScore", "match_score", true),
            ("MatchDetails", "match_details", false),
            ("CvAnalysisQuality", "cv_analysis_quality", true),
            ("CvAnalysisCoverageJson", "cv_analysis_coverage_json", true),
            ("CvAnalysisDiagnosticsJson", "cv_analysis_diagnostics_json", true),
            ("Rank", "rank", false));
    }

    [Fact]
    [Trait("Requirement", "R-05")]
    public void Model_ResultPairIsUniqueOnlyWithinItsRun()
    {
        using var context = CreateContext();

        AssertUniqueIndex(
            context,
            typeof(CandidateJobScanResult),
            nameof(CandidateJobScanResult.RunId),
            nameof(CandidateJobScanResult.JobId));
        AssertUniqueIndex(
            context,
            typeof(RecruiterCvScanResult),
            nameof(RecruiterCvScanResult.RunId),
            nameof(RecruiterCvScanResult.CvId));

        AssertNoUniqueIndex(context, typeof(CandidateJobScanResult), nameof(CandidateJobScanResult.JobId));
        AssertNoUniqueIndex(context, typeof(RecruiterCvScanResult), nameof(RecruiterCvScanResult.CvId));

        AssertIndex(
            context,
            typeof(CandidateJobScanRun),
            [false, false, false, true],
            nameof(CandidateJobScanRun.CandidateUserId),
            nameof(CandidateJobScanRun.CvId),
            nameof(CandidateJobScanRun.Status),
            nameof(CandidateJobScanRun.CreatedAt));
        AssertIndex(
            context,
            typeof(RecruiterCvScanRun),
            [false, false, false, false, true],
            nameof(RecruiterCvScanRun.RecruiterUserId),
            nameof(RecruiterCvScanRun.CompanyId),
            nameof(RecruiterCvScanRun.JobId),
            nameof(RecruiterCvScanRun.Status),
            nameof(RecruiterCvScanRun.CreatedAt));
    }

    [Fact]
    [Trait("Requirement", "R-04")]
    public void Model_RunStatusUsesBoundedStringConversion()
    {
        using var context = CreateContext();

        AssertEnumProperty(
            context,
            typeof(CandidateJobScanRun),
            nameof(CandidateJobScanRun.Status),
            16,
            (MatchingScanRunStatus.Pending, "PENDING"),
            (MatchingScanRunStatus.Processing, "PROCESSING"),
            (MatchingScanRunStatus.Completed, "COMPLETED"),
            (MatchingScanRunStatus.Failed, "FAILED"));
        AssertEnumProperty(
            context,
            typeof(RecruiterCvScanRun),
            nameof(RecruiterCvScanRun.Status),
            16,
            (MatchingScanRunStatus.Pending, "PENDING"),
            (MatchingScanRunStatus.Processing, "PROCESSING"),
            (MatchingScanRunStatus.Completed, "COMPLETED"),
            (MatchingScanRunStatus.Failed, "FAILED"));

        AssertMaxLength(context, typeof(CandidateJobScanRun), nameof(CandidateJobScanRun.ErrorCode), 128);
        AssertMaxLength(context, typeof(CandidateJobScanRun), nameof(CandidateJobScanRun.ErrorMessage), 1000);
        AssertMaxLength(context, typeof(RecruiterCvScanRun), nameof(RecruiterCvScanRun.ErrorCode), 128);
        AssertMaxLength(context, typeof(RecruiterCvScanRun), nameof(RecruiterCvScanRun.ErrorMessage), 1000);

        AssertCheckConstraint(
            context,
            typeof(CandidateJobScanRun),
            "ck_candidate_job_scan_runs_status",
            "\"status\" IN ('PENDING', 'PROCESSING', 'COMPLETED', 'FAILED')");
        AssertCheckConstraint(
            context,
            typeof(RecruiterCvScanRun),
            "ck_recruiter_cv_scan_runs_status",
            "\"status\" IN ('PENDING', 'PROCESSING', 'COMPLETED', 'FAILED')");
    }

    [Fact]
    [Trait("Requirement", "R-10")]
    public void Model_UnlockIsUniqueByRecruiterUserAndCv()
    {
        using var context = CreateContext();

        AssertUniqueIndex(
            context,
            typeof(RecruiterUnlockedCvs),
            nameof(RecruiterUnlockedCvs.RecruiterId),
            nameof(RecruiterUnlockedCvs.CvId));
        AssertNoUniqueIndex(context, typeof(RecruiterUnlockedCvs), nameof(RecruiterUnlockedCvs.JobId));

        var entity = GetModel(context).FindEntityType(typeof(RecruiterUnlockedCvs))!;
        var status = entity.FindProperty(nameof(RecruiterUnlockedCvs.Status))!;
        AssertEnumProperty(
            context,
            typeof(RecruiterUnlockedCvs),
            nameof(RecruiterUnlockedCvs.Status),
            16,
            (RecruiterCvUnlockStatus.Pending, "PENDING"),
            (RecruiterCvUnlockStatus.Completed, "COMPLETED"),
            (RecruiterCvUnlockStatus.Failed, "FAILED"));
        status.GetDefaultValue().Should().Be(RecruiterCvUnlockStatus.Completed);
        AssertMaxLength(context, typeof(RecruiterUnlockedCvs), nameof(RecruiterUnlockedCvs.FailureCode), 128);

        AssertCheckConstraint(
            context,
            typeof(RecruiterUnlockedCvs),
            "ck_recruiter_unlocked_cvs_status",
            "\"status\" IN ('PENDING', 'COMPLETED', 'FAILED')");

        AssertForeignKey(context, typeof(RecruiterUnlockedCvs), "RecruiterId", typeof(User), false, DeleteBehavior.Restrict);
        AssertForeignKey(context, typeof(RecruiterUnlockedCvs), "CvId", typeof(Cvs), false, DeleteBehavior.Restrict);
        AssertNoForeignKey(context, typeof(RecruiterUnlockedCvs), nameof(RecruiterUnlockedCvs.JobId));
        AssertForeignKey(context, typeof(RecruiterUnlockedCvs), "SourceScanResultId", typeof(RecruiterCvScanResult), true, DeleteBehavior.Restrict);

        AssertColumns(context, typeof(RecruiterUnlockedCvs),
            ("JobId", "job_id", true),
            ("Status", "status", false),
            ("SourceScanResultId", "source_scan_result_id", true),
            ("SnapshotStorageKey", "snapshot_storage_key", true),
            ("SnapshotFileName", "snapshot_file_name", true),
            ("SnapshotContentHash", "snapshot_content_hash", true),
            ("SnapshotCreatedAt", "snapshot_created_at", true),
            ("FailureCode", "failure_code", true));

        foreach (var propertyName in new[]
                 {
                     nameof(RecruiterUnlockedCvs.SourceScanResultId),
                     nameof(RecruiterUnlockedCvs.SnapshotStorageKey),
                     nameof(RecruiterUnlockedCvs.SnapshotFileName),
                     nameof(RecruiterUnlockedCvs.SnapshotContentHash),
                     nameof(RecruiterUnlockedCvs.SnapshotCreatedAt),
                     nameof(RecruiterUnlockedCvs.FailureCode)
                 })
        {
            entity.FindProperty(propertyName)!.IsNullable.Should().BeTrue(
                because: "legacy completed unlocks have no fabricated retained snapshot");
        }
    }

    [Fact]
    [Trait("Requirement", "R-08")]
    public void Model_ScanForeignKeysNeverCascadeDeleteCvJobUserOrCompany()
    {
        using var context = CreateContext();

        AssertForeignKey(context, typeof(CandidateJobScanRun), "CandidateUserId", typeof(User), false, DeleteBehavior.Restrict);
        AssertForeignKey(context, typeof(CandidateJobScanRun), "CvId", typeof(Cvs), false, DeleteBehavior.Restrict);
        AssertForeignKey(context, typeof(CandidateJobScanResult), "RunId", typeof(CandidateJobScanRun), false, DeleteBehavior.Cascade);
        AssertForeignKey(context, typeof(CandidateJobScanResult), "JobId", typeof(JobPostings), false, DeleteBehavior.Restrict);

        AssertForeignKey(context, typeof(RecruiterCvScanRun), "RecruiterUserId", typeof(User), false, DeleteBehavior.Restrict);
        AssertForeignKey(context, typeof(RecruiterCvScanRun), "RecruiterProfileId", typeof(RecruiterProfiles), false, DeleteBehavior.Restrict);
        AssertForeignKey(context, typeof(RecruiterCvScanRun), "CompanyId", typeof(Companies), false, DeleteBehavior.Restrict);
        AssertForeignKey(context, typeof(RecruiterCvScanRun), "JobId", typeof(JobPostings), false, DeleteBehavior.Restrict);
        AssertForeignKey(context, typeof(RecruiterCvScanResult), "RunId", typeof(RecruiterCvScanRun), false, DeleteBehavior.Cascade);
        AssertForeignKey(context, typeof(RecruiterCvScanResult), "CvId", typeof(Cvs), false, DeleteBehavior.Restrict);
        AssertForeignKey(context, typeof(RecruiterCvScanResult), "CandidateUserId", typeof(User), false, DeleteBehavior.Restrict);
    }

    [Fact]
    [Trait("Requirement", "R-12")]
    public void Model_CvJobMatchProductScopeIsNullableForLegacyAndBounded()
    {
        using var context = CreateContext();

        var entity = GetModel(context).FindEntityType(typeof(CvJobMatchScores))!;
        var property = entity.FindProperty(nameof(CvJobMatchScores.ProductScope))!;
        var storeObject = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());

        property.IsNullable.Should().BeTrue();
        property.GetColumnName(storeObject).Should().Be("product_scope");
        property.GetMaxLength().Should().Be(32);
        AssertEnumProperty(
            context,
            typeof(CvJobMatchScores),
            nameof(CvJobMatchScores.ProductScope),
            32,
            (CvJobMatchProductScope.CandidateOneToOne, "CANDIDATE_ONE_TO_ONE"));

        AssertCheckConstraint(
            context,
            typeof(CvJobMatchScores),
            "ck_cv_job_match_scores_product_scope",
            "\"product_scope\" IS NULL OR \"product_scope\" IN ('CANDIDATE_ONE_TO_ONE')");

        AssertIndex(
            context,
            typeof(CvJobMatchScores),
            [false, false, true],
            nameof(CvJobMatchScores.ProductScope),
            nameof(CvJobMatchScores.UserId),
            nameof(CvJobMatchScores.UpdatedAt));
    }

    private static ITHunterviewContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ITHunterviewContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new MatchingScanTestContext(options);
    }

    private sealed class MatchingScanTestContext : ITHunterviewContext
    {
        public MatchingScanTestContext(DbContextOptions<ITHunterviewContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
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

    private static void AssertUniqueIndex(
        ITHunterviewContext context,
        Type entityType,
        params string[] propertyNames)
    {
        var index = FindIndex(context, entityType, propertyNames);
        index.Should().NotBeNull();
        index!.IsUnique.Should().BeTrue();
    }

    private static void AssertNoUniqueIndex(
        ITHunterviewContext context,
        Type entityType,
        params string[] propertyNames)
    {
        var indexes = GetModel(context).FindEntityType(entityType)!.GetIndexes();
        indexes.Should().NotContain(index =>
            index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual(propertyNames));
    }

    private static void AssertIndex(
        ITHunterviewContext context,
        Type entityType,
        IReadOnlyList<bool> descending,
        params string[] propertyNames)
    {
        var index = FindIndex(context, entityType, propertyNames);
        index.Should().NotBeNull();
        index!.IsUnique.Should().BeFalse();
        index!.IsDescending.Should().Equal(descending);
    }

    private static IIndex? FindIndex(
        ITHunterviewContext context,
        Type entityType,
        params string[] propertyNames)
    {
        return GetModel(context).FindEntityType(entityType)!.GetIndexes().SingleOrDefault(index =>
            index.Properties.Select(property => property.Name).SequenceEqual(propertyNames));
    }

    private static void AssertEnumProperty<TEnum>(
        ITHunterviewContext context,
        Type entityType,
        string propertyName,
        int expectedMaxLength,
        params (TEnum Value, string ProviderValue)[] expectedValues)
        where TEnum : struct, Enum
    {
        var property = GetModel(context).FindEntityType(entityType)!.FindProperty(propertyName)!;
        var converter = property.GetValueConverter()!;
        foreach (var (value, providerValue) in expectedValues)
        {
            converter.ConvertToProvider(value).Should().Be(providerValue);
            converter.ConvertFromProvider(providerValue).Should().Be(value);
        }

        property.GetMaxLength().Should().Be(expectedMaxLength);
    }

    private static void AssertMaxLength(
        ITHunterviewContext context,
        Type entityType,
        string propertyName,
        int expectedMaxLength)
    {
        GetModel(context).FindEntityType(entityType)!.FindProperty(propertyName)!
            .GetMaxLength().Should().Be(expectedMaxLength);
    }

    private static void AssertColumns(
        ITHunterviewContext context,
        Type entityType,
        params (string PropertyName, string ColumnName, bool IsNullable)[] expectedColumns)
    {
        var entity = GetModel(context).FindEntityType(entityType)!;

        foreach (var (propertyName, columnName, isNullable) in expectedColumns)
        {
            var property = entity.FindProperty(propertyName);
            property.Should().NotBeNull();
            property!.GetColumnName().Should().Be(columnName);
            property.IsNullable.Should().Be(isNullable);
        }
    }

    private static void AssertCheckConstraint(
        ITHunterviewContext context,
        Type entityType,
        string constraintName,
        string expectedSql)
    {
        var constraint = GetModel(context).FindEntityType(entityType)!
            .GetCheckConstraints()
            .SingleOrDefault(value => value.Name == constraintName);

        constraint.Should().NotBeNull();
        constraint!.Sql.Should().Be(expectedSql);
    }

    private static void AssertForeignKey(
        ITHunterviewContext context,
        Type dependentType,
        string dependentProperty,
        Type principalType,
        bool dependentNullable,
        DeleteBehavior deleteBehavior)
    {
        var foreignKey = GetModel(context).FindEntityType(dependentType)!.GetForeignKeys()
            .SingleOrDefault(value => value.Properties.Select(property => property.Name).SequenceEqual([dependentProperty]));

        foreignKey.Should().NotBeNull();
        foreignKey!.PrincipalEntityType.ClrType.Should().Be(principalType);
        foreignKey.PrincipalKey.Properties.Select(property => property.Name).Should().Equal("Id");
        foreignKey.Properties.Single().IsNullable.Should().Be(dependentNullable);
        foreignKey.DeleteBehavior.Should().Be(deleteBehavior);
    }

    private static void AssertNoForeignKey(
        ITHunterviewContext context,
        Type dependentType,
        string dependentProperty)
    {
        GetModel(context).FindEntityType(dependentType)!.GetForeignKeys()
            .Should().NotContain(value =>
                value.Properties.Count == 1 && value.Properties[0].Name == dependentProperty);
    }

    private static IModel GetModel(ITHunterviewContext context) =>
        context.GetService<IDesignTimeModel>().Model;
}
