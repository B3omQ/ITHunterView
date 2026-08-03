using System.Net;
using System.Text;
using System.Text.Json;
using ITHunterview.Service.Config;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Service.AiProviders;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;

namespace ITHunterview.Service.Tests.JobAnalysis;

public sealed class AiGenerationOptionsTests
{
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
        Assert.Equal(8192, payload.RootElement.GetProperty("max_tokens").GetInt32());
    }

    [Fact]
    public async Task Gemini_StrictJsonExtraction_UsesGenerationConfig()
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
                ["Gemini"] = new() { ApiKey = "test-key", Model = "test-model", Endpoint = "https://example.test/models" }
            }
        }), systemConfigs.Object, cache);

        await provider.GenerateTextAsync("input", "system", AiGenerationOptions.StrictJsonExtraction, CancellationToken.None);

        using var payload = JsonDocument.Parse(handler.RequestBody!);
        var generation = payload.RootElement.GetProperty("generationConfig");
        Assert.Equal(0m, generation.GetProperty("temperature").GetDecimal());
        Assert.Equal("application/json", generation.GetProperty("responseMimeType").GetString());
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
        Assert.Equal(8192, payload.RootElement.GetProperty("max_tokens").GetInt32());
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
        Assert.Equal(8192, payload.RootElement.GetProperty("max_tokens").GetInt32());
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
}
