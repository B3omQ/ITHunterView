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
            if (string.IsNullOrEmpty(_config.ApiKey) || _config.ApiKey == "YOUR_GROQ_API_KEY")
            {
                throw new InvalidOperationException("Groq API Key is not configured.");
            }

            var endpoint = string.IsNullOrEmpty(_config.Endpoint) 
                ? "https://api.groq.com/openai/v1/chat/completions" 
                : _config.Endpoint;

            var model = string.IsNullOrEmpty(_config.Model) ? "llama-3.3-70b-versatile" : _config.Model;

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
            if (options?.TopP is decimal topP) payload["top_p"] = topP;
            if (options?.MaxOutputTokens is int maxTokens) payload["max_tokens"] = maxTokens;

            var jsonPayload = JsonSerializer.Serialize(payload);

            HttpResponseMessage response = null;
            string errorContent = string.Empty;
            int maxRetries = Math.Clamp(options?.MaxTransportAttempts ?? 3, 1, 3);

            for (int i = 0; i < maxRetries; i++)
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint);
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiKey);
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
                    Console.WriteLine($"[WARNING] Groq API call returned transient status {response?.StatusCode} or threw exception. Retrying in 2 seconds... (Attempt {i + 1} of {maxRetries})");
                    await Task.Delay(2000, cancellationToken);
                }
            }

            if (response == null || !response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Groq API call failed after {maxRetries} attempts. Status: {response?.StatusCode}; ErrorBodyLength: {errorContent.Length}");
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

            throw new Exception("Unexpected response format from Groq API.");
        }
    }
}
