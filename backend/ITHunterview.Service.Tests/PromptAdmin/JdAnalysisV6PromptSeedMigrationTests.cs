using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using ITHunterview.Service.Constant.Prompts;
using ITHunterview.Service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace ITHunterview.Service.Tests.PromptAdmin;

public sealed class JdAnalysisV6PromptSeedMigrationTests
{
    private const string SystemVersionId = "3a7a42f2-f4f0-4a2a-8d65-8d4c8b16d501";
    private const string UserVersionId = "3a7a42f2-f4f0-4a2a-8d65-8d4c8b16d502";
    private const string PreviousSystemVersionId = "116d8e1c-a9fd-4c45-9ed8-76406af92edc";
    private const string PreviousUserVersionId = "f077bef1-d090-4f9c-a39a-035868f083e6";
    private const string SystemContentHash = "fb382fb3745878ed2a4a80f398a0493f4b6f6e637b3c5217230353e1a3724bce";
    private const string UserContentHash = "e003aad9808ca95daf179d35ac4ecafb9b0ab52064df1dac664526518431ca1b";

    [Fact]
    public void Up_SeedsTheReviewedSemanticOnlyV6PairByteForByte()
    {
        var upSql = ReadSql(FindMigrationType(), "Up");
        var systemContent = ExtractDollarQuoted(upSql, "jd_v6_system");
        var userContent = ExtractDollarQuoted(upSql, "jd_v6_user");
        var expectedSystem = ReadFixture("jd-analysis-v6-system-semantic.txt");
        var expectedUser = ReadFixture("jd-analysis-v6-user-semantic.txt");

        systemContent.Should().Be(expectedSystem);
        userContent.Should().Be(expectedUser);
        Hash(systemContent).Should().Be(SystemContentHash);
        Hash(userContent).Should().Be(UserContentHash);

        var normalized = JdAnalysisOutputSchema.NormalizeManagedContent(systemContent);
        normalized.RemovedKnownSchema.Should().BeFalse();
        normalized.SemanticContent.Should().Be(systemContent.Trim());
        Count(JdAnalysisOutputSchema.ComposeSystemPrompt(systemContent), JdAnalysisOutputSchema.BeginMarker)
            .Should().Be(1);
        Count(systemContent, JdAnalysisPromptContract.UserPlaceholder).Should().Be(0);
        Count(userContent, JdAnalysisPromptContract.UserPlaceholder).Should().Be(1);

        systemContent.Should().NotContain("\"schema_version\"");
        systemContent.Should().NotContain("\"requirement_groups\"");
        systemContent.Should().NotContain("OUTPUT CONTRACT");
        systemContent.Should().NotContain(JdAnalysisOutputSchema.BeginMarker);
        systemContent.Should().NotContain(JdAnalysisOutputSchema.EndMarker);

        upSql.Should().Contain(SystemVersionId);
        upSql.Should().Contain(UserVersionId);
        upSql.Should().Contain("'v6.0.0'");
        upSql.Should().Contain("{\"contract\":\"jd-analysis-prompt/v6\",\"role\":\"system\"}");
        upSql.Should().Contain("{\"contract\":\"jd-analysis-prompt/v6\",\"role\":\"user\"}");
        upSql.Should().Contain("LOCK TABLE \"PromptVersions\" IN SHARE ROW EXCLUSIVE MODE");
        upSql.Should().Contain("HAVING COUNT(*) <> 1");
        upSql.Should().Contain("JD_ANALYSIS_V6_PROMPT_SEED_POSTCONDITION_FAILED");
        upSql.Should().Contain("position('[JOB_INPUT_JSON]' IN system_content) > 0");
        upSql.Should().NotContain("CV_ANALYSIS");
        upSql.Should().NotContain("JD_MATCHING");
        upSql.Should().NotContain("CvJobMatchScores");
        upSql.Contains("wallet", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
        upSql.Contains("coins", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
    }

    [Fact]
    public void UpAndDown_ActivateAtomicallyAndPreservePromptHistory()
    {
        var migrationType = FindMigrationType();
        var upSql = ReadSql(migrationType, "Up");
        var downSql = ReadSql(migrationType, "Down");

        upSql.Should().Contain("UPDATE \"PromptVersions\"");
        upSql.Should().Contain("\"IsActive\" = FALSE");
        upSql.Should().Contain("\"IsActive\" = TRUE");
        upSql.Should().Contain("CURRENT_TIMESTAMP");
        upSql.Should().Contain("ON CONFLICT (\"Id\") DO UPDATE");

        downSql.Should().Contain(PreviousSystemVersionId);
        downSql.Should().Contain(PreviousUserVersionId);
        downSql.Should().Contain("{\"contract\":\"jd-analysis/v5.2\",\"role\":\"system\"}");
        downSql.Should().Contain("{\"contract\":\"jd-analysis/v5.2\",\"role\":\"user\"}");
        downSql.Should().Contain("JD_ANALYSIS_V6_PROMPT_SEED_DOWN_POSTCONDITION_FAILED");
        downSql.Should().Contain("LOCK TABLE \"PromptVersions\" IN SHARE ROW EXCLUSIVE MODE");
        downSql.Should().NotContain("DELETE FROM \"PromptVersions\"");
    }

    private static Type FindMigrationType()
    {
        var migrationType = typeof(ITHunterviewContext).Assembly
            .GetTypes()
            .SingleOrDefault(type => type.GetCustomAttribute<MigrationAttribute>()?.Id
                .EndsWith("_SeedJdAnalysisV6SemanticPromptPair", StringComparison.Ordinal) == true);

        migrationType.Should().NotBeNull("the semantic-only JD Analysis v6 pair migration must be present");
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

    private static int Count(string content, string value) =>
        content.Split(value, StringSplitOptions.None).Length - 1;
}
