using System.Text.Json;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.DTOs.JobAnalysis;
using ITHunterview.Service.Exceptions;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Service;
using ITHunterview.Service.Service.Matching;
using ITHunterview.Service.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ITHunterview.Service.Tests.Matching;

/// <summary>
/// A bounded, provider-free pipeline test. It follows the same data shape as
/// the runtime: provider JSON -> validator -> effective JSON -> projector ->
/// hardcode/Stage 2 adapters -> common calculator.
/// </summary>
public sealed class JdAnalysisThreeStateEndToEndTests
{
    private const string ReactClause = "Proficient in ReactJS.";

    [Fact]
    public void CompleteV4_ReachesHardcodeAndStageTwoWithCompleteMetadata()
    {
        var input = new JobAnalysisInputSnapshot { Requirements = ReactClause };
        var validation = new JdAnalysisResponseValidator().Validate(CompleteV4(), input);

        Assert.True(validation.IsValid, validation.FailureCode);
        Assert.Equal(JdAnalysisQuality.COMPLETE, validation.Quality);

        var effective = Serialize(validation.Data!);
        var projection = new JdRequirementProjector().Project(effective);
        Assert.Equal("COMPLETE", projection.AnalysisQuality);
        Assert.True(projection.RequirementSetComplete);

        var hardcode = new HardcodeJdRequirementScoringService(
            new JdRequirementProjector(),
            new JdHardcodeRequirementEvaluator())
            .Evaluate(effective, new[] { "react" });
        Assert.True(hardcode.HasRequirementGroups);
        Assert.Equal("COMPLETE", hardcode.AnalysisQuality);
        Assert.NotNull(hardcode.Evaluation);

        var stageTwo = ValidateStageTwo(projection);
        var calculated = CalculateAndSerialize(projection, stageTwo);
        using var details = JsonDocument.Parse(calculated.JsonString);
        Assert.Equal(JdFitResultContract.Version5, details.RootElement.GetProperty("contract").GetString());
        Assert.Equal("jd-analysis-effective/v1", details.RootElement.GetProperty("sourceJdSchemaVersion").GetString());
        Assert.Equal(1, details.RootElement.GetProperty("analysis").GetProperty("acceptedCount").GetInt32());
    }

    [Fact]
    public void PartialV4_DiscardsOnlyBadGroupAndStillDeliversScoredResult()
    {
        var input = new JobAnalysisInputSnapshot { Requirements = ReactClause };
        var validation = new JdAnalysisResponseValidator().Validate(PartialV4(), input);

        Assert.False(validation.IsValid);
        Assert.True(validation.IsUsable);
        Assert.Equal(JdAnalysisQuality.PARTIAL, validation.Quality);
        Assert.Single(validation.Data!.RequirementGroups);
        Assert.False(validation.Data.Coverage.RequirementSetComplete);

        var effective = Serialize(validation.Data);
        var projection = new JdRequirementProjector().Project(effective);
        Assert.Equal("PARTIAL", projection.AnalysisQuality);
        Assert.False(projection.RequirementSetComplete);
        Assert.Contains("INVALID_REQUIREMENT_GROUP", projection.WarningCodes!);

        var response = ValidateStageTwo(projection);
        var calculated = CalculateAndSerialize(projection, response);
        using var details = JsonDocument.Parse(calculated.JsonString);
        Assert.Equal(JdFitResultContract.Version5, details.RootElement.GetProperty("contract").GetString());
        Assert.Equal(1, details.RootElement.GetProperty("analysis").GetProperty("acceptedCount").GetInt32());
        Assert.Single(details.RootElement.GetProperty("jdFit").GetProperty("requirementGroups").EnumerateArray());
    }

    [Fact]
    public void PartialV5_InvalidSiblingDropsWholeGroupBeforeEffectiveProjection()
    {
        const string json = """
            {"schema_version":"jd-analysis/v5","matching_metrics":{"job_titles_normalized":[],"total_years_exp":0,"domains":[],"requirement_groups":[
              {"source_requirement_id":"req-001","intent":"qualification","operator":"all_of","importance":"must_have","source_section":"requirements","requirement_verbatim":"Java and Spring","items":[{"category":"tech_skill","skill_name":"Java","raw_mention":"Java"},{"category":"invalid","skill_name":"Spring","raw_mention":"Spring"}]},
              {"source_requirement_id":"req-002","intent":"qualification","operator":"all_of","importance":"must_have","source_section":"requirements","requirement_verbatim":"React required","items":[{"category":"tech_skill","skill_name":"React","raw_mention":"React"}]}
            ]}}
            """;
        var validation = new JdAnalysisResponseValidator().Validate(json, new JobAnalysisInputSnapshot());
        Assert.True(validation.IsUsable);
        Assert.Equal(JdAnalysisQuality.PARTIAL, validation.Quality);
        Assert.Equal("req-002", Assert.Single(validation.Data!.RequirementGroups).SourceRequirementId);

        var projection = new JdRequirementProjector().Project(Serialize(validation.Data));
        var projectedGroup = Assert.Single(projection.Groups);
        Assert.Equal("React", Assert.Single(projectedGroup.Items).SkillName);
        Assert.DoesNotContain(
            projection.Groups.SelectMany(group => group.Items),
            item => item.SkillName is "Java" or "Spring");
        Assert.Equal("PARTIAL", projection.AnalysisQuality);
        Assert.False(projection.RequirementSetComplete);
    }

