using System.Text.Json;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Service.Matching;

namespace ITHunterview.Service.Tests.Matching;

public class JdStageTwoScoringTests
{
    [Fact]
    public void Build_PreservesItemCategoryAndGroupOperatorInPromptContext()
    {
        var context = new JdMatchingRequirementContextBuilder().Build(Projection());
        using var document = JsonDocument.Parse(context.Json);

        var entries = document.RootElement.EnumerateArray().ToArray();
        Assert.Equal("one_of", entries[0].GetProperty("Operator").GetString());
        Assert.Equal(1, entries[0].GetProperty("MinSatisfied").GetInt32());
        Assert.Equal("tech_skill", entries[0].GetProperty("Category").GetString());
        Assert.Equal("language", entries[1].GetProperty("Category").GetString());
    }

    [Fact]
    public void Build_PreservesExperienceBoundsInPromptContext()
    {
        var projection = new JdRequirementProjection("jd-analysis/v3", new[]
        {
            Group("g-years", "all_of", "must_have",
                new ProjectedJdRequirementItem("g-years:i1", "experience", "professional experience", "3-5 years", "3-5 years", "requirements", new[] { "3-5 years" }, 3, 5, JdRequirementCategoryWeights.Get("experience")))
        }, false);

        var context = new JdMatchingRequirementContextBuilder().Build(projection);
        using var document = JsonDocument.Parse(context.Json);
        var item = document.RootElement[0];

        Assert.Equal(3, item.GetProperty("MinYears").GetInt32());
        Assert.Equal(5, item.GetProperty("MaxYears").GetInt32());
    }

    [Fact]
    public void Validate_RejectsMissingItemScoreInsteadOfInventingZero()
    {
        using var response = JsonDocument.Parse("""
            {"scores":[{"reqId":"g1:i1","handlerCode":"H_TECH_05","handlerScore":1,"reasoning":"evidence","confidence":"high"}]}
            """);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new JdMatchingResponseAdapter().Adapt(response, Projection()));

