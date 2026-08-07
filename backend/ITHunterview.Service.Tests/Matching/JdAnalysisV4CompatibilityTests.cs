using System.Text.Json;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Service;
using ITHunterview.Service.Service.Matching;
using ITHunterview.Service.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ITHunterview.Service.Tests.Matching;

public sealed class JdAnalysisV4CompatibilityTests
{
    private const string Clause = "Understanding of caching strategies, job queues, and asynchronous processing (e.g., Redis, Horizon, or similar tools).";

    [Fact]
    public void CompactV4_UsesCanonicalV3AcrossProjectorHardcodeAndStageTwo()
    {
        var effectiveJson = ValidateAndSerialize(ReadFixture("jd-v4-compact-caching-group.json"));

        using var document = JsonDocument.Parse(effectiveJson);
        Assert.Equal("jd-analysis/v3", document.RootElement.GetProperty("schema_version").GetString());
        var group = Assert.Single(document.RootElement.GetProperty("matching_metrics").GetProperty("requirement_groups").EnumerateArray());
        Assert.Equal(3, group.GetProperty("min_satisfied").GetInt32());
        Assert.Equal(3, group.GetProperty("items").GetArrayLength());
        Assert.All(group.GetProperty("items").EnumerateArray(), item =>
            Assert.Equal(Clause, item.GetProperty("evidences")[0].GetString()));
        var normalizedSkills = document.RootElement
            .GetProperty("matching_metrics")
            .GetProperty("skills_normalized");
        Assert.Equal(3, normalizedSkills.GetArrayLength());
        Assert.Contains(normalizedSkills.EnumerateArray(), skill =>
            skill.GetProperty("name").GetString() == "caching");

        var projection = new JdRequirementProjector().Project(effectiveJson);
        Assert.Equal("jd-analysis/v3", projection.SourceSchemaVersion);
        Assert.False(projection.UsesLegacySemantics);
        var projectedGroup = Assert.Single(projection.Groups);
        Assert.Equal(3, projectedGroup.Items.Count);

        var evaluator = new JdHardcodeRequirementEvaluator();
        var complete = evaluator.Evaluate(projection, new[] { "caching", "job queues", "asynchronous processing" });
        var partial = evaluator.Evaluate(projection, new[] { "caching", "job queues" });
        Assert.Equal(1m, complete.SkillScore);
        Assert.Equal(2m / 3m, partial.SkillScore);

        var context = new JdMatchingRequirementContextBuilder().Build(projection);
        Assert.Equal(1, context.GroupCount);
        Assert.Equal(3, context.RequirementCount);
        Assert.All(projectedGroup.Items, item => Assert.Contains(item.ItemId, context.Json, StringComparison.Ordinal));

        var secondProjection = new JdRequirementProjector().Project(ValidateAndSerialize(ReadFixture("jd-v4-compact-caching-group.json")));
        Assert.Equal(projectedGroup.GroupId, Assert.Single(secondProjection.Groups).GroupId);
        Assert.Equal(
            projectedGroup.Items.Select(item => item.ItemId),
            Assert.Single(secondProjection.Groups).Items.Select(item => item.ItemId));
    }

    [Fact]
    public void EquivalentV3AndV4_ProduceTheSameCanonicalRequirementIds()
    {
        const string v3 = """
        {"schema_version":"jd-analysis/v3","matching_metrics":{"job_titles_normalized":[],"skills_normalized":[],"total_years_exp":0,"domains":[],"requirement_groups":[{"operator":"all_of","min_satisfied":3,"importance":"must_have","items":[{"category":"tech_skill","skill_name":"caching","detail_verbatim":"Understanding of caching strategies, job queues, and asynchronous processing (e.g., Redis, Horizon, or similar tools).","raw_mention":"caching strategies","source_section":"requirements","evidences":["Understanding of caching strategies, job queues, and asynchronous processing (e.g., Redis, Horizon, or similar tools)."]},{"category":"tech_skill","skill_name":"job queues","detail_verbatim":"Understanding of caching strategies, job queues, and asynchronous processing (e.g., Redis, Horizon, or similar tools).","raw_mention":"job queues","source_section":"requirements","evidences":["Understanding of caching strategies, job queues, and asynchronous processing (e.g., Redis, Horizon, or similar tools)."]},{"category":"tech_skill","skill_name":"asynchronous processing","detail_verbatim":"Understanding of caching strategies, job queues, and asynchronous processing (e.g., Redis, Horizon, or similar tools).","raw_mention":"asynchronous processing","source_section":"requirements","evidences":["Understanding of caching strategies, job queues, and asynchronous processing (e.g., Redis, Horizon, or similar tools)."]}]}]}}
        """;

        var v4Projection = new JdRequirementProjector().Project(ValidateAndSerialize(ReadFixture("jd-v4-compact-caching-group.json")));
        var v3Projection = new JdRequirementProjector().Project(ValidateAndSerialize(v3));

        var v4Group = Assert.Single(v4Projection.Groups);
        var v3Group = Assert.Single(v3Projection.Groups);
        Assert.Equal(v3Group.GroupId, v4Group.GroupId);
        Assert.Equal(v3Group.Items.Select(item => item.ItemId), v4Group.Items.Select(item => item.ItemId));
    }

    private static string ValidateAndSerialize(string providerOutput)
    {
        var validator = new JdAnalysisResponseValidator();
        var validation = validator.Validate(providerOutput, new JobAnalysisInputSnapshot { Requirements = Clause });
        Assert.True(validation.IsValid, validation.FailureCode);

        var service = new JobAnalysisExtractionService(
            Mock.Of<IAiService>(),
            Mock.Of<IPromptManagementService>(),
            Mock.Of<IJdAnalysisResponseValidator>(),
            NullLogger<JobAnalysisExtractionService>.Instance);
        return service.SerializeEffectiveAnalysis(validation.Data!);
    }

    private static string ReadFixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Matching", "Fixtures", name));
}
