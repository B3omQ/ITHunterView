using System.Reflection;
using FluentAssertions;
using ITHunterview.Service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace ITHunterview.Service.Tests.PromptAdmin;

public sealed class JdAnalysisV4PromptSeedMigrationTests
{
    private const string SystemVersionId = "440a81ce-07f7-4cbe-b2d9-da4141ff4c94";
    private const string UserVersionId = "a207323f-1576-4595-a05b-a1ac28e9a1c7";

    [Fact]
    public void Up_SeedsInactiveCompactV4JdPromptPair()
    {
        var migrationType = FindMigrationType();
        var sql = ReadMigrationSql(migrationType, "Up");

        sql.Should().Contain(SystemVersionId);
        sql.Should().Contain(UserVersionId);
        sql.Should().Contain("'v5.0.0'");
        sql.Should().Contain("{\"contract\":\"jd-analysis/v4\",\"role\":\"system\"}");
        sql.Should().Contain("{\"contract\":\"jd-analysis/v4\",\"role\":\"user\"}");
        sql.Should().Contain("LOCK TABLE \"PromptVersions\" IN SHARE ROW EXCLUSIVE MODE");
        sql.Should().Contain("requirement_verbatim");
        sql.Should().Contain("Do not output detail_verbatim, evidence, evidences, confidence, group_id, requirements_list, or skills_normalized.");
        sql.Should().Contain("[JOB_INPUT_JSON]");
        sql.Should().NotContain("SET \"IsActive\" = TRUE");
        sql.Should().Contain("JD_ANALYSIS_V4_PROMPT_SEED_POSTCONDITION_FAILED");
    }

    [Fact]
    public void Down_DeletesOnlyTheInactiveCompactV4Pair()
    {
        var sql = ReadMigrationSql(FindMigrationType(), "Down");

        sql.Should().Contain(SystemVersionId);
        sql.Should().Contain(UserVersionId);
        sql.Should().Contain("DELETE FROM \"PromptVersions\"");
        sql.Should().NotContain("UPDATE \"PromptVersions\"");
    }

    private static Type FindMigrationType() => typeof(ITHunterviewContext).Assembly
        .GetTypes()
        .Single(type => type.GetCustomAttribute<MigrationAttribute>()?.Id
            .EndsWith("_SeedJdAnalysisV4CompactPromptPair", StringComparison.Ordinal) == true);

    private static string ReadMigrationSql(Type migrationType, string methodName)
    {
        var migration = Activator.CreateInstance(migrationType);
        var method = migrationType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();

        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        method!.Invoke(migration, [builder]);
        return string.Join("\n", builder.Operations.OfType<SqlOperation>().Select(operation => operation.Sql));
    }
}
