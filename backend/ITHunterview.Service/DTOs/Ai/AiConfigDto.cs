using System.Collections.Generic;

namespace ITHunterview.Service.DTOs.Ai
{
    public class AiConfigResponseDto
    {
        public string ActiveProvider { get; set; }
        public int RequestsPerMinute { get; set; }
        public List<AiProviderConfigDto> AvailableProviders { get; set; } = new();
    }

    public class AiProviderConfigDto
    {
        public string ProviderName { get; set; }
        public string Model { get; set; }
        public bool IsConfigured { get; set; }
        public string ApiKeyPreview { get; set; } // Example: "sk-***"
    }

    public class UpdateAiConfigRequestDto
    {
        public string ProviderName { get; set; }
        public int RequestsPerMinute { get; set; }
        public string ApiKey { get; set; } // Optional, only set when updating
    }

    public class TestConnectionRequestDto
    {
        public string ProviderName { get; set; }
        public string Prompt { get; set; } = "Hello";
    }

    public class TestConnectionResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ResponseText { get; set; }
        public long ResponseTimeMs { get; set; }
    }
}
