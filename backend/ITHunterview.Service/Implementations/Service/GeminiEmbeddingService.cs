using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ITHunterview.Service.Interface.Service;
using Microsoft.Extensions.Configuration;

namespace ITHunterview.Service.Implementations.Service
{
    public class GeminiEmbeddingService : IAiEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string ApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/text-embedding-004:embedContent";

        public GeminiEmbeddingService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GeminiSettings:ApiKey"];
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new InvalidOperationException("Gemini API Key is not configured.");
            }

            var requestBody = new
            {
                model = "models/text-embedding-004",
                content = new
                {
                    parts = new[]
                    {
                        new { text = text }
                    }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var url = $"{ApiUrl}?key={_apiKey}";

            var response = await _httpClient.PostAsync(url, jsonContent);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(responseString);
            
            var valuesArray = jsonDoc.RootElement
                .GetProperty("embedding")
                .GetProperty("values")
                .EnumerateArray();

            var embedding = new float[768];
            int i = 0;
            foreach (var val in valuesArray)
            {
                embedding[i++] = val.GetSingle();
            }

            return embedding;
        }
    }
}
