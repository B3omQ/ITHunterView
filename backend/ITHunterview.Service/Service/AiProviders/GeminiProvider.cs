using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Service.Config;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Service.Matching;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;

namespace ITHunterview.Service.Service.AiProviders
{
    public class GeminiProvider : IAiProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ProviderConfig _config;
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _memoryCache;
        
        // SemaphoreSlim để tránh cache stampede khi nhiều thread cùng miss cache
        private static readonly System.Threading.SemaphoreSlim _cacheSemaphore = new System.Threading.SemaphoreSlim(1, 1);
        private const string CacheKey = "GeminiProvider_ApiKey";

        public string ProviderName => "Gemini";

        public GeminiProvider(HttpClient httpClient, IOptions<AiSettings> settings, ISystemConfigRepository systemConfigRepository, Microsoft.Extensions.Caching.Memory.IMemoryCache memoryCache)
        {
            _httpClient = httpClient;
            _systemConfigRepository = systemConfigRepository;
            _memoryCache = memoryCache;
            if (settings.Value.Providers.TryGetValue("Gemini", out var config))
            {
                _config = config;
            }
            else
            {
                _config = new ProviderConfig();
            }
        }
        
        private async Task<string> GetApiKeyAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_memoryCache.TryGetValue(CacheKey, out string cachedKey))
                return cachedKey;

            await _cacheSemaphore.WaitAsync(cancellationToken);
            try
            {
                // Double-check sau khi acquire semaphore
                if (_memoryCache.TryGetValue(CacheKey, out string cachedKey2))
                    return cachedKey2;

                cancellationToken.ThrowIfCancellationRequested();
                var dbKeyConfig = await _systemConfigRepository.GetByKeyAsync("AiApiKey_Gemini");
                var apiKey = dbKeyConfig?.ConfigValue ?? _config.ApiKey ?? string.Empty;

                // Cache 5 phút
                _memoryCache.Set(CacheKey, apiKey, TimeSpan.FromMinutes(5));
                return apiKey;
            }
            finally
            {
                _cacheSemaphore.Release();
            }
        }

        public Task<string> GenerateTextAsync(string prompt, string systemPrompt = null)
            => GenerateTextAsync(prompt, systemPrompt, CancellationToken.None);

        public async Task<string> GenerateTextAsync(string prompt, string systemPrompt, CancellationToken cancellationToken)
            => await GenerateTextAsync(prompt, systemPrompt, null, cancellationToken);

        public async Task<string> GenerateTextAsync(
            string prompt,
            string systemPrompt,
            AiGenerationOptions? options,
            CancellationToken cancellationToken)
        {
            var apiKey = await GetApiKeyAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_GEMINI_API_KEY")
            {
                throw new InvalidOperationException("Gemini API Key is not configured in DB or appsettings.");
            }

            var model = string.IsNullOrEmpty(_config.Model) ? "gemini-flash-latest" : _config.Model;
            
            // Build the standard Gemini URL
            var baseEndpoint = string.IsNullOrEmpty(_config.Endpoint)
                ? "https://generativelanguage.googleapis.com/v1beta/models"
                : _config.Endpoint.TrimEnd('/');
                
            var endpoint = $"{baseEndpoint}/{model}:generateContent?key={apiKey}";

            // Construct Gemini request payload
            // Setup contents: user prompt
            var contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            };

            var payload = new System.Collections.Generic.Dictionary<string, object?>
            {
                ["contents"] = contents
            };
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                payload["systemInstruction"] = new
                {
                    parts = new[]
                    {
                        new { text = systemPrompt }
                    }
                };
            }

            if (options is not null)
            {
                var generationConfig = new System.Collections.Generic.Dictionary<string, object?>();
                if (options.Temperature is decimal temperature) generationConfig["temperature"] = temperature;
                if (options.TopP is decimal topP) generationConfig["topP"] = topP;
                if (options.MaxOutputTokens is int maxOutputTokens) generationConfig["maxOutputTokens"] = maxOutputTokens;
                if (!string.IsNullOrWhiteSpace(options.ResponseMimeType)) generationConfig["responseMimeType"] = options.ResponseMimeType;
                if (generationConfig.Count > 0)
                {
                    payload["generationConfig"] = generationConfig;
                }
            }

            var jsonPayload = JsonSerializer.Serialize(payload);

            HttpResponseMessage response = null;
            string errorContent = string.Empty;
            int maxRetries = Math.Clamp(options?.MaxTransportAttempts ?? 3, 1, 3);

            for (int i = 0; i < maxRetries; i++)
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint);
                requestMessage.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                
                try
                {
                    response = await _httpClient.SendAsync(
                        requestMessage,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        break;
                    }
                    
                    errorContent = await BoundedHttpContentReader.ReadAsStringAsync(
                        response.Content,
                        BoundedHttpContentReader.DefaultMaxBytes,
                        cancellationToken);
                    
                    // If it is NOT a transient error, do not retry
                    if (response.StatusCode != System.Net.HttpStatusCode.ServiceUnavailable && // 503
                        response.StatusCode != System.Net.HttpStatusCode.TooManyRequests && // 429
                        (int)response.StatusCode != 500 &&
                        (int)response.StatusCode != 502 &&
                        (int)response.StatusCode != 504)
                    {
                        break;
                    }
                }
                catch (InvalidOperationException ex) when (ex.Message == "AI_RESPONSE_TOO_LARGE")
                {
                    throw;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    errorContent = ex.Message;
                }

                if (i < maxRetries - 1)
                {
                    Console.WriteLine($"[WARNING] Gemini API call returned transient status {response?.StatusCode} or threw exception. Retrying in 2 seconds... (Attempt {i + 1} of {maxRetries})");
                    await Task.Delay(2000, cancellationToken);
                }
            }

            if (response == null || !response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Gemini API call failed after {maxRetries} attempts. Status: {response?.StatusCode}; ErrorBodyLength: {errorContent.Length}");
            }

            var responseContent = await BoundedHttpContentReader.ReadAsStringAsync(
                response.Content,
                BoundedHttpContentReader.DefaultMaxBytes,
                cancellationToken);
            using var doc = JsonDocument.Parse(responseContent);

            // Extract candidate text content
            if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                candidates.GetArrayLength() > 0 &&
                candidates[0].TryGetProperty("content", out var content) &&
                content.TryGetProperty("parts", out var parts) &&
                parts.GetArrayLength() > 0)
            {
                var answer = new StringBuilder();
                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("thought", out var thought) &&
                        thought.ValueKind == JsonValueKind.True)
                    {
                        continue;
                    }

                    if (part.TryGetProperty("text", out var text) &&
                        text.ValueKind == JsonValueKind.String)
                    {
                        answer.Append(text.GetString());
                    }
                }

                if (answer.Length > 0)
                {
                    return answer.ToString();
                }
            }

            throw new Exception("Unexpected response format from Gemini API.");
        }
    }
}
