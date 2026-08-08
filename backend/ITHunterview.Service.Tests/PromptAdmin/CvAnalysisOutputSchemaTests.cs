using System;
using FluentAssertions;
using ITHunterview.Service.Constant.Prompts;

namespace ITHunterview.Service.Tests.PromptAdmin;

public sealed class CvAnalysisOutputSchemaTests
{
    [Fact]
    public void LockedBlock_PreservesCurrentV2Shape()
    {
        CvAnalysisOutputSchema.SchemaVersion.Should().Be("cv-analysis/v2");
        CvAnalysisOutputSchema.LockedBlock.Should().Contain("\"verbatim_sections\"");
        CvAnalysisOutputSchema.LockedBlock.Should().Contain("\"matching_metrics\"");
        CvAnalysisOutputSchema.LockedBlock.Should().Contain("\"matching_evidence\"");
        CvAnalysisOutputSchema.LockedBlock.Should().Contain("\"skills_normalized\"");
        CvAnalysisOutputSchema.LockedBlock.Should().Contain("\"requirement_signals\"");
        CvAnalysisOutputSchema.LockedBlock.Should().Contain("\"experience_summary\"");
        CvAnalysisOutputSchema.LockedBlock.Should().Contain("\"seniority_signals\"");
    }

    [Fact]
    public void ComposeSystemPrompt_AppendsExactlyOneLockedBlockAndIsIdempotent()
    {
        var once = CvAnalysisOutputSchema.ComposeSystemPrompt("Semantic CV rules");
        var twice = CvAnalysisOutputSchema.ComposeSystemPrompt(once);

        twice.Should().Be(once);
        Count(once, CvAnalysisOutputSchema.BeginMarker).Should().Be(1);
        Count(once, CvAnalysisOutputSchema.EndMarker).Should().Be(1);
    }

    [Fact]
    public void NormalizeManagedContent_PreservesSemanticMentionsOfMatchingMetrics()
    {
        const string content = "Keep matching_metrics concise and evidence-grounded.";

        CvAnalysisOutputSchema.NormalizeManagedContent(content).SemanticContent.Should().Be(content);
    }

    [Fact]
    public void NormalizeManagedContent_RejectsMutatedManagedSchema()
    {
        var mutated = CvAnalysisOutputSchema.LockedBlock.Replace(
            "\"matching_evidence\"",
            "\"matching_evidence_changed\"",
            StringComparison.Ordinal);

        var action = () => CvAnalysisOutputSchema.NormalizeManagedContent(mutated);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*CV_ANALYSIS_PROMPT_SCHEMA_MUTATION*");
    }

    [Fact]
    public void NormalizeManagedContent_RemovesCurrentUnmarkedV2BlockAndPreservesFollowingRules()
    {
        var schema = ExtractJson(CvAnalysisOutputSchema.LockedBlock);
        var input = $"Intro semantic rule.\n\nOUTPUT SCHEMA\n\n{schema}\n\nVERBATIM SECTION RULES\n\nPreserve evidence.";

        var result = CvAnalysisOutputSchema.NormalizeManagedContent(input);

        result.RemovedKnownSchema.Should().BeTrue();
        result.SemanticContent.Should().Contain("Intro semantic rule.");
        result.SemanticContent.Should().Contain("VERBATIM SECTION RULES");
        result.SemanticContent.Should().Contain("Preserve evidence.");
        result.SemanticContent.Should().NotContain("\"schema_version\"");
    }

    [Fact]
    public void NormalizeManagedContent_RemovesHistoricalV1BlockAndPreservesFollowingInstructions()
    {
        var input = $"Intro semantic rule.\n\n{HistoricalV1SchemaSentence}\n{HistoricalV1Schema}\n\nIf any information is missing, provide an empty array [] or empty string \"\".\nEnsure the output is 100% valid JSON.";

        var result = CvAnalysisOutputSchema.NormalizeManagedContent(input);

        result.RemovedKnownSchema.Should().BeTrue();
        result.SemanticContent.Should().Contain("Intro semantic rule.");
        result.SemanticContent.Should().Contain("If any information is missing");
        result.SemanticContent.Should().Contain("Ensure the output is 100% valid JSON.");
        result.SemanticContent.Should().NotContain("two main branches");
        result.SemanticContent.Should().NotContain("\"professional_experience_and_projects\"");
    }

