using System;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Config;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace ITHunterview.Service.Service
{
    public class AiService : IAiService
    {
        private readonly IAiProviderFactory _providerFactory;
        private readonly Microsoft.Extensions.DependencyInjection.IServiceScopeFactory _scopeFactory;
        private readonly AiSettings _settings;

        public AiService(
            IAiProviderFactory providerFactory,
            Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory,
            IOptions<AiSettings> settings)
        {
            _providerFactory = providerFactory;
            _scopeFactory = scopeFactory;
            _settings = settings.Value;
        }

        public async Task<string> GetActiveProviderNameAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var systemConfigRepository = scope.ServiceProvider.GetRequiredService<ISystemConfigRepository>();
            var config = await systemConfigRepository.GetByKeyAsync("ActiveAiProvider");
            if (config != null && !string.IsNullOrWhiteSpace(config.ConfigValue))
            {
                return config.ConfigValue;
            }

            return _settings.DefaultProvider ?? "Gemini";
        }

        public async Task<string> GenerateTextAsync(string prompt, string systemPrompt = null, string providerName = null)
            => await GenerateTextAsync(prompt, systemPrompt, providerName, CancellationToken.None);

        public async Task<string> GenerateTextAsync(
            string prompt,
            string systemPrompt,
            string providerName,
            CancellationToken cancellationToken)
            => await GenerateTextAsync(prompt, systemPrompt, providerName, null, cancellationToken);

        public async Task<string> GenerateTextAsync(
            string prompt,
            string systemPrompt,
            string providerName,
            AiGenerationOptions? options,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var activeProviderName = string.IsNullOrWhiteSpace(providerName)
                ? await GetActiveProviderNameAsync()
                : providerName;
            cancellationToken.ThrowIfCancellationRequested();
            var provider = _providerFactory.GetProvider(activeProviderName);

            string result = null;
            var startTime = DateTime.UtcNow;

            try
            {
                result = options is null
                    ? await provider.GenerateTextAsync(prompt, systemPrompt, cancellationToken)
                    : await provider.GenerateTextAsync(prompt, systemPrompt, options, cancellationToken);
                return result;
            }
            finally
            {
                // Write a log to database (fire and forget or async)
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ITHunterviewContext>();
                    var log = new AiApiUsageLogs
                    {
                        Id = Guid.NewGuid(),
                        ModelName = $"{provider.ProviderName}",
                        PromptTokens = prompt?.Length / 4, // Rough estimation of tokens
                        CompletionTokens = result?.Length / 4 ?? 0,
                        CostUsd = 0.00m,
                        CreatedAt = DateTime.UtcNow
                    };

                    context.AiApiUsageLogs.Add(log);
                    await context.SaveChangesAsync();
                }
                catch
                {
                    // Fail silently so it doesn't block the main flow
                }
            }
        }
    }
}
