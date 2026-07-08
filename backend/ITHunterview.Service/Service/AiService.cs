using System;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Config;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace ITHunterview.Service.Service
{
    public class AiService : IAiService
    {
        private readonly IAiProviderFactory _providerFactory;
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly AiSettings _settings;
        private readonly ITHunterviewContext _context;

        public AiService(
            IAiProviderFactory providerFactory,
            ISystemConfigRepository systemConfigRepository,
            IOptions<AiSettings> settings,
            ITHunterviewContext context)
        {
            _providerFactory = providerFactory;
            _systemConfigRepository = systemConfigRepository;
            _settings = settings.Value;
            _context = context;
        }

        public async Task<string> GetActiveProviderNameAsync()
        {
            var config = await _systemConfigRepository.GetByKeyAsync("ActiveAiProvider");
            if (config != null && !string.IsNullOrWhiteSpace(config.ConfigValue))
            {
                return config.ConfigValue;
            }

            return _settings.DefaultProvider ?? "Gemini";
        }

        public async Task<string> GenerateTextAsync(string prompt, string systemPrompt = null, string providerName = null)
        {
            var activeProviderName = string.IsNullOrWhiteSpace(providerName)
                ? await GetActiveProviderNameAsync()
                : providerName;
            var provider = _providerFactory.GetProvider(activeProviderName);

            string result = null;
            var startTime = DateTime.UtcNow;

            try
            {
                result = await provider.GenerateTextAsync(prompt, systemPrompt);
                return result;
            }
            finally
            {
                // Write a log to database (fire and forget or async)
                try
                {
                    var log = new AiApiUsageLogs
                    {
                        Id = Guid.NewGuid(),
                        ModelName = $"{provider.ProviderName}",
                        PromptTokens = prompt?.Length / 4, // Rough estimation of tokens
                        CompletionTokens = result?.Length / 4 ?? 0,
                        CostUsd = 0.00m,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.AiApiUsageLogs.Add(log);
                    await _context.SaveChangesAsync();
                }
                catch
                {
                    // Fail silently so it doesn't block the main flow
                }
            }
        }
    }
}
