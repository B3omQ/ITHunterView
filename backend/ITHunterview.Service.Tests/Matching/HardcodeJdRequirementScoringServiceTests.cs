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
}
