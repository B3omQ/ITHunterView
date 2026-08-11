using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using ITHunterview.Service.Constant.Prompts;

namespace ITHunterview.Service.Tests.Matching;

public sealed class JdMatchingPromptGoldenMasterTests
{
    private const string ActivePromptSha256 = "91ef4362dc42d2be4424b6afb552cbfb143e020f4b54e54b6237e51ddb297a64";
    private const string FixtureName = "jd-matching-v2-active-prompt.txt";
    private const string V3SemanticFixtureName = "jd-matching-v3-semantic.txt";
    private const string V301SemanticFixtureName = "jd-matching-v3.0.1-semantic.txt";
    private const string OutputSchemaFixtureName = "jd-stage2-v2-output-schema.txt";

    [Fact]
    public void V3SemanticFixture_IsReviewedSchemaFreeAndCoversTheApprovedPolicy()
    {
        var prompt = ReadFixture(V3SemanticFixtureName);

        Hash(prompt).Should().Be("335956b4c7c875fd796be4f4c1b2f3e6ec934648bec7a84abee6a8ddfa8cc00c");
        prompt.Should().NotContain("\r");
        Count(prompt, JdMatchingPromptContract.CvPlaceholder).Should().Be(1);
        Count(prompt, JdMatchingPromptContract.RequirementsPlaceholder).Should().Be(1);
        prompt.Should().Contain("Every textual field in the response must be written in English");
        prompt.Should().Contain("Evaluate every supplied requirement item independently");
        prompt.Should().Contain("Return the exact reqId");
        prompt.Should().Contain("include a short exact quotation and its CV section");
        prompt.Should().Contain("Never fabricate or paraphrase a quotation");

        using var policy = JsonDocument.Parse(ReadFixture("matching-score-policy-v1.json"));
        foreach (var handler in policy.RootElement.GetProperty("handlers").EnumerateArray())
        {
            var handlerCode = handler.GetProperty("handlerCode").GetString()!;
            Count(prompt, handlerCode).Should().Be(1, $"handler {handlerCode} must have one unambiguous rule");
        }

        prompt.Should().NotContain(JdMatchingOutputSchema.BeginMarker);
        prompt.Should().NotContain("schemaVersion");
        prompt.Should().NotContain("handlerScore");
        prompt.Should().NotContain("confidence");
        prompt.Should().NotContain("seniority_fit");
        prompt.Should().NotContain("Pool A");
        prompt.Should().NotContain("Kill-Switch");
        prompt.Should().NotContain("penalties");
        prompt.Should().NotContain("improvements");
    }

    [Fact]
    public void V301SemanticFixture_ContainsOnlyScoreBearingHandlersAndNoLockedSchema()
    {
        var prompt = ReadFixture(V301SemanticFixtureName);

        using var policy = JsonDocument.Parse(ReadFixture("matching-score-policy-v1.json"));
        foreach (var handler in policy.RootElement.GetProperty("handlers").EnumerateArray())
        {
            var handlerCode = handler.GetProperty("handlerCode").GetString()!;
            if (handler.GetProperty("score").ValueKind == JsonValueKind.Null)
            {
                Count(prompt, handlerCode).Should().Be(0);
            }
            else
            {
                Count(prompt, handlerCode).Should().Be(1);
            }
        }

        prompt.Should().Contain(
            "The supplied requirements are already applicable JD items. Never return\n" +
            "NOT_APPLICABLE or EXCLUDED handler codes. If the CV has no supporting evidence,\n" +
            "return the appropriate score-bearing NO_EVIDENCE handler for the item instead.");
        prompt.Should().NotContain(JdMatchingOutputSchema.BeginMarker);
        prompt.Should().NotContain("schemaVersion");
    }

