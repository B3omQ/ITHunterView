using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using ITHunterview.Service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace ITHunterview.Service.Tests.PromptAdmin;

public sealed class AnalysisPromptSeedMigrationTests
{
    [Fact]
    public void Up_SeedsReviewedCvAndJdPromptSnapshotsWithRuntimeContracts()
    {
        var migrationType = typeof(ITHunterviewContext).Assembly
            .GetTypes()
            .SingleOrDefault(type => type.GetCustomAttribute<MigrationAttribute>()?.Id
                .EndsWith("_SeedActiveCvAndJdAnalysisPromptPairs", StringComparison.Ordinal) == true);

        migrationType.Should().NotBeNull("the reviewed active prompt pairs must be reproducible on a fresh database");

        var migration = Activator.CreateInstance(migrationType!);
        var upMethod = migrationType!.GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic);
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        upMethod.Should().NotBeNull();
        upMethod!.Invoke(migration, [builder]);

        var sql = string.Join(
            "\n",
            builder.Operations.OfType<SqlOperation>().Select(operation => operation.Sql));

        Hash(ExtractDollarQuoted(sql, "cv_v2_system")).Should().Be("a3a723408d4d9cd4");
        Hash(ExtractDollarQuoted(sql, "cv_v2_user")).Should().Be("4e19a226a6c83f79");
        Hash(ExtractDollarQuoted(sql, "jd_v4_system")).Should().Be("bb32aed0d9b730b0");
        Hash(ExtractDollarQuoted(sql, "jd_v4_user")).Should().Be("5690576c1b2c0269");

        sql.Should().Contain("'v2.0.1'");
        sql.Should().Contain("'v4.0.1'");
        sql.Should().Contain("{\"contract\":\"cv-analysis/v2\",\"role\":\"system\"}");
        sql.Should().Contain("{\"contract\":\"cv-analysis/v2\",\"role\":\"user\"}");
        sql.Should().Contain("{\"contract\":\"jd-analysis/v3\",\"role\":\"system\"}");
        sql.Should().Contain("{\"contract\":\"jd-analysis/v3\",\"role\":\"user\"}");
        sql.Should().Contain("LOCK TABLE \"PromptVersions\" IN SHARE ROW EXCLUSIVE MODE");
        sql.Should().Contain("PROMPT_SEED_POSTCONDITION_FAILED");
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

    private static string Hash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant()[..16];
    }
}
