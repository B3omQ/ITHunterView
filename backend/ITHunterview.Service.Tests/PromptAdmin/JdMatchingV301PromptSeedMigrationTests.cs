using System.Reflection;
using FluentAssertions;
using ITHunterview.Service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace ITHunterview.Service.Tests.PromptAdmin;

public sealed class JdMatchingV301PromptSeedMigrationTests
{
    private const string MigrationSuffix = "_SeedJdMatchingV301HandlerConsistencyPrompt";
    private const string V301Id = "37aa6caa-66a5-4285-8ee1-634dc1b45923";
    private const string V300Id = "52a5fb08-25e2-4ccd-899e-b76e4292172f";

    [Fact]
    public void Up_SeedsExactSchemaFreeFixtureAndActivatesOnlyV301()
    {
        var sql = ReadSql("Up");
        var embedded = ExtractDollarQuoted(sql, "jd_matching_v301");

        embedded.Should().Be(ReadFixture());
        sql.Should().Contain("LOCK TABLE \"PromptVersions\" IN SHARE ROW EXCLUSIVE MODE");
        sql.Should().Contain("WHERE \"PromptKey\" = 'JD_MATCHING_PROMPT'");
        sql.Should().Contain(V301Id);
        sql.Should().Contain(V300Id);
        sql.Should().Contain("'v3.0.1'");
        sql.Should().Contain("ON CONFLICT (\"Id\") DO NOTHING");
        sql.Should().Contain("\"ModelConfig\" IS NULL");
        sql.Should().Contain("JD_MATCHING_V301_FIXED_ROW_MISMATCH");
        sql.Should().Contain("JD_MATCHING_V301_POSTCONDITION_FAILED");
        sql.Should().Contain("JD_MATCHING_V301_PARSER_PAIR_CHANGED");
        embedded.Should().NotContain("H_EXP_00");
        embedded.Should().NotContain("H_EDU_00");
        embedded.Should().NotContain("H_LANG_00");
        embedded.Should().NotContain("schemaVersion");
        embedded.Should().NotContain("--- BEGIN LOCKED JD MATCHING OUTPUT SCHEMA ---");
    }

    [Fact]
    public void Down_RestoresExactV300WithoutTouchingParserPairs()
    {
        var sql = ReadSql("Down");

        sql.Should().Contain(V301Id);
        sql.Should().Contain(V300Id);
        sql.Should().Contain("JD_MATCHING_V301_DOWN_V300_FALLBACK_MISMATCH");
        sql.Should().Contain("JD_MATCHING_V301_DOWN_POSTCONDITION_FAILED");
        sql.Should().Contain("JD_MATCHING_V301_DOWN_PARSER_PAIR_CHANGED");
        sql.Should().NotContain("DELETE FROM \"PromptVersions\"");
    }

    [Fact]
    public void Migration_ChangesPromptDataOnlyAndKeepsAnalysisRowsUntouched()
    {
        var sql = ReadSql("Up") + "\n" + ReadSql("Down");

        foreach (var forbidden in new[]
                 {
                     "JobPostings", "ParsedData", "JobAnalysisRuns", "JobAnalysisRevisions",
                     "CvJobMatchScores", "MatchDetails", "CREATE TABLE", "ALTER TABLE", "CREATE INDEX"
                 })
        {
            sql.Should().NotContain(forbidden);
        }

        sql.Should().NotContain("UPDATE \"PromptVersions\" SET \"Content\"");
        sql.Should().NotContain("UPDATE \"PromptVersions\" SET \"ModelConfig\"");
    }

    private static string ReadSql(string methodName)
    {
        var migrationType = typeof(ITHunterviewContext).Assembly
            .GetTypes()
            .SingleOrDefault(type => type.GetCustomAttribute<MigrationAttribute>()?.Id
                .EndsWith(MigrationSuffix, StringComparison.Ordinal) == true);
        migrationType.Should().NotBeNull();

        var migration = Activator.CreateInstance(migrationType!);
        var method = migrationType!.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();

        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        method!.Invoke(migration, [builder]);
        return string.Join("\n", builder.Operations.OfType<SqlOperation>().Select(operation => operation.Sql));
    }

    private static string ExtractDollarQuoted(string sql, string tag)
    {
        var delimiter = $"${tag}$";
        var start = sql.IndexOf(delimiter, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0);
        start += delimiter.Length;
        var end = sql.IndexOf(delimiter, start, StringComparison.Ordinal);
        end.Should().BeGreaterThan(start);
        return sql[start..end];
    }

    private static string ReadFixture() => File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Matching", "Fixtures", "jd-matching-v3.0.1-semantic.txt"));
}