    [Fact]
    public void InvalidOutput_IsNotUsableAndMapsToNonRetryableAiFailure()
    {
        var validation = new JdAnalysisResponseValidator().Validate(
            "{not-json",
            new JobAnalysisInputSnapshot { Requirements = ReactClause });

        Assert.Equal(JdAnalysisQuality.INVALID, validation.Quality);
        Assert.False(validation.IsUsable);

        var exception = new JdAnalysisValidationException(validation);
        var classification = MatchingFailureClassifier.Classify(exception);
        Assert.Equal("AI_OUTPUT_INVALID", classification.ErrorCode);
        Assert.False(classification.Retryable);
        Assert.Equal(JdAnalysisQuality.INVALID, classification.JdAnalysisQuality);
    }

    private static JdStageTwoValidatedResponse ValidateStageTwo(JdRequirementProjection projection)
    {
        var scores = projection.Groups
            .SelectMany(group => group.Items.Select(item => (group, item)))
            .Select(value => new
            {
                reqId = value.item.ItemId,
                handlerCode = value.item.Category switch
                {
                    "tech_skill" => "H_TECH_05",
                    "experience" => "H_EXP_D05",
                    "domain_knowledge" => "H_DOMAIN_05",
                    "language" => "H_LANG_Q05",
                    "education" => "H_EDU_06",
                    _ => "H_SOFT_01"
                },
                reasoning = "Evidence supports the requirement.",
                evidence = new[] { new { quotation = value.group.RequirementVerbatim, section = "experience" } }
            })
            .ToArray();
        var json = JsonSerializer.Serialize(new
        {
            schemaVersion = "jd-stage2/v2",
            scores,
            narrative = "The accepted requirements were scored."
        });

        using var document = JsonDocument.Parse(json);
        return new JdMatchingResponseAdapter().Adapt(document, projection);
    }

    private static JdFitScoreCalculation CalculateAndSerialize(
        JdRequirementProjection projection,
        JdStageTwoValidatedResponse response)
    {
        var scoreResult = new JdFitScoreCalculator().Calculate(projection, response);
        var gaps = new JdCriticalGapEvaluator().Evaluate(projection, response.ItemAssessments);
        return new JdFitResultSerializer().Serialize(
            projection,
            response,
            scoreResult,
            gaps,
            new JdFitSerializationContext(Guid.Empty, "test", "semantic-hash", "schema-hash", 1));
    }

    private static string Serialize(ValidatedJobAnalysis analysis)
    {
        var service = new JobAnalysisExtractionService(
            Mock.Of<IAiService>(),
            Mock.Of<IPromptManagementService>(),
            Mock.Of<IJdAnalysisResponseValidator>(),
            NullLogger<JobAnalysisExtractionService>.Instance);
        return service.SerializeEffectiveAnalysis(analysis);
    }

    private static string CompleteV4() => $$"""
        {
          "schema_version": "jd-analysis/v4",
          "matching_metrics": {
            "job_titles_normalized": [],
            "total_years_exp": 0,
            "domains": [],
            "requirement_groups": [
              {
                "operator": "all_of",
                "importance": "must_have",
                "source_section": "requirements",
                "requirement_verbatim": "{{ReactClause}}",
                "items": [
                  { "category": "tech_skill", "skill_name": "react", "raw_mention": "ReactJS" }
                ]
              }
            ]
          }
        }
        """;

    private static string PartialV4() => $$"""
        {
          "schema_version": "jd-analysis/v4",
          "matching_metrics": {
            "job_titles_normalized": "invalid-auxiliary-value",
            "domains": [],
            "requirement_groups": [
              {
                "operator": "all_of",
                "importance": "must_have",
                "source_section": "requirements",
                "requirement_verbatim": "{{ReactClause}}",
                "items": [
                  { "category": "tech_skill", "skill_name": "react", "raw_mention": "ReactJS" }
                ]
              },
              {
                "operator": "all_of",
                "importance": "must_have",
                "source_section": "requirements",
                "requirement_verbatim": "A malformed group",
                "items": []
              }
            ]
          }
        }
        """;
}
