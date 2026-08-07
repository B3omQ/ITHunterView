using System.Reflection;
using FluentAssertions;
using ITHunterview.Service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace ITHunterview.Service.Tests.PromptAdmin;

public sealed class CvAnalysisQualityMigrationTests
{
    [Fact]
    public void Up_AddsOnlyNullableCvQualityMetadataAndConstraints()
    {
        var operations = ReadOperations("Up");

        operations.OfType<AddColumnOperation>()
            .Select(operation => (operation.Table, operation.Name, operation.IsNullable))
            .Should().BeEquivalentTo(new[]
            {
                ("cvs", "analysis_quality", true),
                ("cvs", "analysis_coverage_json", true),
                ("cvs", "analysis_diagnostics_json", true),
                ("cv_job_match_scores", "cv_analysis_quality", true),
                ("cv_job_match_scores", "cv_analysis_coverage_json", true),
                ("cv_job_match_scores", "cv_analysis_diagnostics_json", true)
            });

        operations.OfType<AddCheckConstraintOperation>().Select(operation => operation.Name)
            .Should().BeEquivalentTo(new[]
            {
                "ck_cvs_analysis_quality",
                "ck_cv_job_match_scores_cv_analysis_quality"
            });
        operations.Should().NotContain(operation => operation is SqlOperation);
    }

    [Fact]
    public void Down_RemovesOnlyCvQualityMetadataAndConstraints()
    {
        var operations = ReadOperations("Down");

        operations.OfType<DropCheckConstraintOperation>().Select(operation => operation.Name)
            .Should().BeEquivalentTo(new[]
            {
                "ck_cvs_analysis_quality",
                "ck_cv_job_match_scores_cv_analysis_quality"
            });
        operations.OfType<DropColumnOperation>()
            .Select(operation => (operation.Table, operation.Name))
            .Should().BeEquivalentTo(new[]
            {
                ("cvs", "analysis_quality"),
                ("cvs", "analysis_coverage_json"),
                ("cvs", "analysis_diagnostics_json"),
                ("cv_job_match_scores", "cv_analysis_quality"),
                ("cv_job_match_scores", "cv_analysis_coverage_json"),
                ("cv_job_match_scores", "cv_analysis_diagnostics_json")
            });
    }

    private static IReadOnlyList<MigrationOperation> ReadOperations(string methodName)
    {
        var migrationType = typeof(ITHunterviewContext).Assembly
            .GetTypes()
            .Single(type => type.GetCustomAttribute<MigrationAttribute>()?.Id
                .EndsWith("_AddCvAnalysisQualityStates", StringComparison.Ordinal) == true);
        var migration = Activator.CreateInstance(migrationType);
        migration.Should().NotBeNull();

        var method = migrationType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();

        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        method!.Invoke(migration, [builder]);
        return builder.Operations;
    }
}
