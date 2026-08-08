using System.Reflection;
using FluentAssertions;
using ITHunterview.Service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace ITHunterview.Service.Tests.PromptAdmin;

public sealed class JdAnalysisQualityMigrationTests
{
    [Fact]
    public void Up_AddsNullableQualityMetadataAndSafeReadyBackfill()
    {
        var operations = ReadOperations("Up");

        operations.OfType<AddColumnOperation>().Should().Contain(operation =>
            operation.Table == "job_analysis_runs" && operation.Name == "analysis_quality" && operation.IsNullable);
        operations.OfType<AddColumnOperation>().Should().Contain(operation =>
            operation.Table == "job_analysis_runs" && operation.Name == "analysis_coverage_json" && operation.IsNullable);
        operations.OfType<AddColumnOperation>().Should().Contain(operation =>
            operation.Table == "job_analysis_runs" && operation.Name == "analysis_diagnostics_json" && operation.IsNullable);
        operations.OfType<AddColumnOperation>().Should().Contain(operation =>
            operation.Table == "cv_job_match_scores" && operation.Name == "jd_analysis_quality" && operation.IsNullable);
        operations.OfType<AddColumnOperation>().Should().Contain(operation =>
            operation.Table == "cv_job_match_scores" && operation.Name == "jd_analysis_coverage_json" && operation.IsNullable);
        operations.OfType<AddColumnOperation>().Should().Contain(operation =>
            operation.Table == "cv_job_match_scores" && operation.Name == "jd_analysis_diagnostics_json" && operation.IsNullable);

        var backfill = operations.OfType<SqlOperation>()
            .Select(operation => operation.Sql)
            .Single(sql => sql.Contains("UPDATE job_analysis_runs", StringComparison.Ordinal));
        backfill.Should().Contain("status = 'READY'");
        backfill.Should().Contain("effective_analysis_json IS NOT NULL");
        backfill.Should().Contain("analysis_quality IS NULL");
        backfill.Should().NotContain("cv_job_match_scores");

        operations.OfType<AddCheckConstraintOperation>().Select(operation => operation.Name)
            .Should().Contain(new[]
            {
                "ck_job_analysis_runs_analysis_quality",
                "ck_cv_job_match_scores_jd_analysis_quality"
            });
    }

    [Fact]
    public void Down_RemovesOnlyTheQualityMetadataColumnsAndConstraints()
    {
        var operations = ReadOperations("Down");

        operations.OfType<DropCheckConstraintOperation>().Select(operation => operation.Name)
            .Should().BeEquivalentTo(new[]
            {
                "ck_job_analysis_runs_analysis_quality",
                "ck_cv_job_match_scores_jd_analysis_quality"
            });

        operations.OfType<DropColumnOperation>().Select(operation => (operation.Table, operation.Name))
            .Should().BeEquivalentTo(new[]
            {
                ("job_analysis_runs", "analysis_quality"),
                ("job_analysis_runs", "analysis_coverage_json"),
                ("job_analysis_runs", "analysis_diagnostics_json"),
                ("cv_job_match_scores", "jd_analysis_quality"),
                ("cv_job_match_scores", "jd_analysis_coverage_json"),
                ("cv_job_match_scores", "jd_analysis_diagnostics_json")
            });
    }

    private static IReadOnlyList<MigrationOperation> ReadOperations(string methodName)
    {
        var migrationType = typeof(ITHunterviewContext).Assembly
            .GetTypes()
            .Single(type => type.GetCustomAttribute<MigrationAttribute>()?.Id
                .EndsWith("_AddJdAnalysisQualityStates", StringComparison.Ordinal) == true);
        var migration = Activator.CreateInstance(migrationType);
        migration.Should().NotBeNull();

        var method = migrationType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();

        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        method!.Invoke(migration, [builder]);
        return builder.Operations;
    }
}