        Assert.Equal(JdMatchingResponseValidator.InvalidStageTwoResponse, exception.Message);
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
              "scores": [
                {"reqId":"g-tech:i1","handlerCode":"H_TECH_01","handlerScore":0,"reasoning":"No React evidence","confidence":"high"},
                {"reqId":"g-language:i1","handlerCode":"H_LANG_06","handlerScore":1,"reasoning":"English B2","confidence":"high"}
              ],
              "narrative":"Candidate summary",
              "improvements":[],
              "penalties":[]
            }
            """);
        var validated = new JdMatchingResponseAdapter().Adapt(response, projection);

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
              "scores": [
                {"reqId":"g1:i1","handlerCode":"H_TECH_01","handlerScore":0,"reasoning":"No React","confidence":"high"},
                {"reqId":"g1:i2","handlerCode":"H_LANG_06","handlerScore":1,"reasoning":"English evidence","confidence":"high"}
              ],
              "narrative":"Candidate summary",
              "improvements":[],
              "penalties":[]
            }
            """);
        var validated = new JdMatchingResponseAdapter().Adapt(response, projection);

        var result = new JdFitScoreCalculator().Calculate(projection, validated);
        using var final = JsonDocument.Parse(result.JsonString);
        var groups = final.RootElement.GetProperty("jdFit").GetProperty("requirementGroups");

        Assert.Equal(1, groups.GetArrayLength());
        Assert.Equal(1m, groups[0].GetProperty("handlerScore").GetDecimal());
        Assert.Equal("tech_skill", groups[0].GetProperty("items")[0].GetProperty("category").GetString());
        Assert.Equal("language", groups[0].GetProperty("items")[1].GetProperty("category").GetString());
    }

    [Fact]
    public void Calculate_AtLeastN_SelectsOnlyTheTopNItemsIndependentlyOfInputOrder()
    {
        var firstOrder = new JdRequirementProjection("jd-analysis/v3", new[]
        {
            Group("g-tech", "at_least_n", "must_have",
                Item("g-tech:i1", "react", "tech_skill"),
                Item("g-tech:i2", "angular", "tech_skill"),
                Item("g-tech:i3", "vue", "tech_skill"))
        }, false);
        var reverseOrder = new JdRequirementProjection("jd-analysis/v3", new[]
        {
            Group("g-tech", "at_least_n", "must_have",
                Item("g-tech:i3", "vue", "tech_skill"),
                Item("g-tech:i2", "angular", "tech_skill"),
                Item("g-tech:i1", "react", "tech_skill"))
        }, false);
        using var response = JsonDocument.Parse("""
            {"scores":[
              {"reqId":"g-tech:i1","handlerCode":"H_TECH_05","handlerScore":1,"reasoning":"React evidence","confidence":"high"},
              {"reqId":"g-tech:i2","handlerCode":"H_TECH_05","handlerScore":0.7,"reasoning":"Angular evidence","confidence":"high"},
              {"reqId":"g-tech:i3","handlerCode":"H_TECH_05","handlerScore":0.2,"reasoning":"Vue evidence","confidence":"high"}
            ]}
            """);

        var validator = new JdMatchingResponseAdapter();
        var calculator = new JdFitScoreCalculator();
        var first = calculator.Calculate(firstOrder, validator.Adapt(response, firstOrder));
        var second = calculator.Calculate(reverseOrder, validator.Adapt(response, reverseOrder));
        using var final = JsonDocument.Parse(first.JsonString);
        var selected = final.RootElement.GetProperty("jdFit").GetProperty("requirementGroups")[0].GetProperty("selectedItemIds");

        Assert.Equal(89.5m, first.FinalScore);
        Assert.Equal(first.FinalScore, second.FinalScore);
        Assert.Equal(new[] { "g-tech:i1", "g-tech:i2" }, selected.EnumerateArray().Select(item => item.GetString()).ToArray());
    }

    [Fact]
    public void Validate_RejectsHandlerCodeFromAnotherCategory()
    {
        using var response = JsonDocument.Parse("""
            {
              "scores": [
                {"reqId":"g1:i1","handlerCode":"H_LANG_06","handlerScore":1,"reasoning":"evidence","confidence":"high"},
                {"reqId":"g1:i2","handlerCode":"H_LANG_06","handlerScore":1,"reasoning":"evidence","confidence":"high"}
              ]
            }
            """);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new JdMatchingResponseAdapter().Adapt(response, Projection()));

        Assert.Equal(JdMatchingResponseValidator.InvalidStageTwoResponse, exception.Message);
    }

    [Fact]
    public void Calculate_IgnoresModelControlledCredibilityPenalty()
    {
        using var response = JsonDocument.Parse("""
            {
              "scores": [
                {"reqId":"g1:i1","handlerCode":"H_TECH_05","handlerScore":1,"reasoning":"evidence","confidence":"high"},
                {"reqId":"g1:i2","handlerCode":"H_LANG_06","handlerScore":1,"reasoning":"evidence","confidence":"high"}
              ],
              "penalties": [
                {"code":"PNL_TC1_01","triggered":true,"evidence":"model claim"}
              ]
            }
            """);

        var validated = new JdMatchingResponseAdapter().Adapt(response, Projection());
        var result = new JdFitScoreCalculator().Calculate(Projection(), validated);
        using var final = JsonDocument.Parse(result.JsonString);

        Assert.Equal(100m, result.FinalScore);
        Assert.DoesNotContain(final.RootElement.GetProperty("jdFit").GetProperty("penalties").EnumerateArray(),
            penalty => penalty.GetProperty("code").GetString() == "PNL_TC1_01");
    }

    [Fact]
    public void Validate_RejectsDuplicateModelPenaltyObservation()
    {
        using var response = JsonDocument.Parse("""
            {
              "scores": [
                {"reqId":"g1:i1","handlerCode":"H_TECH_05","handlerScore":1,"reasoning":"evidence","confidence":"high"},
                {"reqId":"g1:i2","handlerCode":"H_LANG_06","handlerScore":1,"reasoning":"evidence","confidence":"high"}
              ],
              "penalties": [
                {"code":"PNL_TC1_01","triggered":true,"evidence":"first observation"},
                {"code":"PNL_TC1_01","triggered":false,"evidence":"duplicate observation"}
              ]
            }
            """);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new JdMatchingResponseAdapter().Adapt(response, Projection()));

        Assert.Equal(JdMatchingResponseValidator.InvalidStageTwoResponse, exception.Message);
    }

    private static JdRequirementProjection Projection() => new(
        "jd-analysis/v3",
        new[] { Group("g1", "one_of", "must_have", Item("g1:i1", "react", "tech_skill"), Item("g1:i2", "english", "language")) },
        false);

    private static ProjectedJdRequirementGroup Group(string id, string operation, string importance, params ProjectedJdRequirementItem[] items) =>
        new(id, operation, operation == "all_of" ? items.Length : operation == "at_least_n" ? 2 : 1, importance, items);

    private static ProjectedJdRequirementItem Item(string id, string name, string category) =>
        new(id, category, name, name, name, "requirements", new[] { name }, null, null, JdRequirementCategoryWeights.Get(category));
}
