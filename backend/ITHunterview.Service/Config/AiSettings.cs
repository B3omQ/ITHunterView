using System.Collections.Generic;

namespace ITHunterview.Service.Config
{
    public class AiSettings
    {
        public string DefaultProvider { get; set; } = "Gemini";
        public Dictionary<string, ProviderConfig> Providers { get; set; } = new();
    }

    public class ProviderConfig
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
    }
}
