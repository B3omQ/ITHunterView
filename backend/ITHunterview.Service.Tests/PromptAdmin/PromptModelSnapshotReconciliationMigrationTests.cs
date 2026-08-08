using System.Reflection;
using FluentAssertions;
using ITHunterview.Service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace ITHunterview.Service.Tests.PromptAdmin;

public sealed class PromptModelSnapshotReconciliationMigrationTests
{
    [Fact]
    public void ReconciliationMigration_OnlyAssertsTheAlreadyAppliedQualitySchema()
    {
        var migrationType = typeof(ITHunterviewContext).Assembly
            .GetTypes()
            .SingleOrDefault(type => type.GetCustomAttribute<MigrationAttribute>()?.Id
                .EndsWith("_ReconcileJdAnalysisQualityModelSnapshot", StringComparison.Ordinal) == true);

        migrationType.Should().NotBeNull(
            "the EF snapshot must be reconciled before a data-only prompt migration is generated");

        var migration = Activator.CreateInstance(migrationType!);
        var up = InvokeMigrationMethod(migrationType!, migration!, "Up");
        var down = InvokeMigrationMethod(migrationType!, migration!, "Down");

        up.Operations.Should().ContainSingle(operation => operation is SqlOperation);
        up.Operations.Should().NotContain(operation => operation is AddColumnOperation);
        up.Operations.Should().NotContain(operation => operation is AlterColumnOperation);
        up.Operations.Should().NotContain(operation => operation is AddCheckConstraintOperation);
        down.Operations.Should().BeEmpty();

        var sql = up.Operations.OfType<SqlOperation>().Single().Sql;
        foreach (var catalogName in new[]
                 {
                     "analysis_coverage_json",
                     "analysis_diagnostics_json",
                     "analysis_quality",
                     "jd_analysis_coverage_json",
                     "jd_analysis_diagnostics_json",
                     "jd_analysis_quality",
                     "ck_job_analysis_runs_analysis_quality",
                     "ck_cv_job_match_scores_jd_analysis_quality",
                     "JD_ANALYSIS_QUALITY_SCHEMA_RECONCILIATION_FAILED"
                 })
        {
            sql.Should().Contain(catalogName);
        }
    }

    private static MigrationBuilder InvokeMigrationMethod(Type migrationType, object migration, string methodName)
    {
        var method = migrationType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();

        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        method!.Invoke(migration, [builder]);
        return builder;
    }
}
