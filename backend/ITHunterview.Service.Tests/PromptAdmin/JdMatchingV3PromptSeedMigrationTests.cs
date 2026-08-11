using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using ITHunterview.Service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace ITHunterview.Service.Tests.PromptAdmin;

public sealed class JdMatchingV3PromptSeedMigrationTests
{
    private const string MigrationSuffix = "_SeedJdMatchingV3SemanticPrompt";
    private const string V3Id = "52a5fb08-25e2-4ccd-899e-b76e4292172f";
    private const string V2Id = "4969f6f7-5696-4700-8817-1fee806ecf9e";

    [Fact]
    public void Up_SeedsTheReviewedSemanticFixtureAndActivatesOnlyMatchingV3()
    {
        var sql = ReadSql("Up");
        var embedded = ExtractDollarQuoted(sql, "jd_matching_v3");
        var fixture = ReadFixture();

        embedded.Should().Be(fixture);
        Hash(embedded).Should().Be("335956b4c7c875fd796be4f4c1b2f3e6ec934648bec7a84abee6a8ddfa8cc00c");
        sql.Should().Contain("LOCK TABLE \"PromptVersions\" IN SHARE ROW EXCLUSIVE MODE");
        sql.Should().Contain("SELECT \"Id\" INTO STRICT matching_prompt_id");
        sql.Should().Contain("WHERE \"PromptKey\" = 'JD_MATCHING_PROMPT'");
        sql.Should().Contain(V3Id);
        sql.Should().Contain(V2Id);
        sql.Should().Contain("'v3.0.0'");
        sql.Should().Contain("ON CONFLICT (\"Id\") DO NOTHING");
        sql.Should().Contain("\"ModelConfig\" IS NULL");
        sql.Should().Contain("JD_MATCHING_V3_DUPLICATE_TAG");
        sql.Should().Contain("JD_MATCHING_V3_EXPECTED_V2_MISMATCH");
        sql.Should().Contain("JD_MATCHING_V3_REPLAY_MISMATCH");
        sql.Should().Contain("JD_MATCHING_V3_UNEXPECTED_ACTIVE_VERSION");
        sql.Should().Contain("JD_MATCHING_V3_FIXED_ROW_MISMATCH");
        sql.Should().Contain("JD_MATCHING_V3_POSTCONDITION_FAILED");
        sql.Should().Contain("JD_MATCHING_V3_PARSER_PAIR_CHANGED");
        sql.Should().Contain("CV_ANALYSIS_SYSTEM");
        sql.Should().Contain("CV_ANALYSIS_USER");
        sql.Should().Contain("JD_ANALYSIS_V2_SYSTEM");
        sql.Should().Contain("JD_ANALYSIS_V2_USER");
    }

    [Fact]
    public void Down_RestoresOnlyTheExactV2FallbackAndLeavesV3History()
    {
        var sql = ReadSql("Down");

        ExtractDollarQuoted(sql, "jd_matching_v3").Should().Be(ReadFixture());
        sql.Should().Contain("JD_MATCHING_V3_DOWN_FIXED_ROW_MISMATCH");
        sql.Should().Contain("JD_MATCHING_V3_DOWN_V2_FALLBACK_MISMATCH");
        sql.Should().Contain("JD_MATCHING_V3_DOWN_REPLAY_MISMATCH");
        sql.Should().Contain("JD_MATCHING_V3_DOWN_NEWER_ACTIVE_VERSION");
        sql.Should().Contain("JD_MATCHING_V3_DOWN_POSTCONDITION_FAILED");
        sql.Should().Contain("JD_MATCHING_V3_DOWN_PARSER_PAIR_CHANGED");
        sql.Should().Contain(V2Id);
        sql.Should().Contain(V3Id);
        sql.Should().NotContain("DELETE FROM \"PromptVersions\"");
    }

    [Fact]
    public void Migration_DoesNotTouchParserContentOrSavedAnalysisAndMatchingData()
    {
        var sql = ReadSql("Up") + "\n" + ReadSql("Down");

        foreach (var forbidden in new[]
                 {
                     "JobPostings", "ParsedData", "JobAnalysisRuns", "JobAnalysisRevisions",
                     "CvJobMatchScores", "MatchDetails"
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
        Path.Combine(AppContext.BaseDirectory, "Matching", "Fixtures", "jd-matching-v3-semantic.txt"));

    private static string Hash(string content) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
}
