using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Service.Matching;
using Xunit;

namespace ITHunterview.Service.Tests.Matching;

public class JdHardcodeRequirementEvaluatorTests
{
    [Fact]
    public void Evaluate_OneOfGroup_WithOneMatchingSkill_IsSatisfied()
    {
        var evaluator = new JdHardcodeRequirementEvaluator();
        var result = evaluator.Evaluate(Projection(new ProjectedJdRequirementGroup(
            "grp-001", "one_of", 1, "must_have", new[]
            {
                Item("grp-001:item-001", "react", "tech_skill"),
                Item("grp-001:item-002", "angular", "tech_skill"),
                Item("grp-001:item-003", "vue", "tech_skill")
            })), new[] { "React" });

        Assert.Equal(1m, result.SkillScore);
        Assert.True(result.Outcomes.Single().Satisfied);
        Assert.True(result.Outcomes.Single().EvaluatedBySkillComponent);
    }

    [Fact]
    public void Evaluate_NonTechnicalRequirement_DoesNotLowerSkillScore()
    {
        var evaluator = new JdHardcodeRequirementEvaluator();

        var result = evaluator.Evaluate(Projection(new ProjectedJdRequirementGroup(
            "grp-002", "all_of", 1, "must_have", new[]
            {
                Item("grp-002:item-001", "professional experience", "experience")
            })), Array.Empty<string>());

        Assert.Equal(0.5m, result.SkillScore);
        Assert.False(result.Outcomes.Single().EvaluatedBySkillComponent);
        Assert.Equal("non_technical_group", result.Outcomes.Single().NotEvaluatedReason);
    }

    private static JdRequirementProjection Projection(params ProjectedJdRequirementGroup[] groups) =>
        new("jd-analysis/v3", groups, false);

    private static ProjectedJdRequirementItem Item(string itemId, string skillName, string category) =>
        new(itemId, category, skillName, string.Empty, string.Empty, "requirements", Array.Empty<string>(), null, null, JdRequirementCategoryWeights.Get(category));
}
