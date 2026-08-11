using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using ITHunterview.Service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace ITHunterview.Service.Tests.PromptAdmin;

public sealed class JdAnalysisV601PromptSeedMigrationTests
{
    private const string SystemVersionId = "7837b4ec-1094-45b4-aebd-2f732958b74b";
    private const string UserVersionId = "7d3f097a-17b5-4b91-aa2a-02cd453507f3";
    private const string PreviousSystemVersionId = "3a7a42f2-f4f0-4a2a-8d65-8d4c8b16d501";
    private const string PreviousUserVersionId = "3a7a42f2-f4f0-4a2a-8d65-8d4c8b16d502";
    private const string SystemContentHash = "f7f675d4cb63448a553300abd617cc3b93a939e8b74849540a41265b680ce651";
    private const string UserContentHash = "e003aad9808ca95daf179d35ac4ecafb9b0ab52064df1dac664526518431ca1b";

    [Fact]
    public void Up_SeedsTheReviewedV601SemanticPairByteForByte()
    {
        var upSql = ReadSql(FindMigrationType(), "Up");
        var systemContent = ExtractDollarQuoted(upSql, "jd_v601_system");
        var userContent = ExtractDollarQuoted(upSql, "jd_v601_user");

        systemContent.Should().Be(ReadFixture("jd-analysis-v6.0.1-system-semantic.txt"));
        userContent.Should().Be(ReadFixture("jd-analysis-v6.0.1-user-semantic.txt"));
        Hash(systemContent).Should().Be(SystemContentHash);
        Hash(userContent).Should().Be(UserContentHash);
        upSql.Should().Contain(SystemVersionId);
        upSql.Should().Contain(UserVersionId);
        upSql.Should().Contain("'v6.0.1'");
        upSql.Should().Contain("{\"contract\":\"jd-analysis-prompt/v6\",\"role\":\"system\"}");
        upSql.Should().Contain("{\"contract\":\"jd-analysis-prompt/v6\",\"role\":\"user\"}");
        systemContent.Should().NotContain("--- BEGIN LOCKED JD ANALYSIS OUTPUT SCHEMA ---");
        systemContent.Should().NotContain("\"schema_version\"");
        userContent.Should().NotContain("\"schema_version\"");
    }

    [Fact]
    public void Up_RequiresExactPriorPairAndPreservesUnrelatedActivePrompts()
    {
        var upSql = ReadSql(FindMigrationType(), "Up");

        upSql.Should().Contain("ORDER BY \"PromptId\", \"Id\"");
        upSql.Should().Contain("FOR UPDATE");
        upSql.Should().Contain(PreviousSystemVersionId);
        upSql.Should().Contain(PreviousUserVersionId);
        upSql.Should().Contain("JD_ANALYSIS_V601_UNEXPECTED_ACTIVE_PAIR");
        upSql.Should().Contain("JD_ANALYSIS_V601_DUPLICATE_TAG");
        upSql.Should().Contain("JD_ANALYSIS_V601_FIXED_ROW_MISMATCH");
        upSql.Should().Contain("JD_ANALYSIS_V601_POSTCONDITION_FAILED");
        upSql.Should().Contain("cv_system_active_id");
        upSql.Should().Contain("cv_user_active_id");
        upSql.Should().Contain("matching_active_id");
        upSql.Should().Contain("JD_ANALYSIS_V601_UNRELATED_PROMPT_CHANGED");
        upSql.Should().Contain("HAVING COUNT(*) FILTER (WHERE v.\"IsActive\") <> 1");
        upSql.Should().Contain("position('[JOB_INPUT_JSON]' IN system_content) > 0");
        upSql.Should().Contain("position('[JOB_INPUT_JSON]' IN user_content) = 0");
    }

    [Fact]
    public void Down_RestoresExactV600WithoutDeletingHistoryOrTouchingParsedData()
    {
        var downSql = ReadSql(FindMigrationType(), "Down");
        var allSql = ReadSql(FindMigrationType(), "Up") + "\n" + downSql;

        downSql.Should().Contain(SystemVersionId);
        downSql.Should().Contain(UserVersionId);
        downSql.Should().Contain(PreviousSystemVersionId);
        downSql.Should().Contain(PreviousUserVersionId);
        downSql.Should().Contain("JD_ANALYSIS_V601_DOWN_NEWER_ACTIVE_PAIR");
        downSql.Should().Contain("JD_ANALYSIS_V601_DOWN_POSTCONDITION_FAILED");
        downSql.Should().NotContain("DELETE FROM \"PromptVersions\"");
        allSql.Should().NotContain("JobPostings");
        allSql.Should().NotContain("ParsedData");
        allSql.Should().NotContain("CvJobMatchScores");
    }

    [Fact]
    public void UpAndDown_CastTextModelConfigBeforeJsonbComparison()
    {
        var allSql = ReadSql(FindMigrationType(), "Up") + "\n" +
                     ReadSql(FindMigrationType(), "Down");

        Regex.Matches(
                allSql,
                "\\\"ModelConfig\\\"\\s*(?:=|<>)\\s*'[^']+'::jsonb",
                RegexOptions.CultureInvariant)
            .Should().BeEmpty("ModelConfig is stored as text and PostgreSQL cannot compare text directly with jsonb");
        Regex.Matches(
                allSql,
                "\\\"ModelConfig\\\"::jsonb\\s*(?:=|<>)\\s*'[^']+'::jsonb",
                RegexOptions.CultureInvariant)
            .Count.Should().BeGreaterThan(0);
    }

    private static Type FindMigrationType()
    {
        var migrationType = typeof(ITHunterviewContext).Assembly
            .GetTypes()
            .SingleOrDefault(type => type.GetCustomAttribute<MigrationAttribute>()?.Id
                .EndsWith("_SeedJdAnalysisV601ResponsibilityPromptPair", StringComparison.Ordinal) == true);

        migrationType.Should().NotBeNull("the JD Analysis v6.0.1 prompt-pair migration must be present");
        return migrationType!;
    }

    private static string ReadSql(Type migrationType, string methodName)
    {
        var migration = Activator.CreateInstance(migrationType);
        migration.Should().NotBeNull();
        var method = migrationType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
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

    private static string ReadFixture(string name) => File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "PromptAdmin", "Fixtures", name));

    private static string Hash(string content) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
}
