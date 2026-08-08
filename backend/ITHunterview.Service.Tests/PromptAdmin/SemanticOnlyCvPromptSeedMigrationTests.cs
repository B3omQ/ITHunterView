using System.Reflection;
using FluentAssertions;
using ITHunterview.Service.Constant.Prompts;
using ITHunterview.Service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace ITHunterview.Service.Tests.PromptAdmin;

public sealed class SemanticOnlyCvPromptSeedMigrationTests
{
    private const string SystemVersionId = "9dc9b06c-ac31-4673-9af5-41ae6ec5c098";
    private const string UserVersionId = "09abed76-a6a4-4d84-9e9e-8e5d231415af";
    private const string PreviousSystemVersionId = "9559310e-0c9e-4c2a-8601-d3ba9f92963e";
    private const string PreviousUserVersionId = "e1561d27-1596-4b8b-93c4-56aa137c7352";

    [Fact]
    public void Up_SeedsSemanticOnlyCvPairUsingTheReviewedHistoricalContent()
    {
        var upSql = ReadSql(FindMigrationType(), "Up");
        var oldSystem = ExtractDollarQuoted(ReadSql(FindHistoricalMigrationType(), "Up"), "cv_v3_system");
        var oldUser = ExtractDollarQuoted(ReadSql(FindHistoricalMigrationType(), "Up"), "cv_v3_user");
        var newSystem = ExtractDollarQuoted(upSql, "cv_v3_1_system");
        var newUser = ExtractDollarQuoted(upSql, "cv_v3_1_user");

        newSystem.Should().Be(CvAnalysisOutputSchema.NormalizeManagedContent(oldSystem).SemanticContent);
        newUser.Should().Be(CvAnalysisOutputSchema.NormalizeManagedContent(oldUser).SemanticContent);
        CvAnalysisOutputSchema.NormalizeManagedContent(newUser).SemanticContent.Should().Be(newUser);
        newUser.Should().NotContain("Required top-level structure");
        newUser.Should().NotContain("schema_version must be exactly");
        newUser.Should().NotContain("\"schema_version\"");
        CvAnalysisOutputSchema.ComposeSystemPrompt(oldSystem)
            .Should().Be(CvAnalysisOutputSchema.ComposeSystemPrompt(newSystem));
        newSystem.Should().NotContain(CvAnalysisOutputSchema.BeginMarker);
        newSystem.Should().NotContain("\"schema_version\": \"cv-analysis/v2\"");

        upSql.Should().Contain(SystemVersionId);
        upSql.Should().Contain(UserVersionId);
        upSql.Should().Contain("'v3.1.0'");
        upSql.Should().Contain("{\"contract\":\"cv-analysis/v3\",\"role\":\"system\"}");
        upSql.Should().Contain("{\"contract\":\"cv-analysis/v3\",\"role\":\"user\"}");
        upSql.Should().Contain("LOCK TABLE \"PromptVersions\" IN SHARE ROW EXCLUSIVE MODE");
        upSql.Should().Contain("CV_SEMANTIC_PROMPT_SEED_POSTCONDITION_FAILED");
        upSql.Should().Contain("Required top-level structure");
        upSql.Should().NotContain("JD_ANALYSIS");
        upSql.Should().NotContain("JD_MATCHING");
        upSql.Should().NotContain("CvJobMatchScores");
        upSql.Should().NotContain("wallet");
        upSql.Should().NotContain("coins");
    }

    [Fact]
    public void UpAndDown_UseAtomicActivationWithoutDeletingPromptHistory()
    {
        var upSql = ReadSql(FindMigrationType(), "Up");
        var downSql = ReadSql(FindMigrationType(), "Down");

        upSql.Should().Contain("UPDATE \"PromptVersions\"");
        upSql.Should().Contain("\"IsActive\" = TRUE");
        upSql.Should().Contain("HAVING COUNT(*) <> 1");
        upSql.Should().Contain("CURRENT_TIMESTAMP");
        upSql.Should().Contain("PromptKey");

        downSql.Should().Contain(PreviousSystemVersionId);
        downSql.Should().Contain(PreviousUserVersionId);
        downSql.Should().Contain("CV_SEMANTIC_PROMPT_SEED_DOWN_POSTCONDITION_FAILED");
        downSql.Should().Contain("LOCK TABLE \"PromptVersions\" IN SHARE ROW EXCLUSIVE MODE");
        downSql.Should().NotContain("DELETE FROM \"PromptVersions\"");
    }

    private static Type FindMigrationType()
    {
        var migrationType = typeof(ITHunterviewContext).Assembly
            .GetTypes()
            .SingleOrDefault(type => type.GetCustomAttribute<MigrationAttribute>()?.Id
                .EndsWith("_SeedSemanticOnlyCvAnalysisPromptPair", StringComparison.Ordinal) == true);

        migrationType.Should().NotBeNull("the semantic-only CV pair migration must be present");
        return migrationType!;
    }

    private static Type FindHistoricalMigrationType()
    {
        var migrationType = typeof(ITHunterviewContext).Assembly
            .GetTypes()
            .SingleOrDefault(type => type.GetCustomAttribute<MigrationAttribute>()?.Id
                .EndsWith("_SeedCurrentActiveAnalysisAndMatchingPrompts", StringComparison.Ordinal) == true);

        migrationType.Should().NotBeNull();
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
}
