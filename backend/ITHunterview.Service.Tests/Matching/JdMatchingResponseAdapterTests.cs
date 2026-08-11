using System.Text.Json;
using FluentAssertions;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Service.Matching;

namespace ITHunterview.Service.Tests.Matching;

public sealed class JdMatchingResponseAdapterTests
{
    [Fact]
    public void Adapt_ApprovedHandlerCodes_MapsScoresFromBackendPolicy()
    {
        using var response = JsonDocument.Parse("""
            {
              "schemaVersion":"jd-stage2/v2",
              "scores":[
                {
                  "reqId":"grp:tech",
                  "handlerCode":"H_TECH_05",
                  "handlerScore":0,
                  "reasoning":"React is used in a production feature.",
                  "evidence":[{"quotation":"Built React checkout","section":"Experience / Alpha"}]
                },
                {"reqId":"grp:exp","handlerCode":"H_EXP_D04","handlerScore":1}
              ],
              "narrative":"The candidate covers most requirements."
            }
            """);

        var result = new JdMatchingResponseAdapter().Adapt(response, MixedProjection());

        result.Quality.Should().Be(JdStageTwoOutputQuality.COMPLETE);
        result.ItemAssessments.Should().HaveCount(2);
        result.ItemAssessments["grp:tech"].Score.Should().Be(1m,
            "provider numeric fields are ignored and the handler policy owns the score");
        result.ItemAssessments["grp:exp"].Score.Should().Be(0.75m);
        result.ItemAssessments["grp:tech"].Evidence.Should().ContainSingle()
            .Which.Section.Should().Be("Experience / Alpha");
    }

    [Fact]
    public void Adapt_MissingReasoningAndEvidence_KeepsCompleteCoverageAndScores()
    {
        using var response = JsonDocument.Parse("""
            {"schemaVersion":"jd-stage2/v2","scores":[
              {"reqId":"grp:tech","handlerCode":"H_TECH_05"},
              {"reqId":"grp:exp","handlerCode":"H_EXP_D05","reasoning":42,"evidence":"bad"}
            ]}
            """);

        var result = new JdMatchingResponseAdapter().Adapt(response, MixedProjection());

        result.Quality.Should().Be(JdStageTwoOutputQuality.COMPLETE);
        result.ItemAssessments.Should().HaveCount(2);
        result.ItemAssessments["grp:tech"].Score.Should().Be(1m);
        result.ItemAssessments["grp:exp"].Score.Should().Be(1m);
        result.ItemAssessments["grp:exp"].Reasoning.Should().BeEmpty();
        result.ItemAssessments["grp:exp"].Evidence.Should().BeEmpty();
        result.WarningCodes.Should().Contain("REASONING_MISSING_OR_INVALID");
        result.WarningCodes.Should().Contain("EVIDENCE_MISSING_OR_INVALID");
    }

    [Fact]
    public void Adapt_SemanticallyDubiousButStructuredDetails_PreservesWithoutJudgement()
    {
        using var response = JsonDocument.Parse("""
            {"schemaVersion":"jd-stage2/v2","scores":[
              {"reqId":"grp:tech","handlerCode":"H_TECH_05","reasoning":"A dubious claim is still provider text.","evidence":[{"quotation":"unrelated words","section":"Summary"}]},
              {"reqId":"grp:exp","handlerCode":"H_EXP_D05"}
            ]}
            """);

        var result = new JdMatchingResponseAdapter().Adapt(response, MixedProjection());

        result.Quality.Should().Be(JdStageTwoOutputQuality.COMPLETE);
        result.ItemAssessments["grp:tech"].Reasoning.Should().Contain("dubious claim");
        result.ItemAssessments["grp:tech"].Evidence.Should().ContainSingle();
    }

