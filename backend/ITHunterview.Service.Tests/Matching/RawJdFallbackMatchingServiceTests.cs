using System.Text.Json;
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
                It.IsAny<CancellationToken>()))
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
        Assert.Equal(JdFitResultContract.RawTextFallback, document.RootElement.GetProperty("contract").GetString());
        Assert.Equal("raw_text_fallback", document.RootElement.GetProperty("jdAnalysis").GetProperty("scoreBasis").GetString());
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("jdFit").GetProperty("poolA").GetProperty("score").ValueKind);
        Assert.Contains("RAW_TEXT_FALLBACK", document.RootElement.GetProperty("jdAnalysis").GetProperty("warningCodes").EnumerateArray().Select(item => item.GetString()));
        ai.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_OutputWithUnexpectedProperty_FailsInsteadOfInventingStructuredSemantics()
    {
        var ai = new Mock<IAiService>(MockBehavior.Strict);
        ai.Setup(service => service.GetActiveProviderNameAsync()).ReturnsAsync("Gemini");
        ai.Setup(service => service.GenerateTextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                "Gemini",
                It.IsAny<AiGenerationOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"score\":72,\"narrative\":\"Useful\",\"improvements\":[],\"requirementGroups\":[]}");
        var service = new RawJdFallbackMatchingService(ai.Object);

        var action = () => service.ExecuteAsync("{\"cv\":true}", "JD", null, Array.Empty<JdAnalysisDiagnostic>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(action);
        Assert.Equal("RAW_JD_FALLBACK_OUTPUT_INVALID", exception.Message);
        ai.VerifyAll();
    }
}