    [Fact]
    public void V301SemanticFixture_DiffFromV300IsExactlyTheReviewedHandlerConsistencyChange()
    {
        var historical = ReadFixture(V3SemanticFixtureName);
        var actual = ReadFixture(V301SemanticFixtureName);
        var expected = historical
            .Replace(
                "[H_EXP_NOT_APPLICABLE] category experience\n" +
                "- H_EXP_00 NOT_APPLICABLE: Use only when the supplied item does not actually require duration, hands-on experience, prior responsibility, or professional context.\n\n",
                string.Empty,
                StringComparison.Ordinal)
            .Replace(
                "- H_EDU_00 NOT_APPLICABLE: Use only when the supplied item does not actually require a degree, study status, or major.\n",
                string.Empty,
                StringComparison.Ordinal)
            .Replace(
                "[H_LANG_NOT_APPLICABLE] category language\n" +
                "- H_LANG_00 NOT_APPLICABLE: Use only when the supplied item does not actually contain a language requirement.\n\n",
                string.Empty,
                StringComparison.Ordinal)
            .Replace(
                "3. Select exactly one approved handlerCode for the item's supplied category. Do not return a numeric score; the application maps handlerCode to its fixed score.\n",
                "3. Select exactly one approved handlerCode for the item's supplied category. Do not return a numeric score; the application maps handlerCode to its fixed score.\n" +
                "The supplied requirements are already applicable JD items. Never return\n" +
                "NOT_APPLICABLE or EXCLUDED handler codes. If the CV has no supporting evidence,\n" +
                "return the appropriate score-bearing NO_EVIDENCE handler for the item instead.\n",
                StringComparison.Ordinal);

        actual.Should().Be(expected);
        JdMatchingOutputSchema.Compose(actual).Split(JdMatchingOutputSchema.BeginMarker).Should().HaveCount(2);
    }

    [Fact]
    public void ActiveV2Fixture_MatchesTheReviewedDatabaseSnapshot()
    {
        var prompt = ReadFixture();
        var hash = Hash(prompt);

        hash.Should().Be(ActivePromptSha256);
        prompt.Should().Contain("[CV_TEXT]");
        prompt.Should().Contain("[PARSED_JD_REQUIREMENTS]");
        prompt.Should().Contain("\"scores\"");
        prompt.Should().Contain("\"criticalGaps\"");
        prompt.Should().Contain("\"penalties\"");
        prompt.Should().Contain("\"narrative\"");
        prompt.Should().Contain("\"improvements\"");
        prompt.Should().NotContain("\"itemScores\"");
        prompt.Should().NotContain("\"itemId\"");
    }

    [Fact]
    public void MatchingSchemaComposer_IsAvailableAndAppendsExactlyOneLockedBlock()
    {
        var type = Type.GetType(
            "ITHunterview.Service.Constant.Prompts.JdMatchingOutputSchema, ITHunterview.Service");
        type.Should().NotBeNull();

        var compose = type!.GetMethod("Compose", BindingFlags.Public | BindingFlags.Static);
        compose.Should().NotBeNull();

        var composed = compose!.Invoke(null, new object[] { "Semantic instructions." }) as string;
        composed.Should().NotBeNullOrWhiteSpace();
        Count(composed!, "--- BEGIN LOCKED JD MATCHING OUTPUT SCHEMA ---").Should().Be(1);
        Count(composed, "--- END LOCKED JD MATCHING OUTPUT SCHEMA ---").Should().Be(1);
        composed.Should().Contain("\"schemaVersion\": \"jd-stage2/v2\"");
        composed.Should().Contain("\"scores\"");
        composed.Should().Contain("\"handlerCode\"");
        composed.Should().Contain("\"evidence\"");
        composed.Should().NotContain("\"itemScores\"");
        composed.Should().NotContain("\"handlerScore\"");
    }

    [Fact]
    public void Compose_CurrentActiveV2_RemovesOnlyReviewedSchemaAndFooter()
    {
        var fixture = ReadFixture();

        var normalized = JdMatchingOutputSchema.NormalizeManagedContent(fixture);

        normalized.RemovedKnownSchema.Should().BeTrue();
        normalized.RemovedKnownFormatFooter.Should().BeTrue();
        normalized.SemanticContent.Length.Should().Be(4309);
        Hash(normalized.SemanticContent).Should().Be(
            "78e8bc2565b85e39afcda0ae569ef55160f2b78f67cadb5de4eb937e71f4d6eb");
        JdMatchingOutputSchema.LockedBlock.Should().Be(ReadOutputSchemaFixture().Trim());
        normalized.SemanticContent.Should().NotContain("SCHEMA OUTPUT BẮT BUỘC");
        normalized.SemanticContent.Should().NotContain("Chỉ trả về JSON hợp lệ. Bắt đầu bằng { và kết thúc bằng }.");
        normalized.SemanticContent.Should().Contain("HANDLER SCORING RULES (MANDATORY — follow exactly):");
        normalized.SemanticContent.Should().Contain("[H_TECH]");
        normalized.SemanticContent.Should().Contain("[PARSED_JD_REQUIREMENTS]");
    }

