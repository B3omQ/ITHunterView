using FluentAssertions;
using ITHunterview.Service.Config;
using ITHunterview.Service.DTOs.Ai;
using ITHunterview.Service.Interface.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ITHunterview.Service.Tests.JobAnalysis;

public class AiServiceTests
{
    [Fact]
    public async Task GenerateTextWithMetadataAsync_MetadataProvider_IsInvokedExactlyOnce()
    {
        var expected = new AiTextGenerationResult(
            "structured",
            AiCompletionState.OutputLimited,
            "MAX_TOKENS",
            PromptTokens: 11,
            CandidateTokens: 7,
            ThoughtTokens: 3,
            TotalTokens: 21);
        var provider = new Mock<IAiProvider>(MockBehavior.Strict);
        provider.SetupGet(x => x.ProviderName).Returns("Gemini");
        var metadataProvider = provider.As<IAiProviderWithCompletionMetadata>();
        metadataProvider.Setup(x => x.GenerateTextWithMetadataAsync(
                "prompt", "system", AiGenerationOptions.CvAnalysisJsonExtraction, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var service = CreateService(provider.Object);

        var result = await service.GenerateTextWithMetadataAsync(
            "prompt",
            "system",
            "Gemini",
            AiGenerationOptions.CvAnalysisJsonExtraction,
            CancellationToken.None,
            "CV_EXTRACTION");

        result.Should().Be(expected);
        metadataProvider.Verify(x => x.GenerateTextWithMetadataAsync(
            "prompt", "system", AiGenerationOptions.CvAnalysisJsonExtraction, It.IsAny<CancellationToken>()), Times.Once);
        provider.Verify(x => x.GenerateTextAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AiGenerationOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GenerateTextWithMetadataAsync_LegacyProvider_WrapsOneCallAsUnknown()
    {
        var provider = new Mock<IAiProvider>(MockBehavior.Strict);
        provider.SetupGet(x => x.ProviderName).Returns("Legacy");
        provider.Setup(x => x.GenerateTextAsync(
                "prompt", "system", AiGenerationOptions.CvAnalysisJsonExtraction, It.IsAny<CancellationToken>()))
            .ReturnsAsync("legacy-text");
        var service = CreateService(provider.Object, "Legacy");

        var result = await service.GenerateTextWithMetadataAsync(
            "prompt",
            "system",
            "Legacy",
            AiGenerationOptions.CvAnalysisJsonExtraction,
            CancellationToken.None,
            "CV_EXTRACTION");

        result.Text.Should().Be("legacy-text");
        result.CompletionState.Should().Be(AiCompletionState.Unknown);
        provider.Verify(x => x.GenerateTextAsync(
            "prompt", "system", AiGenerationOptions.CvAnalysisJsonExtraction, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateTextAsync_MetadataProvider_ReturnsTextWithoutSecondProviderCall()
    {
        var provider = new Mock<IAiProvider>(MockBehavior.Strict);
        provider.SetupGet(x => x.ProviderName).Returns("Gemini");
        var metadataProvider = provider.As<IAiProviderWithCompletionMetadata>();
        metadataProvider.Setup(x => x.GenerateTextWithMetadataAsync(
                "prompt", "system", AiGenerationOptions.CvAnalysisJsonExtraction, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiTextGenerationResult("one-call", AiCompletionState.Complete, "STOP"));
        var service = CreateService(provider.Object);

        var result = await service.GenerateTextAsync(
            "prompt",
            "system",
            "Gemini",
            AiGenerationOptions.CvAnalysisJsonExtraction,
            CancellationToken.None,
            "CV_EXTRACTION");

        result.Should().Be("one-call");
        metadataProvider.Verify(x => x.GenerateTextWithMetadataAsync(
            "prompt", "system", AiGenerationOptions.CvAnalysisJsonExtraction, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateTextWithMetadataAsync_PreCancelled_DoesNotResolveOrInvokeProvider()
    {
        var providerFactory = new Mock<IAiProviderFactory>(MockBehavior.Strict);
        var service = CreateService(providerFactory);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var action = () => service.GenerateTextWithMetadataAsync(
            "prompt",
            "system",
            "Gemini",
            AiGenerationOptions.CvAnalysisJsonExtraction,
            cancellation.Token,
            "CV_EXTRACTION");

        await action.Should().ThrowAsync<OperationCanceledException>();
        providerFactory.VerifyNoOtherCalls();
    }

    private static ITHunterview.Service.Service.AiService CreateService(
        IAiProvider provider,
        string providerName = "Gemini")
    {
        var factory = new Mock<IAiProviderFactory>(MockBehavior.Strict);
        factory.Setup(x => x.GetProvider(providerName)).Returns(provider);
        return CreateService(factory);
    }

    private static ITHunterview.Service.Service.AiService CreateService(
        Mock<IAiProviderFactory> factory)
    {
        var scopeFactory = new Mock<IServiceScopeFactory>(MockBehavior.Loose);
        return new ITHunterview.Service.Service.AiService(
            factory.Object,
            scopeFactory.Object,
            Options.Create(new AiSettings { DefaultProvider = "Gemini" }),
            new HttpContextAccessor());
    }
}
