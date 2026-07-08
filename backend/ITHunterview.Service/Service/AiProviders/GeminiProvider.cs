using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ITHunterview.Service.Config;
using ITHunterview.Service.Interface.Service;
using Microsoft.Extensions.Options;

namespace ITHunterview.Service.Service.AiProviders
{
    public class GeminiProvider : IAiProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ProviderConfig _config;

        public string ProviderName => "Gemini";

        public GeminiProvider(HttpClient httpClient, IOptions<AiSettings> settings)
        {
            _httpClient = httpClient;
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
            if (string.IsNullOrEmpty(_config.ApiKey) || _config.ApiKey == "YOUR_GEMINI_API_KEY")
            {
                throw new InvalidOperationException("Gemini API Key is not configured.");
            }

            var model = string.IsNullOrEmpty(_config.Model) ? "gemini-2.5-flash" : _config.Model;
            
            // Build the standard Gemini URL
            var baseEndpoint = string.IsNullOrEmpty(_config.Endpoint)
                ? "https://generativelanguage.googleapis.com/v1beta/models"
                : _config.Endpoint.TrimEnd('/');
                
            var endpoint = $"{baseEndpoint}/{model}:generateContent?key={_config.ApiKey}";

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint);

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
            requestMessage.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(requestMessage);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Gemini API call failed with status code {response.StatusCode}: {errorContent}");
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
