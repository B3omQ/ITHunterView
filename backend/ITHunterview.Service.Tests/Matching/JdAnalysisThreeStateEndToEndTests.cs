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

        var stageTwo = ValidateStageTwo(projection, 0.9m);
        var calculated = new JdFitScoreCalculator().Calculate(projection, stageTwo);
        using var details = JsonDocument.Parse(calculated.JsonString);
        Assert.Equal("COMPLETE", details.RootElement.GetProperty("jdAnalysis").GetProperty("quality").GetString());
        Assert.Equal("complete_requirement_set", details.RootElement.GetProperty("jdAnalysis").GetProperty("scoreBasis").GetString());
        Assert.Equal("COMPLETE", JdMatchMetadataReader.Read(calculated.JsonString)!.Quality);
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

        var response = ValidateStageTwo(projection, 0.8m);
        var calculated = new JdFitScoreCalculator().Calculate(projection, response);
        using var details = JsonDocument.Parse(calculated.JsonString);
        var metadata = details.RootElement.GetProperty("jdAnalysis");
        Assert.Equal("PARTIAL", metadata.GetProperty("quality").GetString());
        Assert.Equal("accepted_requirements_only", metadata.GetProperty("scoreBasis").GetString());
        Assert.False(metadata.GetProperty("requirementSetComplete").GetBoolean());
        Assert.Equal("PARTIAL", JdMatchMetadataReader.Read(calculated.JsonString)!.Quality);
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

    private static JdStageTwoValidatedResponse ValidateStageTwo(
        JdRequirementProjection projection,
        decimal score)
    {
        var itemScores = projection.Groups
            .SelectMany(group => group.Items.Select(item => (group, item)))
            .Select(value => new
            {
                itemId = value.item.ItemId,
                handlerCode = value.item.Category switch
                {
                    "tech_skill" => "H_TECH_01",
                    "experience" => "H_EXP_01",
                    "seniority_fit" => "H_SENIOR_01",
                    "domain_knowledge" => "H_DOMAIN_01",
                    "language" => "H_LANG_01",
                    "education" => "H_EDU_01",
                    _ => "H_SOFT_01"
                },
                handlerScore = score,
                reasoning = "Evidence supports the requirement.",
                confidence = "high",
                evidence = new[] { value.group.RequirementVerbatim }
            })
            .ToArray();
        var json = JsonSerializer.Serialize(new
        {
            itemScores,
            narrative = "The accepted requirements were scored.",
            improvements = Array.Empty<object>(),
            penalties = Array.Empty<object>()
        });

        using var document = JsonDocument.Parse(json);
        return new JdStageTwoResponseValidator().Validate(document, projection);
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
