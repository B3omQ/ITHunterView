using FluentAssertions;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Service.Matching;

namespace ITHunterview.Service.Tests.Matching;

public sealed class LegacyJdStageTwoProjectionAdapterTests
{
    [Fact]
    public void Adapt_HomogeneousV3Group_PreservesCategoryAndGroupSemantics()
    {
        var projection = new JdRequirementProjection(
            "jd-analysis/v3",
            new[]
            {
                new ProjectedJdRequirementGroup(
                    "group-experience",
                    "one_of",
                    1,
                    "must_have",
                    new[]
                    {
                        Item("group-experience:item-001", "experience", "3 years backend experience", 3),
                        Item("group-experience:item-002", "experience", "2 years platform experience", 2)
                    })
            },
            false);

        var result = LegacyJdStageTwoProjectionAdapter.Adapt(projection);

        result.Should().ContainSingle();
        result[0].ReqId.Should().Be("group-experience");
        result[0].Category.Should().Be("experience");
        result[0].CategoryWeight.Should().Be(0.9m);
        result[0].Operator.Should().Be("one_of");
        result[0].MinSatisfied.Should().Be(1);
        result[0].NormalizedText.Should().Contain("3 years backend experience");
        result[0].NormalizedText.Should().Contain("2 years platform experience");
    }

    [Fact]
    public void Adapt_MixedCategoryGroup_FailsBeforeCallingStageTwoProvider()
    {
        var projection = new JdRequirementProjection(
            "jd-analysis/v3",
            new[]
            {
                new ProjectedJdRequirementGroup(
                    "mixed-group",
                    "one_of",
                    1,
                    "must_have",
                    new[]
                    {
                        Item("mixed-group:item-001", "tech_skill", "React", null),
                        Item("mixed-group:item-002", "experience", "3 years frontend experience", 3)
                    })
            },
            false);

        var action = () => LegacyJdStageTwoProjectionAdapter.Adapt(projection);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("MATCHING_LEGACY_CONTRACT_UNREPRESENTABLE");
    }

    [Fact]
    public void Classify_UnrepresentableLegacyGroup_IsNonRetryableConfigurationFailure()
    {
        var result = MatchingFailureClassifier.Classify(
            new InvalidOperationException("MATCHING_LEGACY_CONTRACT_UNREPRESENTABLE"));

        result.ErrorCode.Should().Be("MATCHING_CONFIGURATION_INVALID");
        result.Retryable.Should().BeFalse();
    }

    private static ProjectedJdRequirementItem Item(
        string itemId,
        string category,
        string skillName,
        int? minYears)
        => new(
            itemId,
            category,
            skillName,
            skillName,
            skillName,
            "requirements",
            new[] { skillName },
            minYears,
            null,
            JdRequirementCategoryWeights.Get(category));
}
