using ITHunterview.Service.Service.Matching;

namespace ITHunterview.Service.Tests.Matching;

public class HardcodeJdRequirementScoringServiceTests
{
    [Fact]
    public void Evaluate_V3NonTechnicalGroup_DoesNotBecomeSkillRequirement()
    {
        var service = new HardcodeJdRequirementScoringService(
            new JdRequirementProjector(),
            new JdHardcodeRequirementEvaluator());

        var result = service.Evaluate(
            """
            {
              "schema_version": "jd-analysis/v3",
              "matching_metrics": {
                "requirement_groups": [
                  {
                    "group_id": "grp-exp",
                    "operator": "all_of",
                    "min_satisfied": 1,
                    "importance": "must_have",
                    "items": [
                      {
                        "category": "experience",
                        "skill_name": "3 years professional experience",
                        "detail_verbatim": "At least three years of experience",
                        "raw_mention": "three years",
                        "source_section": "requirements",
                        "evidence": ["At least three years of experience"],
                        "min_years": 3
                      }
                    ]
                  }
                ]
              }
            }
            """,
            Array.Empty<string>());

        Assert.True(result.HasRequirementGroups);
        Assert.Equal(0.5m, result.Evaluation!.SkillScore);
        Assert.False(result.Evaluation.Outcomes.Single().EvaluatedBySkillComponent);
    }

    [Fact]
    public void Evaluate_InvalidAnalysis_ReturnsCompatibilityFallbackWithoutThrowing()
    {
        var service = new HardcodeJdRequirementScoringService(
            new JdRequirementProjector(),
            new JdHardcodeRequirementEvaluator());

        var result = service.Evaluate("{not-json", new[] { "React" });

        Assert.False(result.HasRequirementGroups);
        Assert.Equal(JdRequirementProjector.InvalidEffectiveJdAnalysis, result.FailureCode);
        Assert.Null(result.Evaluation);
    }

    [Fact]
    public void Evaluate_InvalidV3Analysis_IsTerminalUnscoredInsteadOfFlatteningLostSemantics()
    {
        var service = new HardcodeJdRequirementScoringService(
            new JdRequirementProjector(),
            new JdHardcodeRequirementEvaluator());

        var result = service.Evaluate(
            """
            { "schema_version": "jd-analysis/v3", "matching_metrics": { "requirement_groups": "invalid" } }
            """,
            new[] { "React" });

        Assert.False(result.HasRequirementGroups);
        Assert.False(result.CanUseLegacyCompatibilityFallback);
        Assert.Equal(JdRequirementProjector.InvalidEffectiveJdAnalysis, result.FailureCode);
    }

    [Fact]
    public void Evaluate_EffectiveV1_ReceivesEveryTechnicalRequirement()
    {
        var service = new HardcodeJdRequirementScoringService(
            new JdRequirementProjector(),
            new JdHardcodeRequirementEvaluator());

        var result = service.Evaluate(
            """
            {"schema_version":"jd-analysis-effective/v1","analysis_quality":"COMPLETE","matching_metrics":{"requirement_groups":[{"group_id":"grp-001","source_requirement_id":"req-001","intent":"qualification","operator":"all_of","min_satisfied":2,"importance":"must_have","source_section":"requirements","requirement_verbatim":"Java and Spring Boot.","items":[{"item_id":"grp-001:item-001","category":"tech_skill","skill_name":"Java","raw_mention":"Java"},{"item_id":"grp-001:item-002","category":"tech_skill","skill_name":"Spring Boot","raw_mention":"Spring Boot"}]}]}}
            """,
            new[] { "Java", "Spring Boot" });

        Assert.True(result.HasRequirementGroups);
        var outcome = Assert.Single(result.Evaluation!.Outcomes);
        Assert.True(outcome.EvaluatedBySkillComponent);
        Assert.Equal(2, outcome.MatchedItems);
        Assert.Equal(1m, result.Evaluation.SkillScore);
    }

    [Fact]
    public void Evaluate_InvalidEffectiveV1_IsTerminalUnscoredInsteadOfFlatteningLostSemantics()
    {
        var service = new HardcodeJdRequirementScoringService(
            new JdRequirementProjector(),
            new JdHardcodeRequirementEvaluator());

        var result = service.Evaluate(
            """{"schema_version":"jd-analysis-effective/v1","matching_metrics":{"requirement_groups":"invalid"}}""",
            new[] { "Java" });

        Assert.False(result.CanUseLegacyCompatibilityFallback);
        Assert.Equal(JdRequirementProjector.InvalidEffectiveJdAnalysis, result.FailureCode);
    }
}
