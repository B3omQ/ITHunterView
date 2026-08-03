using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Service.Config;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Service.Matching;
using Microsoft.Extensions.Options;

namespace ITHunterview.Service.Service.AiProviders
{
    public class OpenAiProvider : IAiProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ProviderConfig _config;

        public string ProviderName => "OpenAI";

        public OpenAiProvider(HttpClient httpClient, IOptions<AiSettings> settings)
        {
            _httpClient = httpClient;
            if (settings.Value.Providers.TryGetValue("OpenAI", out var config))
            {
                _config = config;
            }
            else
            {
                _config = new ProviderConfig();
            }
        }

        public Task<string> GenerateTextAsync(string prompt, string systemPrompt = null)
            => GenerateTextAsync(prompt, systemPrompt, CancellationToken.None);

        public async Task<string> GenerateTextAsync(string prompt, string systemPrompt, CancellationToken cancellationToken)
            => await GenerateTextAsync(prompt, systemPrompt, options: null, cancellationToken);

        public async Task<string> GenerateTextAsync(
            string prompt,
            string systemPrompt,
            AiGenerationOptions? options,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_config.ApiKey) || _config.ApiKey == "YOUR_OPENAI_API_KEY")
            {
                throw new InvalidOperationException("OpenAI API Key is not configured.");
            }

            var endpoint = string.IsNullOrEmpty(_config.Endpoint) 
                ? "https://api.openai.com/v1/chat/completions" 
                : _config.Endpoint;

            var model = string.IsNullOrEmpty(_config.Model) ? "gpt-4o" : _config.Model;

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiKey);

            var messages = new System.Collections.Generic.List<object>();
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                messages.Add(new { role = "system", content = systemPrompt });
            }
            messages.Add(new { role = "user", content = prompt });

            var payload = new System.Collections.Generic.Dictionary<string, object?>
            {
                ["model"] = model,
                ["messages"] = messages,
                ["temperature"] = options?.Temperature ?? 0.7m
            };
            if (options?.TopP is not null)
                payload["top_p"] = options.TopP.Value;
            if (options?.MaxOutputTokens is not null)
                payload["max_tokens"] = options.MaxOutputTokens.Value;
            if (string.Equals(options?.ResponseMimeType, "application/json", StringComparison.OrdinalIgnoreCase))
                payload["response_format"] = new { type = "json_object" };

            var jsonPayload = JsonSerializer.Serialize(payload);
            requestMessage.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(
                requestMessage,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await BoundedHttpContentReader.ReadAsStringAsync(
                    response.Content,
                    BoundedHttpContentReader.DefaultMaxBytes,
                    cancellationToken);
                throw new HttpRequestException($"OpenAI API call failed with status code {response.StatusCode}; ErrorBodyLength: {errorContent.Length}");
            }

            var responseContent = await BoundedHttpContentReader.ReadAsStringAsync(
                response.Content,
                BoundedHttpContentReader.DefaultMaxBytes,
                cancellationToken);
            using var doc = JsonDocument.Parse(responseContent);
            
            // Extract choice content
            if (doc.RootElement.TryGetProperty("choices", out var choices) && 
                choices.GetArrayLength() > 0 && 
                choices[0].TryGetProperty("message", out var message) && 
                message.TryGetProperty("content", out var content))
            {
                return content.GetString();
            }

            throw new Exception("Unexpected response format from OpenAI API.");
        }
    }
}
