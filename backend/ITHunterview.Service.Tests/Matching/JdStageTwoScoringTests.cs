using System.Text.Json;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Service.Matching;

namespace ITHunterview.Service.Tests.Matching;

public class JdStageTwoScoringTests
{
    [Fact]
    public void Build_PreservesItemCategoryAndGroupOperatorInPromptContext()
    {
        var context = new JdStageTwoContextBuilder().Build(Projection());
        using var document = JsonDocument.Parse(context.Json);

        var group = document.RootElement.GetProperty("requirementGroups")[0];
        Assert.Equal("one_of", group.GetProperty("operator").GetString());
        Assert.Equal(1, group.GetProperty("minSatisfied").GetInt32());
        Assert.Equal("tech_skill", group.GetProperty("items")[0].GetProperty("category").GetString());
        Assert.Equal("language", group.GetProperty("items")[1].GetProperty("category").GetString());
    }

    [Fact]
    public void Validate_RejectsMissingItemScoreInsteadOfInventingZero()
    {
        using var response = JsonDocument.Parse("""
            {"itemScores":[{"itemId":"g1:i1","handlerCode":"S1","handlerScore":1,"reasoning":"evidence","confidence":"high"}]}
            """);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new JdStageTwoResponseValidator().Validate(response, Projection()));

        Assert.Equal(JdStageTwoResponseValidator.InvalidStageTwoResponse, exception.Message);
    }

    [Fact]
    public void Calculate_KswOnlyTriggersWhenEveryCoreTechnicalMustHaveItemScoresZero()
    {
        var projection = new JdRequirementProjection("jd-analysis/v3", new[]
        {
            Group("g-tech", "all_of", "must_have", Item("g-tech:i1", "react", "tech_skill")),
            Group("g-language", "all_of", "must_have", Item("g-language:i1", "english", "language"))
        }, false);
        using var response = JsonDocument.Parse("""
            {
              "itemScores": [
                {"itemId":"g-tech:i1","handlerCode":"S1","handlerScore":0,"reasoning":"No React evidence","confidence":"high"},
                {"itemId":"g-language:i1","handlerCode":"S1","handlerScore":1,"reasoning":"English B2","confidence":"high"}
              ],
              "narrative":"Candidate summary",
              "improvements":[],
              "penalties":[]
            }
            """);
        var validated = new JdStageTwoResponseValidator().Validate(response, projection);

        var result = new JdFitScoreCalculator().Calculate(projection, validated);
        using var final = JsonDocument.Parse(result.JsonString);

        Assert.Equal(15m, result.FinalScore);
        Assert.True(final.RootElement.GetProperty("jdFit").GetProperty("killSwitchTriggered").GetBoolean());
        Assert.Equal("KSW_01", final.RootElement.GetProperty("jdFit").GetProperty("penalties")[0].GetProperty("code").GetString());
    }

    [Fact]
    public void Calculate_OneOfGroupIsOneRequirementAndKeepsItsItemCategories()
    {
        var projection = Projection();
        using var response = JsonDocument.Parse("""
            {
              "itemScores": [
                {"itemId":"g1:i1","handlerCode":"S1","handlerScore":0,"reasoning":"No React","confidence":"high"},
                {"itemId":"g1:i2","handlerCode":"S1","handlerScore":1,"reasoning":"English evidence","confidence":"high"}
              ],
              "narrative":"Candidate summary",
              "improvements":[],
              "penalties":[]
            }
            """);
        var validated = new JdStageTwoResponseValidator().Validate(response, projection);

        var result = new JdFitScoreCalculator().Calculate(projection, validated);
        using var final = JsonDocument.Parse(result.JsonString);
        var groups = final.RootElement.GetProperty("jdFit").GetProperty("requirementGroups");

        Assert.Equal(1, groups.GetArrayLength());
        Assert.Equal(1m, groups[0].GetProperty("handlerScore").GetDecimal());
        Assert.Equal("tech_skill", groups[0].GetProperty("items")[0].GetProperty("category").GetString());
        Assert.Equal("language", groups[0].GetProperty("items")[1].GetProperty("category").GetString());
    }

    private static JdRequirementProjection Projection() => new(
        "jd-analysis/v3",
        new[] { Group("g1", "one_of", "must_have", Item("g1:i1", "react", "tech_skill"), Item("g1:i2", "english", "language")) },
        false);

    private static ProjectedJdRequirementGroup Group(string id, string operation, string importance, params ProjectedJdRequirementItem[] items) =>
        new(id, operation, operation == "all_of" ? items.Length : 1, importance, items);

    private static ProjectedJdRequirementItem Item(string id, string name, string category) =>
        new(id, category, name, name, name, "requirements", new[] { name }, null, null, JdRequirementCategoryWeights.Get(category));
}
