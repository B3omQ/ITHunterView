using System.Text.Json;
using FluentAssertions;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Service.Matching;

namespace ITHunterview.Service.Tests.Matching;

public sealed class JdMatchingResponseAdapterTests
{
    [Fact]
    public void Adapt_ApprovedScoresReqIdShape_AcceptsMixedCategories()
    {
        var projection = MixedProjection();
        using var response = JsonDocument.Parse("""
            {
              "scores": [
                { "reqId": "grp:tech", "handlerCode": "H_TECH_05", "handlerScore": 1.0, "reasoning": "Production evidence", "confidence": "high", "flag": null, "evidence": ["Built React features"] },
                { "reqId": "grp:exp", "handlerCode": "H_EXP_06", "handlerScore": 0.8, "reasoning": "Timeline meets requirement", "confidence": "medium", "flag": null, "evidence": ["Five years"] }
              ],
              "criticalGaps": [],
              "penalties": [],
              "narrative": "The candidate is a strong fit.",
              "improvements": []
            }
            """);

        var result = new JdMatchingResponseAdapter().Adapt(response, projection);

        result.ItemScores.Should().HaveCount(2);
        result.ItemScores["grp:tech"].HandlerScore.Should().Be(1m);
        result.ItemScores["grp:exp"].HandlerCode.Should().Be("H_EXP_06");
        result.Narrative.Should().Be("The candidate is a strong fit.");
    }

    [Fact]
    public void Adapt_MissingUnknownOrDuplicateIds_KeepsOnlyValidScoresAsPartial()
    {
        var projection = MixedProjection();
        using var response = JsonDocument.Parse("""
            {
              "scores": [
                {"reqId":"grp:tech","handlerCode":"H_TECH_05","handlerScore":1},
                {"reqId":"grp:tech","handlerCode":"H_TECH_04","handlerScore":0},
                {"reqId":"grp:unknown","handlerCode":"H_TECH_05","handlerScore":1}
              ]
            }
            """);

        var result = new JdMatchingResponseAdapter().Adapt(response, projection);

        result.Quality.Should().Be(JdStageTwoOutputQuality.PARTIAL);
        result.ItemScores.Should().ContainSingle().Which.Key.Should().Be("grp:tech");
        result.Coverage.ExpectedScoreCount.Should().Be(2);
        result.Coverage.AcceptedScoreCount.Should().Be(1);
        result.Coverage.MissingScoreCount.Should().Be(1);
        result.Coverage.DiscardedScoreCount.Should().Be(2);
    }

    [Fact]
    public void Adapt_OutOfRangeOrHandlerCategoryMismatch_DiscardsBadItemsWithoutLosingGoodItems()
    {
        var projection = MixedProjection();
        using var response = JsonDocument.Parse("""
            {"scores":[
              {"reqId":"grp:tech","handlerCode":"H_EXP_06","handlerScore":1},
              {"reqId":"grp:exp","handlerCode":"H_EXP_06","handlerScore":1}
            ]}
            """);

        var result = new JdMatchingResponseAdapter().Adapt(response, projection);

        result.Quality.Should().Be(JdStageTwoOutputQuality.PARTIAL);
        result.ItemScores.Should().ContainSingle().Which.Key.Should().Be("grp:exp");
    }

    [Fact]
    public void Adapt_MissingOptionalEvidenceAndExtraTopLevelFields_IsAccepted()
    {
        var projection = MixedProjection();
        using var response = JsonDocument.Parse("""
            {
              "scores": [
                { "reqId": "grp:tech", "handlerCode": "H_TECH_05", "handlerScore": 1.0 },
                { "reqId": "grp:exp", "handlerCode": "H_EXP_06", "handlerScore": 0.8 }
              ],
              "futureField": { "ignored": true }
            }
            """);

        var result = new JdMatchingResponseAdapter().Adapt(response, projection);

        result.ItemScores["grp:tech"].Evidence.Should().BeEmpty();
        result.ItemScores["grp:tech"].Confidence.Should().Be("unknown");
    }

    [Fact]
    public void Adapt_InvalidOptionalTopLevelType_PreservesCompleteScoresAsPartial()
    {
        var projection = MixedProjection();
        using var response = JsonDocument.Parse("""
            {
              "scores": [
                { "reqId": "grp:tech", "handlerCode": "H_TECH_05", "handlerScore": 1.0 },
                { "reqId": "grp:exp", "handlerCode": "H_EXP_06", "handlerScore": 0.8 }
              ],
              "improvements": "not-an-array"
            }
            """);

        var result = new JdMatchingResponseAdapter().Adapt(response, projection);

        result.Quality.Should().Be(JdStageTwoOutputQuality.PARTIAL);
        result.ItemScores.Should().HaveCount(2);
        result.Improvements.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public void Adapt_ModelPenaltyIsOnlyCopiedAndCannotChangeBackendScore()
    {
        var projection = MixedProjection();
        using var response = JsonDocument.Parse("""
            {
              "scores": [
                { "reqId": "grp:tech", "handlerCode": "H_TECH_05", "handlerScore": 1.0 },
                { "reqId": "grp:exp", "handlerCode": "H_EXP_06", "handlerScore": 1.0 }
              ],
              "penalties": [
                { "code": "PNL_TC1_01", "triggered": true, "evidence": "model claim" }
              ]
            }
            """);

        var validated = new JdMatchingResponseAdapter().Adapt(response, projection);
        var calculation = new JdFitScoreCalculator().Calculate(projection, validated);

        validated.Penalties.Should().ContainSingle(item => item.Triggered);
        calculation.FinalScore.Should().Be(100m);
    }

    private static JdRequirementProjection MixedProjection() => new(
        "jd-analysis/v4",
        new[]
        {
            new ProjectedJdRequirementGroup(
                "grp",
                "all_of",
                2,
                "must_have",
                new[]
                {
                    Item("grp:tech", "tech_skill", "React"),
                    Item("grp:exp", "experience", "software development")
                })
        },
        false);

    private static ProjectedJdRequirementItem Item(string id, string category, string skill) => new(
        id,
        category,
        skill,
        skill,
        skill,
        "requirements",
        Array.Empty<string>(),
        null,
        null,
        JdRequirementCategoryWeights.Get(category));
}
