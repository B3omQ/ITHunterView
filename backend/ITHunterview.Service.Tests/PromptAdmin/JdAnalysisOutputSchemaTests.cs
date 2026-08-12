using System;
using System.Linq;
using FluentAssertions;
using ITHunterview.Service.Constant.Prompts;

namespace ITHunterview.Service.Tests.PromptAdmin;

public sealed class JdAnalysisOutputSchemaTests
{
    [Fact]
    public void Contracts_KeepPromptPairProviderAndEffectiveIdentifiersIndependent()
    {
        JdAnalysisPromptContract.CurrentPairContract.Should().Be("jd-analysis-prompt/v6");
        JdAnalysisOutputSchema.ProviderSchemaVersion.Should().Be("jd-analysis/v5");
        JdAnalysisEffectiveContract.SchemaVersion.Should().Be("jd-analysis-effective/v1");

        new[]
        {
            JdAnalysisPromptContract.CurrentPairContract,
            JdAnalysisOutputSchema.ProviderSchemaVersion,
            JdAnalysisEffectiveContract.SchemaVersion
        }.Distinct(StringComparer.Ordinal).Should().HaveCount(3);
    }

    [Fact]
    public void LockedBlock_DeclaresCompactV5ShapeAndExcludesDerivedFields()
    {
        JdAnalysisOutputSchema.LockedBlock.Should().Contain("\"schema_version\": \"jd-analysis/v5\"");
        JdAnalysisOutputSchema.LockedBlock.Should().Contain("\"source_requirement_id\"");
        JdAnalysisOutputSchema.LockedBlock.Should().Contain("\"intent\"");
        JdAnalysisOutputSchema.LockedBlock.Should().Contain("qualification");
        JdAnalysisOutputSchema.LockedBlock.Should().Contain("experience_duration");
        JdAnalysisOutputSchema.LockedBlock.Should().Contain("\"requirement_verbatim\"");
        JdAnalysisOutputSchema.LockedBlock.Should().Contain("at most 50 requirement groups");
        JdAnalysisOutputSchema.LockedBlock.Should().Contain("at most 100 total group items");
        JdAnalysisOutputSchema.LockedBlock.Should().NotContain("\"skills_normalized\": [");
        JdAnalysisOutputSchema.LockedBlock.Should().NotContain("\"requirements_list\": [");
        JdAnalysisOutputSchema.LockedBlock.Should().NotContain("\"confidence\"");
        JdAnalysisOutputSchema.LockedBlock.Should().NotContain("\"seniority_fit\"");
    }

    [Fact]
    public void ComposeSystemPrompt_AppendsExactlyOneLockedBlockAndIsIdempotent()
    {
        var once = JdAnalysisOutputSchema.ComposeSystemPrompt("Semantic rule");
        var twice = JdAnalysisOutputSchema.ComposeSystemPrompt(once);

        twice.Should().Be(once);
        Count(once, JdAnalysisOutputSchema.BeginMarker).Should().Be(1);
        Count(once, JdAnalysisOutputSchema.EndMarker).Should().Be(1);
    }

    [Fact]
    public void NormalizeManagedContent_PreservesSemanticRequirementGroupDiscussion()
    {
        const string semantic = "Use requirement_groups to preserve source order and explicit alternatives.";

        var result = JdAnalysisOutputSchema.NormalizeManagedContent(semantic);

        result.SemanticContent.Should().Be(semantic);
        result.RemovedKnownSchema.Should().BeFalse();
    }

    [Fact]
    public void NormalizeManagedContent_RemovesExactManagedBlock()
    {
        var input = $"Intro semantic rule.\n\n{JdAnalysisOutputSchema.LockedBlock}\n\nPreserve exact source clauses.";

        var result = JdAnalysisOutputSchema.NormalizeManagedContent(input);

        result.RemovedKnownSchema.Should().BeTrue();
        result.SemanticContent.Should().Be("Intro semantic rule.\n\nPreserve exact source clauses.");
    }

    [Fact]
    public void NormalizeManagedContent_RemovesKnownLegacyV4BlockAndKeepsEvidenceHeading()
    {
        const string input = """
            Intro semantic rule.

            OUTPUT CONTRACT

            Return only one valid JSON object.
            Use this exact compact structure:
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
                    "requirement_verbatim": "exact complete source clause supporting this group",
                    "items": [
                      {
                        "category": "tech_skill",
                        "skill_name": "normalized lowercase requirement name",
                        "raw_mention": "exact phrase from requirement_verbatim"
                      }
                    ]
                  }
                ]
              }
            }

            EVIDENCE AND SOURCE RULES

            Preserve exact source clauses.
            """;

        var result = JdAnalysisOutputSchema.NormalizeManagedContent(input);

        result.RemovedKnownSchema.Should().BeTrue();
        result.SemanticContent.Should().Contain("Intro semantic rule.");
        result.SemanticContent.Should().Contain("EVIDENCE AND SOURCE RULES");
        result.SemanticContent.Should().Contain("Preserve exact source clauses.");
        result.SemanticContent.Should().NotContain("\"schema_version\"");

        var normalizedAgain = JdAnalysisOutputSchema.NormalizeManagedContent(result.SemanticContent);
        normalizedAgain.RemovedKnownSchema.Should().BeFalse();
        normalizedAgain.SemanticContent.Should().Be(result.SemanticContent);
    }

    [Fact]
    public void NormalizeManagedContent_RejectsMutatedManagedSchema()
    {
        var mutated = JdAnalysisOutputSchema.LockedBlock.Replace(
            "\"source_requirement_id\"",
            "\"source_requirement_identifier\"",
            StringComparison.Ordinal);

        var action = () => JdAnalysisOutputSchema.NormalizeManagedContent(mutated);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*JD_ANALYSIS_PROMPT_SCHEMA_MUTATION*");
    }

    [Fact]
    public void NormalizeManagedContent_RejectsDuplicateManagedMarkers()
    {
        var duplicate = JdAnalysisOutputSchema.LockedBlock + Environment.NewLine + JdAnalysisOutputSchema.LockedBlock;

        var action = () => JdAnalysisOutputSchema.NormalizeManagedContent(duplicate);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*JD_ANALYSIS_PROMPT_SCHEMA_MUTATION*");
    }

    [Fact]
    public void NormalizeManagedContent_RejectsUnmarkedUnknownSchemaSignature()
    {
        const string mutated = "Rules. { \"schema_version\":\"jd-analysis/v99\", \"matching_metrics\": { \"requirement_groups\": [] } }";

        var action = () => JdAnalysisOutputSchema.NormalizeManagedContent(mutated);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*JD_ANALYSIS_PROMPT_SCHEMA_MUTATION*");
    }

    [Fact]
    public void NormalizeManagedContent_RejectsEmptySemanticContentAfterRemoval()
    {
        var action = () => JdAnalysisOutputSchema.NormalizeManagedContent(JdAnalysisOutputSchema.LockedBlock);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*JD_ANALYSIS_PROMPT_EMPTY_AFTER_SCHEMA*");
    }

    private static int Count(string value, string token) =>
        value.Split(token, StringSplitOptions.None).Length - 1;
}
