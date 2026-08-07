using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using ITHunterview.Service.Constant.Prompts;

namespace ITHunterview.Service.Tests.Matching;

public sealed class JdMatchingPromptGoldenMasterTests
{
    private const string ActivePromptSha256 = "91ef4362dc42d2be4424b6afb552cbfb143e020f4b54e54b6237e51ddb297a64";
    private const string FixtureName = "jd-matching-v2-active-prompt.txt";

    [Fact]
    public void ActiveV2Fixture_MatchesTheReviewedDatabaseSnapshot()
    {
        var prompt = ReadFixture();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(prompt))).ToLowerInvariant();

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
        composed.Should().Contain("\"scores\"");
        composed.Should().NotContain("\"itemScores\"");
    }

    [Fact]
    public void Compose_CurrentActiveV2_RemovesOnlyReviewedSchemaAndFooter()
    {
        var fixture = ReadFixture();

        var normalized = JdMatchingOutputSchema.NormalizeManagedContent(fixture);

        normalized.RemovedKnownSchema.Should().BeTrue();
        normalized.RemovedKnownFormatFooter.Should().BeTrue();
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
        var modified = composed.Replace("\"criticalGaps\"", "\"criticalGapsMutated\"", StringComparison.Ordinal);

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
        var fixture = ReadFixture();
        var startMarker = "SCHEMA OUTPUT BẮT BUỘC (Chỉ trả về JSON này, không có markdown block hay text thừa):";
        var endMarker = "HANDLER SCORING RULES (MANDATORY — follow exactly):";
        var fixtureStart = fixture.IndexOf(startMarker, StringComparison.Ordinal);
        var fixtureEnd = fixture.IndexOf(endMarker, fixtureStart, StringComparison.Ordinal);
        var lockedStart = JdMatchingOutputSchema.LockedBlock.IndexOf(startMarker, StringComparison.Ordinal);
        var lockedEnd = JdMatchingOutputSchema.LockedBlock.IndexOf("Chỉ trả về JSON hợp lệ.", lockedStart, StringComparison.Ordinal);

        fixtureStart.Should().BeGreaterThanOrEqualTo(0);
        fixtureEnd.Should().BeGreaterThan(fixtureStart);
        lockedStart.Should().BeGreaterThanOrEqualTo(0);
        lockedEnd.Should().BeGreaterThan(lockedStart);

        JdMatchingOutputSchema.LockedBlock[lockedStart..lockedEnd].Trim()
            .Should().Be(fixture[fixtureStart..fixtureEnd].Trim());
    }

    [Fact]
    public void LockedBlock_DoesNotContainItemScoresContract()
    {
        JdMatchingOutputSchema.LockedBlock.Should().NotContain("itemScores");
        JdMatchingOutputSchema.LockedBlock.Should().NotContain("itemId");
        JdMatchingOutputSchema.LockedBlock.Should().Contain("\"scores\"");
        JdMatchingOutputSchema.LockedBlock.Should().Contain("\"reqId\"");
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

    private static string ReadFixture() => File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Matching", "Fixtures", FixtureName));

    private static int Count(string value, string token) =>
        value.Split(token, StringSplitOptions.None).Length - 1;
}

internal static class JdMatchingPromptContractTestHelpers
{
    public static string ComposeSemanticOnly() => JdMatchingOutputSchema.Compose(
        "Semantic instructions.\n[CV_TEXT]\n[PARSED_JD_REQUIREMENTS]");
}