    [Fact]
    public void Compose_CurrentActiveV2_PreservesEveryProtectedSemanticSection()
    {
        var composed = JdMatchingOutputSchema.Compose(ReadFixture());

        foreach (var section in new[]
                 {
                     "MỌI TRƯỜNG VĂN BẢN",
                     "NHIỆM VỤ CỦA BẠN:",
                     "HANDLER SCORING RULES (MANDATORY — follow exactly):",
                     "[H_TECH]",
                     "[H_EXP]",
                     "[H_SENIOR]",
                     "[H_EDU]",
                     "[H_LANG]",
                     "[H_SOFT]",
                     "[H_DOMAIN]",
                     "SOFT SKILL EVIDENCE TABLE",
                     "HARD CAPS & PENALTIES",
                     "KSW_01 (Kill-Switch)",
                     "Xếp loại (result)"
                 })
        {
            Count(composed, section).Should().Be(1, $"protected section '{section}' must be retained exactly once");
        }

        Count(composed, JdMatchingOutputSchema.BeginMarker).Should().Be(1);
        Count(composed, JdMatchingOutputSchema.EndMarker).Should().Be(1);
    }

    [Fact]
    public void ActiveV2_ExplanatoryPlaceholderReferenceDoesNotCreateASecondInputSlot()
    {
        var semantic = JdMatchingOutputSchema.NormalizeManagedContent(ReadFixture()).SemanticContent;

        JdMatchingPromptContract.FindOperationalPlaceholderIndex(
                semantic,
                JdMatchingPromptContract.CvPlaceholder)
            .Should().BeGreaterThanOrEqualTo(0);
        JdMatchingPromptContract.FindOperationalPlaceholderIndex(
                semantic,
                JdMatchingPromptContract.RequirementsPlaceholder)
            .Should().BeGreaterThanOrEqualTo(0);

        Count(semantic, JdMatchingPromptContract.RequirementsPlaceholder).Should().Be(2);
    }

    [Fact]
    public void Compose_IsIdempotent()
    {
        var once = JdMatchingOutputSchema.Compose(ReadFixture());

        var twice = JdMatchingOutputSchema.Compose(once);

        twice.Should().Be(once);
    }

    [Fact]
    public void Normalize_ModifiedEmbeddedSchema_RejectsMutation()
    {
        var modified = ReadFixture().Replace("\"criticalGaps\"", "\"criticalGapsMutated\"", StringComparison.Ordinal);

        var action = () => JdMatchingOutputSchema.NormalizeManagedContent(modified);

        action.Should().Throw<ArgumentException>()
            .Which.Message.Should().Contain("MATCHING_PROMPT_SCHEMA_MUTATION");
    }

    [Fact]
    public void Normalize_ModifiedLockedBlock_RejectsMutation()
    {
        var composed = JdMatchingPromptContractTestHelpers.ComposeSemanticOnly();
        var modified = composed.Replace("\"schemaVersion\"", "\"schemaVersionMutated\"", StringComparison.Ordinal);

        var action = () => JdMatchingOutputSchema.NormalizeManagedContent(modified);

        action.Should().Throw<ArgumentException>()
            .Which.Message.Should().Contain("MATCHING_PROMPT_SCHEMA_MUTATION");
    }

    [Fact]
    public void Normalize_SemanticMentionOfScores_DoesNotDeleteProse()
    {
        const string semantic = "Explain how scores are grounded in the CV evidence.\n\n[CV_TEXT]\n[PARSED_JD_REQUIREMENTS]";

        var normalized = JdMatchingOutputSchema.NormalizeManagedContent(semantic);

        normalized.SemanticContent.Should().Contain("scores are grounded");
        normalized.RemovedKnownSchema.Should().BeFalse();
        normalized.RemovedKnownFormatFooter.Should().BeFalse();
    }

    [Fact]
    public void LockedBlock_ExactlyMatchesExportedSchemaPayload()
    {
        JdMatchingOutputSchema.LockedBlock.Should().Be(ReadOutputSchemaFixture().Trim());
    }

