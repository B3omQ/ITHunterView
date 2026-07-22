using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ITHunterview.Service.Config;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.Extensions.Options;

namespace ITHunterview.Service.Service.AiProviders
{
    public class GeminiProvider : IAiProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ProviderConfig _config;
        private readonly ISystemConfigRepository _systemConfigRepository;

        public string ProviderName => "Gemini";

        public GeminiProvider(HttpClient httpClient, IOptions<AiSettings> settings, ISystemConfigRepository systemConfigRepository)
        {
            _httpClient = httpClient;
            _systemConfigRepository = systemConfigRepository;
            if (settings.Value.Providers.TryGetValue("Gemini", out var config))
            {
                _config = config;
            }
            else
            {
                _config = new ProviderConfig();
            }
        }

        public async Task<string> GenerateTextAsync(string prompt, string systemPrompt = null)
        {
            var dbKeyConfig = await _systemConfigRepository.GetByKeyAsync("AiApiKey_Gemini");
            var apiKey = dbKeyConfig?.ConfigValue ?? _config.ApiKey;

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

            object payload;
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                payload = new
                {
                    contents = contents,
                    systemInstruction = new
                    {
                        parts = new[]
                        {
                            new { text = systemPrompt }
                        }
                    }
                };
            }
            else
            {
                payload = new
                {
                    contents = contents
                };
            }

            var jsonPayload = JsonSerializer.Serialize(payload);

            HttpResponseMessage response = null;
            string errorContent = string.Empty;
            int maxRetries = 3;

            for (int i = 0; i < maxRetries; i++)
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint);
                requestMessage.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                
                try
                {
                    response = await _httpClient.SendAsync(requestMessage);
                    if (response.IsSuccessStatusCode)
                    {
                        break;
                    }
                    
                    errorContent = await response.Content.ReadAsStringAsync();
                    
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
                catch (Exception ex)
                {
                    errorContent = ex.Message;
                }

                if (i < maxRetries - 1)
                {
                    Console.WriteLine($"[WARNING] Gemini API call returned transient status {response?.StatusCode} or threw exception. Retrying in 2 seconds... (Attempt {i + 1} of {maxRetries})");
                    await Task.Delay(2000);
                }
            }

            if (response == null || !response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Gemini API call failed after {maxRetries} attempts. Status: {response?.StatusCode}, Error: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseContent);

            // Extract candidate text content
            if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                candidates.GetArrayLength() > 0 &&
                candidates[0].TryGetProperty("content", out var content) &&
                content.TryGetProperty("parts", out var parts) &&
                parts.GetArrayLength() > 0 &&
                parts[0].TryGetProperty("text", out var text))
            {
                return text.GetString();
            }

            throw new Exception("Unexpected response format from Gemini API.");
        }
    }
}
