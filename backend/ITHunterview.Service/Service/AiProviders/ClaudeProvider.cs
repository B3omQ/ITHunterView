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
    public class ClaudeProvider : IAiProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ProviderConfig _config;

        public string ProviderName => "Claude";

        public ClaudeProvider(HttpClient httpClient, IOptions<AiSettings> settings)
        {
            _httpClient = httpClient;
            if (settings.Value.Providers.TryGetValue("Claude", out var config))
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
            if (string.IsNullOrEmpty(_config.ApiKey) || _config.ApiKey == "YOUR_CLAUDE_API_KEY")
            {
                throw new InvalidOperationException("Claude API Key is not configured.");
            }

            var endpoint = string.IsNullOrEmpty(_config.Endpoint)
                ? "https://api.anthropic.com/v1/messages"
                : _config.Endpoint;

            var model = string.IsNullOrEmpty(_config.Model) ? "claude-3-5-sonnet-20241022" : _config.Model;

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint);
            requestMessage.Headers.Add("x-api-key", _config.ApiKey);
            requestMessage.Headers.Add("anthropic-version", "2023-06-01");

            var messages = new[]
            {
                new { role = "user", content = prompt }
            };

            object payload;
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                payload = new
                {
                    model = model,
                    max_tokens = 4096,
                    system = systemPrompt,
                    messages = messages
                };
            }
            else
            {
                payload = new
                {
                    model = model,
                    max_tokens = 4096,
                    messages = messages
                };
            }

            var jsonPayload = JsonSerializer.Serialize(payload);
            requestMessage.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(requestMessage);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Claude API call failed with status code {response.StatusCode}: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseContent);

            // Extract content text
            if (doc.RootElement.TryGetProperty("content", out var contentList) &&
                contentList.GetArrayLength() > 0 &&
                contentList[0].TryGetProperty("type", out var type) &&
                type.GetString() == "text" &&
                contentList[0].TryGetProperty("text", out var text))
            {
                return text.GetString();
            }

            throw new Exception("Unexpected response format from Claude API.");
        }
    }
}