    [Fact]
    public void LockedBlock_DoesNotContainItemScoresContract()
    {
        JdMatchingOutputSchema.LockedBlock.Should().NotContain("itemScores");
        JdMatchingOutputSchema.LockedBlock.Should().NotContain("itemId");
        JdMatchingOutputSchema.LockedBlock.Should().NotContain("handlerScore");
        JdMatchingOutputSchema.LockedBlock.Should().NotContain("confidence");
        JdMatchingOutputSchema.LockedBlock.Should().NotContain("criticalGaps");
        JdMatchingOutputSchema.LockedBlock.Should().NotContain("penalties");
        JdMatchingOutputSchema.LockedBlock.Should().NotContain("improvements");
        JdMatchingOutputSchema.LockedBlock.Should().Contain("\"scores\"");
        JdMatchingOutputSchema.LockedBlock.Should().Contain("\"reqId\"");
        JdMatchingOutputSchema.LockedBlock.Should().Contain("\"handlerCode\"");
        JdMatchingOutputSchema.LockedBlock.Should().Contain("\"quotation\"");
        JdMatchingOutputSchema.LockedBlock.Should().Contain("\"section\"");
    }

    [Fact]
    public void Normalize_ReviewedHistoricalLockedV2Block_RemainsSupported()
    {
        var historicalPrompt = ReadFixture();
        const string schemaStart = "SCHEMA OUTPUT BẮT BUỘC (Chỉ trả về JSON này, không có markdown block hay text thừa):";
        const string schemaEnd = "HANDLER SCORING RULES (MANDATORY — follow exactly):";
        const string footer = "Chỉ trả về JSON hợp lệ. Bắt đầu bằng { và kết thúc bằng }.";
        var start = historicalPrompt.IndexOf(schemaStart, StringComparison.Ordinal);
        var end = historicalPrompt.IndexOf(schemaEnd, start, StringComparison.Ordinal);
        var historicalSchema = historicalPrompt[start..end].Trim();
        var marked = $"Semantic instructions.\n[CV_TEXT]\n[PARSED_JD_REQUIREMENTS]\n\n{JdMatchingOutputSchema.BeginMarker}\n" +
                     "This output format is managed by the application. Return exactly one JSON object without Markdown, comments, headings, or surrounding text.\n\n" +
                     $"{historicalSchema}\n\n{footer}\n{JdMatchingOutputSchema.EndMarker}";

        var normalized = JdMatchingOutputSchema.NormalizeManagedContent(marked);

        normalized.RemovedKnownSchema.Should().BeTrue();
        normalized.SemanticContent.Should().Be("Semantic instructions.\n[CV_TEXT]\n[PARSED_JD_REQUIREMENTS]");
    }

    [Fact]
    public void Compose_NormalizesLineEndingsBeforeAppendingSchema()
    {
        var composed = JdMatchingOutputSchema.Compose(
            "Semantic instructions.\r\n[CV_TEXT]\r\n[PARSED_JD_REQUIREMENTS]");

        composed.Should().NotContain("\r");
        Count(composed, JdMatchingOutputSchema.BeginMarker).Should().Be(1);
    }

    [Fact]
    public void ProtectedSemanticSections_ArePresentInTheActivePrompt()
    {
        var prompt = ReadFixture();

        foreach (var section in new[]
                 {
                     "HANDLER SCORING RULES",
                     "[H_TECH]",
                     "[H_EXP]",
                     "[H_SENIOR]",
                     "[H_EDU]",
                     "[H_LANG]",
                     "[H_SOFT]",
                     "[H_DOMAIN]",
                     "SOFT SKILL EVIDENCE TABLE",
                     "HARD CAPS & PENALTIES",
                     "KSW_01 (Kill-Switch)",
                     "Xếp loại (result)"
                 })
        {
            Count(prompt, section).Should().Be(1, $"protected section '{section}' must be retained exactly once");
        }
    }

    private static string ReadFixture() => ReadFixture(FixtureName);

    private static string ReadFixture(string fixtureName) => File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Matching", "Fixtures", fixtureName));

    private static string ReadOutputSchemaFixture() => File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Matching", "Fixtures", OutputSchemaFixtureName))
        .Replace("\r\n", "\n", StringComparison.Ordinal);

    private static int Count(string value, string token) =>
        value.Split(token, StringSplitOptions.None).Length - 1;

    private static string Hash(string content) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)))
            .ToLowerInvariant();
}

internal static class JdMatchingPromptContractTestHelpers
{
    public static string ComposeSemanticOnly() => JdMatchingOutputSchema.Compose(
        "Semantic instructions.\n[CV_TEXT]\n[PARSED_JD_REQUIREMENTS]");
}
