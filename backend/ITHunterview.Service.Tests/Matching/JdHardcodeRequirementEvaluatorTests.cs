using ITHunterview.Service.Service.Matching;
using Xunit;

namespace ITHunterview.Service.Tests.Matching;

public class JdHardcodeRequirementEvaluatorTests
{
    [Fact]
    public void Evaluate_OneOfGroup_WithOneMatchingSkill_IsSatisfied()
    {
        var evaluator = new JdHardcodeRequirementEvaluator();
        var result = evaluator.Evaluate(new[]
        {
            new JdRequirementGroupData("grp-001", "one_of", 1, "must_have", new[] { "react", "angular", "vue" })
        }, new[] { "React" });

        Assert.Equal(1m, result.SkillScore);
        Assert.True(result.Outcomes.Single().Satisfied);
    }
}
