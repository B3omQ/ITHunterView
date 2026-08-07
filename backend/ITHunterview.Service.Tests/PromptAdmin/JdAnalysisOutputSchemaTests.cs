using System;
using FluentAssertions;
using ITHunterview.Service.Constant.Prompts;

namespace ITHunterview.Service.Tests.PromptAdmin;

public sealed class JdAnalysisOutputSchemaTests
{
    [Fact]
    public void LockedBlock_DeclaresCurrentV4ShapeAndRequiredVerbatim()
    {
        JdAnalysisPromptContract.CurrentContract.Should().Be("jd-analysis/v4");
        JdAnalysisOutputSchema.LockedBlock.Should().Contain("\"schema_version\": \"jd-analysis/v4\"");
        JdAnalysisOutputSchema.LockedBlock.Should().Contain("\"matching_metrics\"");
        JdAnalysisOutputSchema.LockedBlock.Should().Contain("\"requirement_groups\"");
        JdAnalysisOutputSchema.LockedBlock.Should().Contain("\"requirement_verbatim\"");
        JdAnalysisOutputSchema.LockedBlock.Should().Contain("\"items\"");
        JdAnalysisOutputSchema.LockedBlock.Should().NotContain("\"skills_normalized\": [");
        JdAnalysisOutputSchema.LockedBlock.Should().NotContain("\"requirements_list\": [");
    }

    [Fact]
    public void ComposeSystemPrompt_AppendsExactlyOnceAndIsIdempotent()
    {
        var once = JdAnalysisOutputSchema.ComposeSystemPrompt("Semantic rule");
        var twice = JdAnalysisOutputSchema.ComposeSystemPrompt(once);

        twice.Should().Be(once);
        Count(once, JdAnalysisOutputSchema.BeginMarker).Should().Be(1);
        Count(once, JdAnalysisOutputSchema.EndMarker).Should().Be(1);
    }

    [Fact]
    public void RemoveEmbeddedSchemaBlock_PreservesSemanticSectionsAroundLegacyBlock()
    {
        const string input = """
            Intro semantic rule.

            OUTPUT CONTRACT

            Use this exact compact structure:
            { "schema_version": "jd-analysis/v4" }

            EVIDENCE AND SOURCE RULES

            Preserve exact source clauses.
            """;

        var result = JdAnalysisOutputSchema.RemoveEmbeddedSchemaBlock(input);

        result.Should().Contain("Intro semantic rule.");
        result.Should().Contain("EVIDENCE AND SOURCE RULES");
        result.Should().Contain("Preserve exact source clauses.");
        result.Should().NotContain("Use this exact compact structure");
        result.Should().NotContain("{ \"schema_version\"");
    }

    [Fact]
    public void RemoveEmbeddedSchemaBlock_DoesNotRemoveSemanticDiscussionOfRequirementGroups()
    {
        const string semantic = "requirement_groups is the only requirement representation. Preserve each explicit requirement.";

        JdAnalysisOutputSchema.RemoveEmbeddedSchemaBlock(semantic).Should().Be(semantic);
    }

    private static int Count(string value, string token) =>
        value.Split(token, StringSplitOptions.None).Length - 1;
}
