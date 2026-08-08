using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using ITHunterview.Service.Constant.Prompts;
using ITHunterview.Service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace ITHunterview.Service.Tests.PromptAdmin;

public sealed class CurrentActivePromptSeedMigrationTests
{
    [Fact]
    public void Up_SeedsTheApprovedActivePromptSnapshotsByteForByte()
    {
        var migrationType = FindMigrationType();
        var up = InvokeMigrationMethod(migrationType, "Up");
        var sql = string.Join("\n", up.Operations.OfType<SqlOperation>().Select(operation => operation.Sql));

        Hash(ExtractDollarQuoted(sql, "cv_v3_system")).Should().Be(
            "a3a723408d4d9cd42d6bcf5982dfadcbfc0bb8a12717fbef073ef99abe8e4ae4");
        Hash(ExtractDollarQuoted(sql, "cv_v3_user")).Should().Be(
            "54c58cea144c43d084b61a37a44f70415f799aee7956d671a45c787284cddfa3");
        Hash(ExtractDollarQuoted(sql, "jd_v5_2_system")).Should().Be(
            "b6b99a817fcc547d492fd986a42894bfc139580dbfb95cd12b544d133dd2164c");
        Hash(ExtractDollarQuoted(sql, "jd_v5_2_user")).Should().Be(
            "f9e18f683773e52abde128cbd7d9f3bf20d594d7aabe3cb61e83af2d3d045f6c");

        var matchingSeed = ExtractDollarQuoted(sql, "jd_matching_v2_0_1_semantic");
        matchingSeed.Should().Be(
            JdMatchingOutputSchema.NormalizeManagedContent(ReadActiveMatchingFixture()).SemanticContent);
        Hash(matchingSeed).Should().Be(
            "78e8bc2565b85e39afcda0ae569ef55160f2b78f67cadb5de4eb937e71f4d6eb");
        matchingSeed.Should().NotContain(JdMatchingOutputSchema.BeginMarker);
        matchingSeed.Should().NotContain("SCHEMA OUTPUT BẮT BUỘC");
        Hash(JdMatchingOutputSchema.LockedBlock).Should().Be(
            "2dd465d89778932d8f4bb644df9ffcfee9e68f38651bf16efe41b338d2b40370");

        sql.Should().Contain("LOCK TABLE \"PromptVersions\" IN SHARE ROW EXCLUSIVE MODE");
        sql.Should().Contain("'v3'");
        sql.Should().Contain("'v3.0'");
        sql.Should().Contain("'v5.2'");
        sql.Should().Contain("'v2.0.1'");
        sql.Should().Contain("{\"contract\":\"cv-analysis/v3\",\"role\":\"system\"}");
        sql.Should().Contain("{\"contract\":\"cv-analysis/v3\",\"role\":\"user\"}");
        sql.Should().Contain("{\"contract\":\"jd-analysis/v5.2\",\"role\":\"system\"}");
        sql.Should().Contain("{\"contract\":\"jd-analysis/v5.2\",\"role\":\"user\"}");
        sql.Should().Contain("CURRENT_ACTIVE_PROMPT_SEED_POSTCONDITION_FAILED");
        sql.Should().Contain("4969f6f7-5696-4700-8817-1fee806ecf9e");
        sql.Should().NotContain("3ff0ac2c-2377-4e72-b29a-d0c4fcff4b29");
    }

    [Fact]
    public void Down_RestoresCompatibleActiveVersionsWithoutDeletingHistory()
    {
        var down = InvokeMigrationMethod(FindMigrationType(), "Down");
        var sql = string.Join("\n", down.Operations.OfType<SqlOperation>().Select(operation => operation.Sql));

        sql.Should().Contain("d6c2a4f0-8b71-4e39-9011-000000000001");
        sql.Should().Contain("d6c2a4f0-8b71-4e39-9011-000000000004");
        sql.Should().Contain("3ff0ac2c-2377-4e72-b29a-d0c4fcff4b29");
        sql.Should().Contain("ORDER BY \"CreatedAt\" DESC, \"Id\" DESC");
        sql.Should().Contain("CURRENT_ACTIVE_PROMPT_SEED_DOWN_FALLBACK_NOT_FOUND");
        sql.Should().Contain("CURRENT_ACTIVE_PROMPT_SEED_DOWN_POSTCONDITION_FAILED");
        sql.Should().NotContain("DELETE FROM \"PromptVersions\"");
    }

    private static Type FindMigrationType()
    {
        var migrationType = typeof(ITHunterviewContext).Assembly
            .GetTypes()
            .SingleOrDefault(type => type.GetCustomAttribute<MigrationAttribute>()?.Id
                .EndsWith("_SeedCurrentActiveAnalysisAndMatchingPrompts", StringComparison.Ordinal) == true);

        migrationType.Should().NotBeNull(
            "the approved active CV, JD, and matching prompts must be reproducible on a fresh database");
        return migrationType!;
    }

    private static MigrationBuilder InvokeMigrationMethod(Type migrationType, string methodName)
    {
        var migration = Activator.CreateInstance(migrationType);
        var method = migrationType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();

        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        method!.Invoke(migration, [builder]);
        return builder;
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

    private static string ReadActiveMatchingFixture() => File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Matching", "Fixtures", "jd-matching-v2-active-prompt.txt"));

    private static string Hash(string content) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
}