    [Fact]
    public void Adapt_KnownScoringCodeFromDifferentFamily_AcceptsAndPreservesExpectedCategory()
    {
        using var response = JsonDocument.Parse("""
            {"schemaVersion":"jd-stage2/v2","scores":[
              {"reqId":"grp:tech","handlerCode":"H_EXP_D04"}
            ]}
            """);

        var result = new JdMatchingResponseAdapter().Adapt(response, SingleProjection());

        result.Quality.Should().Be(JdStageTwoOutputQuality.COMPLETE);
        result.ItemAssessments.Should().ContainSingle();
        result.ItemAssessments["grp:tech"].Category.Should().Be("tech_skill");
        result.ItemAssessments["grp:tech"].HandlerCode.Should().Be("H_EXP_D04");
        result.ItemAssessments["grp:tech"].Score.Should().Be(0.75m);
        result.Coverage.MissingItemIds.Should().BeEmpty();
        result.WarningCodes.Should().NotContain("HANDLER_CODE_CATEGORY_MISMATCH");
        result.HandlerDiagnostics.Should().ContainSingle(diagnostic =>
            diagnostic.Code == "HANDLER_CATEGORY_DIFFERENCE_ACCEPTED");
    }

    [Fact]
    public void Adapt_KnownCodeWithDifferentCase_AcceptsCanonicalCode()
    {
        using var response = JsonDocument.Parse("""
            {"schemaVersion":"jd-stage2/v2","scores":[
              {"reqId":"grp:tech","handlerCode":"  h_tech_05  "}
            ]}
            """);

        var result = new JdMatchingResponseAdapter().Adapt(response, SingleProjection());

        result.Quality.Should().Be(JdStageTwoOutputQuality.COMPLETE);
        result.ItemAssessments["grp:tech"].HandlerCode.Should().Be("H_TECH_05");
        result.HandlerDiagnostics.Should().ContainSingle(diagnostic =>
            diagnostic.Code == "HANDLER_CODE_CASE_NORMALIZED");
    }

    [Fact]
    public void Adapt_UnknownHandlerCode_DiscardsOnlyThatAssessment()
    {
        using var response = JsonDocument.Parse("""
            {"schemaVersion":"jd-stage2/v2","scores":[
              {"reqId":"grp:tech","handlerCode":"H_TECH_04"},
              {"reqId":"grp:exp","handlerCode":"H_UNKNOWN_99"}
            ]}
            """);

        var result = new JdMatchingResponseAdapter().Adapt(response, MixedProjection());

        result.Quality.Should().Be(JdStageTwoOutputQuality.PARTIAL);
        result.ItemAssessments.Should().ContainSingle().Which.Key.Should().Be("grp:tech");
        result.Coverage.MissingItemIds.Should().Equal("grp:exp");
        result.HandlerDiagnostics.Should().ContainSingle(diagnostic =>
            diagnostic.Code == "UNKNOWN_HANDLER_CODE");
    }

    [Fact]
    public void Adapt_NonScoringHandlerCode_DiscardsOnlyThatAssessment()
    {
        using var response = JsonDocument.Parse("""
            {"schemaVersion":"jd-stage2/v2","scores":[
              {"reqId":"grp:tech","handlerCode":"H_TECH_04"},
              {"reqId":"grp:exp","handlerCode":"H_EXP_00"}
            ]}
            """);

        var result = new JdMatchingResponseAdapter().Adapt(response, MixedProjection());

        result.Quality.Should().Be(JdStageTwoOutputQuality.PARTIAL);
        result.ItemAssessments.Should().ContainSingle().Which.Key.Should().Be("grp:tech");
        result.Coverage.MissingItemIds.Should().Equal("grp:exp");
        result.HandlerDiagnostics.Should().ContainSingle(diagnostic =>
            diagnostic.Code == "NON_SCORING_HANDLER_CODE");
    }

