using System.Net;
using System.Text;
using System.Text.Json;
using ITHunterview.Service.Config;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Service.AiProviders;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace ITHunterview.Service.Tests.JobAnalysis;

public sealed class AiGenerationOptionsTests
{
    [Fact]
    public void JdAnalysisProfile_ReservesReasoningAndJsonOutputCapacity()
    {
        Assert.Equal(16384, AiGenerationOptions.StrictJsonExtraction.MaxOutputTokens);
        Assert.Equal(3000, AiGenerationOptions.StrictJsonExtraction.ThinkingBudget);
        Assert.Equal("medium", AiGenerationOptions.StrictJsonExtraction.ThinkingLevel);
        Assert.Equal(1, AiGenerationOptions.StrictJsonExtraction.MaxTransportAttempts);
    }

    [Fact]
    public void CvAnalysisProfiles_UseBoundedSingleAttemptsAndThinkingBudget()
    {
        Assert.Equal(8192, AiGenerationOptions.CvAnalysisJsonExtraction.MaxOutputTokens);
        Assert.Equal(12288, AiGenerationOptions.CvAnalysisJsonRetry.MaxOutputTokens);
        Assert.Equal(0m, AiGenerationOptions.CvAnalysisJsonExtraction.Temperature);
        Assert.Equal(0.1m, AiGenerationOptions.CvAnalysisJsonExtraction.TopP);
        Assert.Equal("application/json", AiGenerationOptions.CvAnalysisJsonExtraction.ResponseMimeType);
        Assert.Equal(1, AiGenerationOptions.CvAnalysisJsonExtraction.MaxTransportAttempts);
        Assert.Equal(1, AiGenerationOptions.CvAnalysisJsonRetry.MaxTransportAttempts);
        Assert.Equal(512, AiGenerationOptions.CvAnalysisJsonExtraction.ThinkingBudget);
        Assert.Equal("minimal", AiGenerationOptions.CvAnalysisJsonExtraction.ThinkingLevel);
    }

    [Fact]
    public void JdMatchingProfiles_UseOneTransportAttemptAndAControlledLargerRetry()
    {
        Assert.Equal(16384, AiGenerationOptions.JdMatchingJsonScoring.MaxOutputTokens);
        Assert.Equal(20480, AiGenerationOptions.JdMatchingJsonRetry.MaxOutputTokens);
        Assert.Equal(0.2m, AiGenerationOptions.JdMatchingJsonScoring.Temperature);
        Assert.Equal(0.1m, AiGenerationOptions.JdMatchingJsonScoring.TopP);
        Assert.Equal("application/json", AiGenerationOptions.JdMatchingJsonScoring.ResponseMimeType);
        Assert.Equal(1, AiGenerationOptions.JdMatchingJsonScoring.MaxTransportAttempts);
        Assert.Equal(1, AiGenerationOptions.JdMatchingJsonRetry.MaxTransportAttempts);
        Assert.Equal(3000, AiGenerationOptions.JdMatchingJsonScoring.ThinkingBudget);
        Assert.Equal(3000, AiGenerationOptions.JdMatchingJsonRetry.ThinkingBudget);
        Assert.Equal("medium", AiGenerationOptions.JdMatchingJsonScoring.ThinkingLevel);
        Assert.Equal("medium", AiGenerationOptions.JdMatchingJsonRetry.ThinkingLevel);
    }

    [Fact]
    public async Task Groq_StrictJsonExtraction_UsesRequestedLowTemperatureAndBound()
    {
        var handler = new CaptureHandler("""{"choices":[{"message":{"content":"{}"}}]}""");
        using var client = new HttpClient(handler);
        var provider = new GroqProvider(client, Options.Create(new AiSettings
        {
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["Groq"] = new() { ApiKey = "test-key", Model = "test-model", Endpoint = "https://example.test/chat" }
            }
        }));

