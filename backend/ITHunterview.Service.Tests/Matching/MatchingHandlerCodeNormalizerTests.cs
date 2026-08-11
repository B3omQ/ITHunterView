using FluentAssertions;
using ITHunterview.Service.Service.Matching;

namespace ITHunterview.Service.Tests.Matching;

public sealed class MatchingHandlerCodeNormalizerTests
{
    [Theory]
    [InlineData("H_TECH_04", "H_TECH_04", "")]
    [InlineData("h_tech_04", "H_TECH_04", "HANDLER_CODE_CASE_NORMALIZED")]
    [InlineData("H_TECH_04_APPLIED_MATCH", "H_TECH_04", "HANDLER_CODE_DECORATION_NORMALIZED")]
    [InlineData("APPLIED_MATCH (H_TECH_04)", "H_TECH_04", "HANDLER_CODE_DECORATION_NORMALIZED")]
    public void TryNormalize_UniqueCanonicalCode_ReturnsBackendResolution(
        string input,
        string expectedCode,
        string expectedDiagnostic)
    {
        var accepted = MatchingHandlerCodeNormalizer.TryNormalize(
            input,
            out var resolution,
            out var diagnostic);

        accepted.Should().BeTrue();
        resolution.HandlerCode.Should().Be(expectedCode);
        diagnostic.Should().Be(expectedDiagnostic);
    }

    [Theory]
    [InlineData("")]
    [InlineData("APPLIED_MATCH")]
    [InlineData("H_TECH_04 or H_TECH_05")]
    public void TryNormalize_UnknownOrAmbiguousValue_DoesNotInfer(string input)
    {
        var accepted = MatchingHandlerCodeNormalizer.TryNormalize(
            input,
            out _,
            out var diagnostic);

        accepted.Should().BeFalse();
        diagnostic.Should().BeOneOf(
            "INVALID_HANDLER_CODE",
            "UNKNOWN_HANDLER_CODE",
            "AMBIGUOUS_HANDLER_CODE");
    }
}