    [Fact]
    public void Adapt_DuplicateReqId_DiscardsDuplicateAccordingToExistingRule()
    {
        using var response = JsonDocument.Parse("""
            {"schemaVersion":"jd-stage2/v2","scores":[
              {"reqId":"grp:tech","handlerCode":"H_TECH_04"},
              {"reqId":"grp:tech","handlerCode":"H_TECH_05"},
              {"reqId":"grp:exp","handlerCode":"H_EXP_D05"}
            ]}
            """);

        var result = new JdMatchingResponseAdapter().Adapt(response, MixedProjection());

        result.Quality.Should().Be(JdStageTwoOutputQuality.COMPLETE);
        result.ItemAssessments["grp:tech"].HandlerCode.Should().Be("H_TECH_04");
        result.Coverage.DiscardedCount.Should().Be(1);
        result.WarningCodes.Should().Contain("DUPLICATE_REQUIREMENT_ID");
    }

    [Fact]
    public void Adapt_UnsupportedSchemaVersion_IsInvalid()
    {
        using var response = JsonDocument.Parse("""
            {"schemaVersion":"jd-stage2/v1","scores":[{"reqId":"grp:tech","handlerCode":"H_TECH_05"}]}
            """);

        var result = new JdMatchingResponseAdapter().Adapt(response, MixedProjection());

        result.Quality.Should().Be(JdStageTwoOutputQuality.INVALID);
        result.ItemAssessments.Should().BeEmpty();
        result.WarningCodes.Should().Contain("UNSUPPORTED_SCHEMA_VERSION");
    }

    [Fact]
    public void Adapt_DeduplicatesExactStructuredEvidence()
    {
        using var response = JsonDocument.Parse("""
            {"schemaVersion":"jd-stage2/v2","scores":[
              {"reqId":"grp:tech","handlerCode":"H_TECH_05","evidence":[
                {"quotation":"Built React","section":"Project A"},
                {"quotation":"Built React","section":"Project A"},
                {"quotation":"Built React","section":"Project B"}
              ]},
              {"reqId":"grp:exp","handlerCode":"H_EXP_D05"}
            ]}
            """);

        var result = new JdMatchingResponseAdapter().Adapt(response, MixedProjection());

        result.ItemAssessments["grp:tech"].Evidence.Should().HaveCount(2);
    }

    [Fact]
    public void MergeMissingOnly_PreservesDistinctBoundedHandlerDiagnostics()
    {
        using var firstJson = JsonDocument.Parse("""
            {"schemaVersion":"jd-stage2/v2","scores":[
              {"reqId":"grp:tech","handlerCode":"H_EXP_D04"}
            ]}
            """);
        using var secondJson = JsonDocument.Parse("""
            {"schemaVersion":"jd-stage2/v2","scores":[
              {"reqId":"grp:exp","handlerCode":"h_exp_d05"}
            ]}
            """);
        var adapter = new JdMatchingResponseAdapter();
        var projection = MixedProjection();
        var first = adapter.Adapt(firstJson, projection);
        var second = adapter.Adapt(secondJson, projection);

        var merged = adapter.MergeMissingOnly(
            first,
            second,
            new HashSet<string>(["grp:tech", "grp:exp"], StringComparer.Ordinal),
            new HashSet<string>(["grp:exp"], StringComparer.Ordinal));

        merged.Quality.Should().Be(JdStageTwoOutputQuality.COMPLETE);
        merged.HandlerDiagnostics.Should().HaveCount(2);
        merged.HandlerDiagnostics.Select(diagnostic => diagnostic.Code).Should().BeEquivalentTo(
            "HANDLER_CATEGORY_DIFFERENCE_ACCEPTED",
            "HANDLER_CODE_CASE_NORMALIZED");
        merged.WarningCodes.Should().NotContain("HANDLER_CATEGORY_DIFFERENCE_ACCEPTED");
        merged.WarningCodes.Should().NotContain("HANDLER_CODE_CASE_NORMALIZED");
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

    private static JdRequirementProjection SingleProjection() => new(
        "jd-analysis/v4",
        new[]
        {
            new ProjectedJdRequirementGroup(
                "grp",
                "all_of",
                1,
                "must_have",
                new[] { Item("grp:tech", "tech_skill", "React") })
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
