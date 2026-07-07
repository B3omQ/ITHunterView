using System.Collections.Generic;

namespace ITHunterview.Service.DTOs.Ai
{
    public class AiConfigResponseDto
    {
        public string ActiveProvider { get; set; }
        public List<AiProviderConfigDto> AvailableProviders { get; set; } = new();
    }

    public class AiProviderConfigDto
    {
        public string ProviderName { get; set; }
        public string Model { get; set; }
        public bool IsConfigured { get; set; }
    }

    public class UpdateActiveProviderRequestDto
    {
        public string ProviderName { get; set; }
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