    [Fact]
    public void NormalizeManagedContent_RejectsDuplicateManagedMarkers()
    {
        var duplicate = CvAnalysisOutputSchema.LockedBlock + Environment.NewLine + CvAnalysisOutputSchema.LockedBlock;

        var action = () => CvAnalysisOutputSchema.NormalizeManagedContent(duplicate);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*CV_ANALYSIS_PROMPT_SCHEMA_MUTATION*");
    }

    [Fact]
    public void NormalizeManagedContent_RejectsUnmarkedUnknownSchemaSignature()
    {
        const string mutated = "Rules. { \"verbatim_sections\": {}, \"matching_metrics\": {}, \"schema_version\": \"cv-analysis/v9\" }";

        var action = () => CvAnalysisOutputSchema.NormalizeManagedContent(mutated);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*CV_ANALYSIS_PROMPT_SCHEMA_MUTATION*");
    }

    [Fact]
    public void NormalizeManagedContent_RejectsEmptySemanticContentAfterKnownSchemaRemoval()
    {
        var input = "OUTPUT SCHEMA\n\n" + ExtractJson(CvAnalysisOutputSchema.LockedBlock) + "\n\nVERBATIM SECTION RULES";

        var action = () => CvAnalysisOutputSchema.NormalizeManagedContent(input);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NormalizeManagedContent_AcceptsCrLfAndCommonMigrationIndentation()
    {
        var schema = ExtractJson(CvAnalysisOutputSchema.LockedBlock)
            .Replace("\n", "\r\n", StringComparison.Ordinal);
        var input = "  Intro semantic rule.\r\n\r\n  OUTPUT SCHEMA\r\n\r\n" +
                    Indent(schema, "    ") +
                    "\r\n\r\n  VERBATIM SECTION RULES\r\n\r\n  Keep this rule.";

        var result = CvAnalysisOutputSchema.NormalizeManagedContent(input);

        result.SemanticContent.Should().Contain("Intro semantic rule.");
        result.SemanticContent.Should().Contain("VERBATIM SECTION RULES");
        result.SemanticContent.Should().Contain("Keep this rule.");
        result.RemovedKnownSchema.Should().BeTrue();
    }

    private static string ExtractJson(string lockedBlock)
    {
        var start = lockedBlock.IndexOf('{');
        var end = lockedBlock.LastIndexOf('}');
        return lockedBlock[start..(end + 1)];
    }

    private static string Indent(string value, string prefix) =>
        string.Join("\r\n", value.Split('\n').Select(line => prefix + line.TrimEnd('\r')));

    private static int Count(string value, string token) =>
        value.Split(token, StringSplitOptions.None).Length - 1;

    private const string HistoricalV1SchemaSentence =
        "The JSON MUST have the exact following schema with two main branches (`verbatim_sections` and `matching_metrics`):";

    private const string HistoricalV1Schema = """
        {
          "verbatim_sections": {
            "personal_info": {
              "name": "",
              "title": "",
              "summary": ""
            },
            "education": [
              {
                "institution": "",
                "degree": "",
                "major": "",
                "timeline": ""
              }
            ],
            "languages": [
              {
                "language": "",
                "certifications_or_level": ""
              }
            ],
            "skills_section": [
              "A list of skills that are ONLY listed in a standalone 'Skills' section. Do not include skills that only appear in project descriptions."
            ],
            "professional_experience_and_projects": [
              {
                "company_or_project_name": "",
                "role": "",
                "timeline": "",
                "details_and_responsibilities": [
                  "Copy verbatim bullet point 1",
                  "Copy verbatim bullet point 2"
                ],
                "technologies_used": ["List of technologies explicitly mentioned within this specific project/role"]
              }
            ],
            "certifications_and_awards": [
              "Award 1", "Cert 2"
            ],
            "other_information": "Any leftover text that doesn't fit above"
          },
          "matching_metrics": {
            "job_titles_normalized": ["Primary job title 1", "Job title 2"],
            "skills_normalized": ["Skill 1", "Skill 2", "Tool 3"],
            "total_years_exp": 0,
            "domains": ["Finance", "E-commerce"]
          }
        }
        """;
}
