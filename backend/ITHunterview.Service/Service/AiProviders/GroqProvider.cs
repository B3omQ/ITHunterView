using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ITHunterview.Service.Config;
using ITHunterview.Service.Interface.Service;
using Microsoft.Extensions.Options;

namespace ITHunterview.Service.Service.AiProviders
{
    public class GroqProvider : IAiProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ProviderConfig _config;

        public string ProviderName => "Groq";

        public GroqProvider(HttpClient httpClient, IOptions<AiSettings> settings)
        {
            _httpClient = httpClient;
            if (settings.Value.Providers.TryGetValue("Groq", out var config))
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
            if (string.IsNullOrEmpty(_config.ApiKey) || _config.ApiKey == "YOUR_GROQ_API_KEY")
            {
                throw new InvalidOperationException("Groq API Key is not configured.");
            }

            var endpoint = string.IsNullOrEmpty(_config.Endpoint) 
                ? "https://api.groq.com/openai/v1/chat/completions" 
                : _config.Endpoint;

            var model = string.IsNullOrEmpty(_config.Model) ? "llama-3.3-70b-versatile" : _config.Model;

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiKey);

            var messages = new System.Collections.Generic.List<object>();
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                messages.Add(new { role = "system", content = systemPrompt });
            }
            messages.Add(new { role = "user", content = prompt });

            var payload = new
            {
                model = model,
                messages = messages,
                temperature = 0.7
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            requestMessage.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(requestMessage);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Groq API call failed with status code {response.StatusCode}: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseContent);
            
            // Extract choice content
            if (doc.RootElement.TryGetProperty("choices", out var choices) && 
                choices.GetArrayLength() > 0 && 
                choices[0].TryGetProperty("message", out var message) && 
                message.TryGetProperty("content", out var content))
            {
                return content.GetString();
            }

            throw new Exception("Unexpected response format from Groq API.");
        }
    }
}
