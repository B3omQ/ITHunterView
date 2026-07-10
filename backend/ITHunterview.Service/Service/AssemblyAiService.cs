using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ITHunterview.Service.Interface.Service;
using Microsoft.Extensions.Configuration;

namespace ITHunterview.Service.Service
{
    public class AssemblyAiService : ISpeechToTextService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public AssemblyAiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["AssemblyAi:ApiKey"] ?? string.Empty;
        }

        public async Task<string> TranscribeAudioAsync(byte[] audioBytes, string contentType, string? languageCode = "vi")
        {
            if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey == "YOUR_ASSEMBLYAI_API_KEY")
            {
                throw new InvalidOperationException("AssemblyAI API Key is not configured in settings.");
            }

            // Step 1: Upload the audio bytes to AssemblyAI
            var uploadRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.assemblyai.com/v2/upload");
            uploadRequest.Headers.Add("authorization", _apiKey);
            uploadRequest.Content = new ByteArrayContent(audioBytes);
            uploadRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            var uploadResponse = await _httpClient.SendAsync(uploadRequest);
            if (!uploadResponse.IsSuccessStatusCode)
            {
                var errorContent = await uploadResponse.Content.ReadAsStringAsync();
                throw new HttpRequestException($"AssemblyAI upload failed: {uploadResponse.StatusCode} - {errorContent}");
            }

            var uploadResponseText = await uploadResponse.Content.ReadAsStringAsync();
            using var uploadDoc = JsonDocument.Parse(uploadResponseText);
            var uploadUrl = uploadDoc.RootElement.GetProperty("upload_url").GetString();

            if (string.IsNullOrEmpty(uploadUrl))
            {
                throw new Exception("AssemblyAI upload succeeded but did not return a valid upload_url.");
            }

            // Step 2: Request transcription
            var transcriptRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.assemblyai.com/v2/transcript");
            transcriptRequest.Headers.Add("authorization", _apiKey);

            var payload = new
            {
                audio_url = uploadUrl,
                language_code = languageCode ?? "vi"
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            transcriptRequest.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var transcriptResponse = await _httpClient.SendAsync(transcriptRequest);
            if (!transcriptResponse.IsSuccessStatusCode)
            {
                var errorContent = await transcriptResponse.Content.ReadAsStringAsync();
                throw new HttpRequestException($"AssemblyAI transcription request failed: {transcriptResponse.StatusCode} - {errorContent}");
            }

            var transcriptResponseText = await transcriptResponse.Content.ReadAsStringAsync();
            using var transcriptDoc = JsonDocument.Parse(transcriptResponseText);
            var transcriptId = transcriptDoc.RootElement.GetProperty("id").GetString();

            if (string.IsNullOrEmpty(transcriptId))
            {
                throw new Exception("AssemblyAI transcription request succeeded but did not return a valid transcript ID.");
            }

            // Step 3: Poll status until complete or error
            string status = "queued";
            string text = string.Empty;
            int maxRetries = 30; // 30 retries * 1.5 seconds = 45 seconds timeout
            int retryCount = 0;

            while ((status == "queued" || status == "processing") && retryCount < maxRetries)
            {
                await Task.Delay(1500); // Poll every 1.5 seconds

                var pollRequest = new HttpRequestMessage(HttpMethod.Get, $"https://api.assemblyai.com/v2/transcript/{transcriptId}");
                pollRequest.Headers.Add("authorization", _apiKey);

                var pollResponse = await _httpClient.SendAsync(pollRequest);
                if (!pollResponse.IsSuccessStatusCode)
                {
                    var errorContent = await pollResponse.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"AssemblyAI polling status failed: {pollResponse.StatusCode} - {errorContent}");
                }

                var pollResponseText = await pollResponse.Content.ReadAsStringAsync();
                using var pollDoc = JsonDocument.Parse(pollResponseText);
                var root = pollDoc.RootElement;

                status = root.GetProperty("status").GetString() ?? "queued";

                if (status == "completed")
                {
                    text = root.GetProperty("text").GetString() ?? string.Empty;
                    break;
                }
                else if (status == "error")
                {
                    var errorMsg = root.TryGetProperty("error", out var errProp) ? errProp.GetString() : "Unknown error";
                    throw new Exception($"AssemblyAI transcription failed: {errorMsg}");
                }

                retryCount++;
            }

            if (retryCount >= maxRetries)
            {
                throw new TimeoutException("AssemblyAI transcription polling timed out.");
            }

            return text;
        }
    }
}