        await provider.GenerateTextAsync("input", "system", AiGenerationOptions.StrictJsonExtraction, CancellationToken.None);

        using var payload = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal(0m, payload.RootElement.GetProperty("temperature").GetDecimal());
        Assert.Equal(16384, payload.RootElement.GetProperty("max_tokens").GetInt32());
    }

    [Fact]
    public async Task Gemini_25_JdExtraction_LimitsThinkingBudget()
    {
        var handler = new CaptureHandler("""{"candidates":[{"content":{"parts":[{"text":"{}"}]}}]}""");
        using var client = new HttpClient(handler);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var systemConfigs = new Mock<ISystemConfigRepository>();
        systemConfigs.Setup(x => x.GetByKeyAsync("AiApiKey_Gemini")).ReturnsAsync((ITHunterview.Domain.Entities.SystemConfigs?)null);
        var provider = new GeminiProvider(client, Options.Create(new AiSettings
        {
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["Gemini"] = new() { ApiKey = "test-key", Model = "gemini-2.5-flash", Endpoint = "https://example.test/models" }
            }
        }), systemConfigs.Object, cache, NullLogger<GeminiProvider>.Instance);

        await provider.GenerateTextAsync("input", "system", AiGenerationOptions.StrictJsonExtraction, CancellationToken.None);

        using var payload = JsonDocument.Parse(handler.RequestBody!);
        var generation = payload.RootElement.GetProperty("generationConfig");
        Assert.Equal(0m, generation.GetProperty("temperature").GetDecimal());
        Assert.Equal("application/json", generation.GetProperty("responseMimeType").GetString());
        Assert.Equal(16384, generation.GetProperty("maxOutputTokens").GetInt32());
        var thinking = generation.GetProperty("thinkingConfig");
        Assert.Equal(3000, thinking.GetProperty("thinkingBudget").GetInt32());
        Assert.False(thinking.TryGetProperty("thinkingLevel", out _));
    }

    [Fact]
    public async Task Gemini_3_JdExtraction_UsesMediumThinkingLevel()
    {
        var handler = new CaptureHandler("{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"{}\"}]}}]}");
        using var client = new HttpClient(handler);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var systemConfigs = new Mock<ISystemConfigRepository>();
        systemConfigs.Setup(x => x.GetByKeyAsync("AiApiKey_Gemini")).ReturnsAsync((ITHunterview.Domain.Entities.SystemConfigs?)null);
        var provider = new GeminiProvider(client, Options.Create(new AiSettings
        {
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["Gemini"] = new() { ApiKey = "test-key", Model = "gemini-3.5-flash", Endpoint = "https://example.test/models" }
            }
        }), systemConfigs.Object, cache, NullLogger<GeminiProvider>.Instance);

        await provider.GenerateTextAsync("input", "system", AiGenerationOptions.StrictJsonExtraction, CancellationToken.None);

        using var payload = JsonDocument.Parse(handler.RequestBody!);
        var thinking = payload.RootElement.GetProperty("generationConfig").GetProperty("thinkingConfig");
        Assert.Equal("medium", thinking.GetProperty("thinkingLevel").GetString());
        Assert.False(thinking.TryGetProperty("thinkingBudget", out _));
    }

    [Fact]
    public async Task Gemini_25_CvExtraction_UsesThinkingBudget()
    {
        var handler = new CaptureHandler("{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"{}\"}]}}]}");
        using var client = new HttpClient(handler);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var systemConfigs = new Mock<ISystemConfigRepository>();
        systemConfigs.Setup(x => x.GetByKeyAsync("AiApiKey_Gemini")).ReturnsAsync((ITHunterview.Domain.Entities.SystemConfigs?)null);
        var provider = new GeminiProvider(client, Options.Create(new AiSettings
        {
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["Gemini"] = new() { ApiKey = "test-key", Model = "gemini-2.5-flash", Endpoint = "https://example.test/models" }
            }
        }), systemConfigs.Object, cache, NullLogger<GeminiProvider>.Instance);

        await provider.GenerateTextAsync("input", "system", AiGenerationOptions.CvAnalysisJsonExtraction, CancellationToken.None);

        using var payload = JsonDocument.Parse(handler.RequestBody!);
        var thinking = payload.RootElement.GetProperty("generationConfig").GetProperty("thinkingConfig");
        Assert.Equal(512, thinking.GetProperty("thinkingBudget").GetInt32());
        Assert.False(thinking.TryGetProperty("thinkingLevel", out _));
    }

    [Fact]
    public async Task Gemini_3_CvExtraction_UsesThinkingLevel()
    {
        var handler = new CaptureHandler("{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"{}\"}]}}]}");
        using var client = new HttpClient(handler);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var systemConfigs = new Mock<ISystemConfigRepository>();
        systemConfigs.Setup(x => x.GetByKeyAsync("AiApiKey_Gemini")).ReturnsAsync((ITHunterview.Domain.Entities.SystemConfigs?)null);
        var provider = new GeminiProvider(client, Options.Create(new AiSettings
        {
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["Gemini"] = new() { ApiKey = "test-key", Model = "gemini-3.5-flash", Endpoint = "https://example.test/models" }
            }
        }), systemConfigs.Object, cache, NullLogger<GeminiProvider>.Instance);

        await provider.GenerateTextAsync("input", "system", AiGenerationOptions.CvAnalysisJsonExtraction, CancellationToken.None);

        using var payload = JsonDocument.Parse(handler.RequestBody!);
        var thinking = payload.RootElement.GetProperty("generationConfig").GetProperty("thinkingConfig");
        Assert.Equal("minimal", thinking.GetProperty("thinkingLevel").GetString());
        Assert.False(thinking.TryGetProperty("thinkingBudget", out _));
    }

    [Fact]
    public async Task OpenAi_StrictJsonExtraction_UsesRequestedJsonModeAndBounds()
    {
        var handler = new CaptureHandler("""{"choices":[{"message":{"content":"{}"}}]}""");
        using var client = new HttpClient(handler);
        var provider = new OpenAiProvider(client, Options.Create(new AiSettings
        {
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["OpenAI"] = new() { ApiKey = "test-key", Model = "test-model", Endpoint = "https://example.test/chat" }
            }
        }));

        await provider.GenerateTextAsync("input", "system", AiGenerationOptions.StrictJsonExtraction, CancellationToken.None);

        using var payload = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal(0m, payload.RootElement.GetProperty("temperature").GetDecimal());
        Assert.Equal(16384, payload.RootElement.GetProperty("max_tokens").GetInt32());
        Assert.Equal("json_object", payload.RootElement.GetProperty("response_format").GetProperty("type").GetString());
    }

    [Fact]
    public async Task Claude_StrictJsonExtraction_UsesRequestedBoundsAndTemperature()
    {
        var handler = new CaptureHandler("""{"content":[{"type":"text","text":"{}"}]}""");
        using var client = new HttpClient(handler);
        var provider = new ClaudeProvider(client, Options.Create(new AiSettings
        {
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["Claude"] = new() { ApiKey = "test-key", Model = "test-model", Endpoint = "https://example.test/messages" }
            }
        }));

        await provider.GenerateTextAsync("input", "system", AiGenerationOptions.StrictJsonExtraction, CancellationToken.None);

        using var payload = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal(0m, payload.RootElement.GetProperty("temperature").GetDecimal());
        Assert.Equal(16384, payload.RootElement.GetProperty("max_tokens").GetInt32());
    }

    [Fact]
    public async Task Groq_StrictJsonExtraction_UsesOneTransportAttempt()
    {
        var handler = new CountingStatusHandler(HttpStatusCode.ServiceUnavailable);
        using var client = new HttpClient(handler);
        var provider = new GroqProvider(client, Options.Create(new AiSettings
        {
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["Groq"] = new() { ApiKey = "test-key", Model = "test-model", Endpoint = "https://example.test/chat" }
            }
        }));

        await Assert.ThrowsAsync<HttpRequestException>(() => provider.GenerateTextAsync(
            "input", "system", AiGenerationOptions.StrictJsonExtraction, CancellationToken.None));

        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task Gemini_CvExtraction_UsesOneTransportAttempt()
    {
        var handler = new CountingStatusHandler(HttpStatusCode.ServiceUnavailable);
        using var client = new HttpClient(handler);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var systemConfigs = new Mock<ISystemConfigRepository>();
        systemConfigs.Setup(x => x.GetByKeyAsync("AiApiKey_Gemini")).ReturnsAsync((ITHunterview.Domain.Entities.SystemConfigs?)null);
        var provider = new GeminiProvider(client, Options.Create(new AiSettings
        {
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["Gemini"] = new() { ApiKey = "test-key", Model = "gemini-2.5-flash", Endpoint = "https://example.test/models" }
            }
        }), systemConfigs.Object, cache, NullLogger<GeminiProvider>.Instance);

        await Assert.ThrowsAsync<HttpRequestException>(() => provider.GenerateTextAsync(
            "input", "system", AiGenerationOptions.CvAnalysisJsonExtraction, CancellationToken.None));

        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task Gemini_JoinsAllNonThoughtAnswerParts()
    {
        var handler = new CaptureHandler("{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"{\\\"a\\\":\"},{\"text\":\"1}\"}]}}]}");
        using var client = new HttpClient(handler);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var systemConfigs = new Mock<ISystemConfigRepository>();
        systemConfigs.Setup(x => x.GetByKeyAsync("AiApiKey_Gemini")).ReturnsAsync((ITHunterview.Domain.Entities.SystemConfigs?)null);
        var provider = new GeminiProvider(client, Options.Create(new AiSettings
        {
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["Gemini"] = new() { ApiKey = "test-key", Model = "test-model", Endpoint = "https://example.test/models" }
            }
        }), systemConfigs.Object, cache, NullLogger<GeminiProvider>.Instance);

        var result = await provider.GenerateTextAsync("input", "system", AiGenerationOptions.StrictJsonExtraction, CancellationToken.None);

        Assert.Equal("{\"a\":1}", result);
    }

    [Fact]
    public async Task Gemini_MaxTokens_ReturnsNonThoughtPartialText()
    {
        var handler = new CaptureHandler("""
            {"candidates":[{"finishReason":"MAX_TOKENS","content":{"parts":[{"thought":true,"text":"hidden"},{"text":"{\"schema_version\":\"cv-analysis/v2\""}]}}],"usageMetadata":{"promptTokenCount":10,"candidatesTokenCount":5,"thoughtsTokenCount":2,"totalTokenCount":17}}
            """);
        using var client = new HttpClient(handler);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var systemConfigs = new Mock<ISystemConfigRepository>();
        systemConfigs.Setup(x => x.GetByKeyAsync("AiApiKey_Gemini")).ReturnsAsync((ITHunterview.Domain.Entities.SystemConfigs?)null);
        var provider = new GeminiProvider(client, Options.Create(new AiSettings
        {
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["Gemini"] = new() { ApiKey = "test-key", Model = "gemini-2.5-flash", Endpoint = "https://example.test/models" }
            }
        }), systemConfigs.Object, cache, NullLogger<GeminiProvider>.Instance);

        var result = await provider.GenerateTextAsync("input", "system", AiGenerationOptions.CvAnalysisJsonExtraction, CancellationToken.None);

        Assert.Equal("{\"schema_version\":\"cv-analysis/v2\"", result);
    }

    private sealed class CaptureHandler(string responseBody) : HttpMessageHandler
    {
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class CountingStatusHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent("error", Encoding.UTF8, "application/json")
            });
        }
    }
}
