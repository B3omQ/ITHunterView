using System.Net;
using System.Net.Http.Headers;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;
using ITHunterview.Service.Config;
using ITHunterview.Service.Constant.Prompts;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Exceptions;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Service.Matching;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace ITHunterview.Service.Tests.Matching;

public sealed class CvTextExtractorServiceTests
{
    [Fact]
    public async Task ExtractParsedDataFromRawTextAsync_UsesCvStructuredJsonProfile()
    {
        var aiService = new Mock<IAiService>(MockBehavior.Strict);
        aiService.Setup(x => x.GetActiveProviderNameAsync()).ReturnsAsync("Gemini");
        aiService.Setup(x => x.GenerateTextAsync(
                It.IsAny<string>(),
                "system from database",
                "Gemini",
                It.IsAny<AiGenerationOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("{}");
        var validator = new Mock<ICvAnalysisResponseValidator>(MockBehavior.Strict);
        validator.Setup(x => x.ValidateAndCanonicalize(
                "{}",
                It.IsAny<CvAnalysisInputSnapshot>()))
            .Returns(CvAnalysisValidationResult.Success("{\"canonical\":true}"));
        var service = CreateService(aiService, validator);

        var result = await service.ExtractParsedDataFromRawTextAsync(
            "Jane Doe\nC# developer\n",
            "pasted_text",
            "resume.txt",
            CancellationToken.None);

        result.Should().Be("{\"canonical\":true}");
        aiService.Verify(x => x.GenerateTextAsync(
            It.IsAny<string>(),
            "system from database",
            "Gemini",
            It.Is<AiGenerationOptions>(options =>
                options.ProfileId == "cv-analysis-json/v1" &&
                options.Temperature == 0m &&
                options.TopP == 0.1m &&
                options.MaxOutputTokens == 8192 &&
                options.ResponseMimeType == "application/json"),
            It.IsAny<CancellationToken>()), Times.Once);
        aiService.Verify(x => x.GenerateTextAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExtractParsedDataFromRawTextAsync_ExtractsJsonObjectWrappedInProviderProse()
    {
        var aiService = CreateAiServiceReturning(
            "I extracted the CV below.\n{\"schema_version\":\"cv-analysis/v2\"}\nEnd of response.");
        var validator = new Mock<ICvAnalysisResponseValidator>(MockBehavior.Strict);
        validator.Setup(x => x.ValidateAndCanonicalize(
                "{\"schema_version\":\"cv-analysis/v2\"}",
                It.IsAny<CvAnalysisInputSnapshot>()))
            .Returns(CvAnalysisValidationResult.Success("{\"canonical\":true}"));
        var service = CreateService(aiService, validator);

        var result = await service.ExtractParsedDataFromRawTextAsync(
            "Jane Doe\nC# developer\n",
            "pasted_text",
            "resume.txt",
            CancellationToken.None);

        result.Should().Be("{\"canonical\":true}");
    }

    [Fact]
    public async Task ExtractParsedDataFromUrlAsync_ValidationFailureEscapesWithoutSecondAiCall()
    {
        var aiService = CreateAiServiceReturning("{}");
        var validator = new Mock<ICvAnalysisResponseValidator>(MockBehavior.Strict);
        validator.Setup(x => x.ValidateAndCanonicalize(
                "{}",
                It.IsAny<CvAnalysisInputSnapshot>()))
            .Returns(CvAnalysisValidationResult.Failure(
                "CV_ANALYSIS_SCHEMA_INVALID",
                "TYPED_DESERIALIZATION_FAILED",
                "$.matching_evidence"));
        var handler = CreateDocxHandler();
        var service = CreateService(aiService, validator, CreateHttpClientFactory(handler));

        var action = () => service.ExtractParsedDataFromUrlAsync(
            "https://cdn.example/resume.docx",
            "bad",
            CancellationToken.None);

        var exception = await action.Should().ThrowAsync<CvAnalysisValidationException>();
        exception.Which.FailureCode.Should().Be("CV_ANALYSIS_SCHEMA_INVALID");
        aiService.Verify(x => x.GenerateTextAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            "Gemini",
            It.IsAny<AiGenerationOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task ExtractParsedDataFromUrlAsync_ProviderFailureEscapesWithoutFallbackAiCall()
    {
        var aiService = new Mock<IAiService>(MockBehavior.Strict);
        aiService.Setup(x => x.GetActiveProviderNameAsync()).ReturnsAsync("Gemini");
        aiService.Setup(x => x.GenerateTextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                "Gemini",
                It.IsAny<AiGenerationOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("provider unavailable"));
        var handler = CreateDocxHandler();
        var service = CreateService(
            aiService,
            new Mock<ICvAnalysisResponseValidator>(MockBehavior.Strict),
            CreateHttpClientFactory(handler));

        var action = () => service.ExtractParsedDataFromUrlAsync(
            "https://cdn.example/resume.docx",
            "bad",
            CancellationToken.None);

        await action.Should().ThrowAsync<HttpRequestException>();
        aiService.Verify(x => x.GenerateTextAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            "Gemini",
            It.IsAny<AiGenerationOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExtractParsedDataFromUrlAsync_DownloadRejectedAndGarbageFallback_DoesNotCallAi()
    {
        var aiService = new Mock<IAiService>(MockBehavior.Strict);
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var service = CreateService(
            aiService,
            new Mock<ICvAnalysisResponseValidator>(MockBehavior.Strict),
            CreateHttpClientFactory(handler));

        var result = await service.ExtractParsedDataFromUrlAsync(
            "https://cdn.example/resume.docx",
            "bad",
            CancellationToken.None);

        result.Should().BeEmpty();
        aiService.VerifyNoOtherCalls();
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task ExtractParsedDataFromUrlAsync_CleanFallback_SkipsDownloadAndCallsAiOnce()
    {
        var aiService = CreateAiServiceReturning("{}");
        var validator = new Mock<ICvAnalysisResponseValidator>(MockBehavior.Strict);
        validator.Setup(x => x.ValidateAndCanonicalize(
                "{}",
                It.IsAny<CvAnalysisInputSnapshot>()))
            .Returns(CvAnalysisValidationResult.Success("{\"canonical\":true}"));
        var handler = CreateDocxHandler();
        var service = CreateService(aiService, validator, CreateHttpClientFactory(handler));

        var result = await service.ExtractParsedDataFromUrlAsync(
            "https://cdn.example/resume.docx",
            "Jane Doe\nBackend developer with C# experience\n",
            CancellationToken.None);

        result.Should().Be("{\"canonical\":true}");
        handler.CallCount.Should().Be(0);
        aiService.Verify(x => x.GenerateTextAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            "Gemini",
            It.IsAny<AiGenerationOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExtractParsedDataFromUrlAsync_CallerCancellation_DoesNotFallback()
    {
        var aiService = new Mock<IAiService>(MockBehavior.Strict);
        using var cancellation = new CancellationTokenSource();
        var handler = new CancellingHttpMessageHandler(cancellation);
        var service = CreateService(
            aiService,
            new Mock<ICvAnalysisResponseValidator>(MockBehavior.Strict),
            CreateHttpClientFactory(handler));

        var action = () => service.ExtractParsedDataFromUrlAsync(
            "https://cdn.example/resume.docx",
            "bad",
            cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
        handler.CallCount.Should().Be(1);
        aiService.VerifyNoOtherCalls();
    }

    private static CvTextExtractorService CreateService(
        Mock<IAiService> aiService,
        Mock<ICvAnalysisResponseValidator> validator,
        IHttpClientFactory? httpClientFactory = null)
    {
        var promptService = new Mock<IPromptManagementService>(MockBehavior.Strict);
        promptService.Setup(x => x.GetActivePromptPairSnapshotAsync(
                CvAnalysisPromptContract.SystemPromptKey,
                CvAnalysisPromptContract.UserPromptKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePromptPair());

        return new CvTextExtractorService(
            NullLogger<CvTextExtractorService>.Instance,
            httpClientFactory ?? Mock.Of<IHttpClientFactory>(),
            Options.Create(new AiSettings()),
            aiService.Object,
            Mock.Of<ISystemConfigRepository>(),
            promptService.Object,
            validator.Object);
    }

    private static Mock<IAiService> CreateAiServiceReturning(string response)
    {
        var aiService = new Mock<IAiService>(MockBehavior.Strict);
        aiService.Setup(x => x.GetActiveProviderNameAsync()).ReturnsAsync("Gemini");
        aiService.Setup(x => x.GenerateTextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                "Gemini",
                It.IsAny<AiGenerationOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        return aiService;
    }

    private static IHttpClientFactory CreateHttpClientFactory(HttpMessageHandler handler)
    {
        var factory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        factory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handler, disposeHandler: false));
        return factory.Object;
    }

    private static StubHttpMessageHandler CreateDocxHandler()
    {
        var bytes = CreateDocxBytes(
            "Jane Doe Backend Developer. Built and maintained C# APIs for enterprise systems.");
        return new StubHttpMessageHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(bytes)
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
            return response;
        });
    }

    private static byte[] CreateDocxBytes(string text)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(
                   stream,
                   DocumentFormat.OpenXml.WordprocessingDocumentType.Document,
                   autoSave: true))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(
                new Body(
                    new Paragraph(
                        new Run(
                            new Text(text)))));
            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    private static PromptPairSnapshotDto CreatePromptPair() => new()
    {
        Contract = "cv-analysis/v2",
        System = new PromptSnapshotDto
        {
            PromptKey = CvAnalysisPromptContract.SystemPromptKey,
            VersionTag = "v2.0",
            Content = "system from database"
        },
        User = new PromptSnapshotDto
        {
            PromptKey = CvAnalysisPromptContract.UserPromptKey,
            VersionTag = "v2.0",
            Content = $"parse {CvAnalysisPromptContract.UserPlaceholder}"
        }
    };

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(responseFactory(request));
        }
    }

    private sealed class CancellingHttpMessageHandler(
        CancellationTokenSource cancellationSource) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            cancellationSource.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("Cancellation token was not observed.");
        }
    }
}
