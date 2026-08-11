using System.Text.Json;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.DTOs.JobAnalysis;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Service.Matching;
using Moq;

namespace ITHunterview.Service.Tests.Matching;

public sealed class RawJdFallbackMatchingServiceTests
{
    [Fact]
    public async Task ExecuteAsync_ValidMinimalOutput_ReturnsExplicitRawTextFallbackResult()
    {
        var ai = new Mock<IAiService>(MockBehavior.Strict);
        ai.Setup(service => service.GetActiveProviderNameAsync()).ReturnsAsync("Gemini");
        ai.Setup(service => service.GenerateTextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                "Gemini",
                It.IsAny<AiGenerationOptions>(),
                It.IsAny<CancellationToken>(),
                "CV_JD_MATCHING_FALLBACK"))
            .ReturnsAsync("""
                {"score":72.5,"narrative":"The CV covers the main role signals.","improvements":[{"priority":"medium","category":"experience","issue":"Limited evidence","action":"Add measurable outcomes."}]}
                """);
        var service = new RawJdFallbackMatchingService(ai.Object);

        var result = await service.ExecuteAsync(
            "{\"schema_version\":\"cv-analysis/v3\"}",
            "Build and maintain web applications.",
            "Software Engineer",
            new[] { new JdAnalysisDiagnostic("INVALID_JSON_FORMAT", "$") });

        Assert.Equal(72.5m, result.FinalScore);
        using var document = JsonDocument.Parse(result.JsonString);
        Assert.Equal(JdFitResultContract.RawTextFallbackVersion2, document.RootElement.GetProperty("contract").GetString());
        Assert.Equal("raw_text_fallback", document.RootElement.GetProperty("jdAnalysis").GetProperty("scoreBasis").GetString());
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("jdFit").GetProperty("poolA").GetProperty("score").ValueKind);
        Assert.Contains("RAW_TEXT_FALLBACK", document.RootElement.GetProperty("jdAnalysis").GetProperty("warningCodes").EnumerateArray().Select(item => item.GetString()));
        ai.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_OutputWithUnexpectedProperty_IgnoresItAndKeepsSafeScore()
    {
        var ai = new Mock<IAiService>(MockBehavior.Strict);
        ai.Setup(service => service.GetActiveProviderNameAsync()).ReturnsAsync("Gemini");
        ai.Setup(service => service.GenerateTextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                "Gemini",
                It.IsAny<AiGenerationOptions>(),
                It.IsAny<CancellationToken>(),
                "CV_JD_MATCHING_FALLBACK"))
            .ReturnsAsync("{\"score\":72,\"narrative\":\"Useful\",\"improvements\":[],\"requirementGroups\":[]}");
        var service = new RawJdFallbackMatchingService(ai.Object);

        var result = await service.ExecuteAsync("{\"cv\":true}", "JD", null, Array.Empty<JdAnalysisDiagnostic>());

        Assert.Equal(72m, result.FinalScore);
        Assert.Equal(MatchingCompletionDisposition.ScoredBillable, result.CompletionDisposition);
        ai.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_TwoMalformedOutputs_ReturnsTerminalUnscoredEnvelope()
    {
        var ai = new Mock<IAiService>(MockBehavior.Strict);
        ai.Setup(service => service.GetActiveProviderNameAsync()).ReturnsAsync("Gemini");
        ai.SetupSequence(service => service.GenerateTextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                "Gemini",
                It.IsAny<AiGenerationOptions>(),
                It.IsAny<CancellationToken>(),
                "CV_JD_MATCHING_FALLBACK"))
            .ReturnsAsync("{not-json")
            .ReturnsAsync("still not json");
        var service = new RawJdFallbackMatchingService(ai.Object);

        var result = await service.ExecuteAsync(
            "{\"cv\":true}",
            "JD",
            null,
            Array.Empty<JdAnalysisDiagnostic>());
        using var document = JsonDocument.Parse(result.JsonString);

        Assert.Null(result.FinalScore);
        Assert.Equal(MatchingCompletionDisposition.UnscoredRefundable, result.CompletionDisposition);
        Assert.Equal(JdFitResultContract.RawTextFallbackVersion2,
            document.RootElement.GetProperty("contract").GetString());
        Assert.False(document.RootElement.GetProperty("scoreAvailable").GetBoolean());
        ai.Verify(service => service.GenerateTextAsync(
            It.IsAny<string>(), It.IsAny<string>(), "Gemini", It.IsAny<AiGenerationOptions>(),
            It.IsAny<CancellationToken>(), "CV_JD_MATCHING_FALLBACK"), Times.Exactly(2));
    }
}
