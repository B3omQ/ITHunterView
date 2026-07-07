using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Config;
using ITHunterview.Service.DTOs.Ai;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.Interface.Service;
using Microsoft.Extensions.Options;

namespace ITHunterview.Service.UseCase
{
    public class AiConfigUseCase : IAiConfigUseCase
    {
        private readonly IAiProviderFactory _providerFactory;
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly AiSettings _settings;

        public AiConfigUseCase(
            IAiProviderFactory providerFactory,
            ISystemConfigRepository systemConfigRepository,
            IOptions<AiSettings> settings)
        {
            _providerFactory = providerFactory;
            _systemConfigRepository = systemConfigRepository;
            _settings = settings.Value;
        }

        public async Task<AiConfigResponseDto> GetAiConfigAsync()
        {
            var activeConfig = await _systemConfigRepository.GetByKeyAsync("ActiveAiProvider");
            var activeProvider = activeConfig?.ConfigValue ?? _settings.DefaultProvider ?? "Gemini";

            var response = new AiConfigResponseDto
            {
                ActiveProvider = activeProvider
            };

            foreach (var kvp in _settings.Providers)
            {
                var isConfigured = !string.IsNullOrEmpty(kvp.Value.ApiKey) && 
                                   !kvp.Value.ApiKey.StartsWith("YOUR_") && 
                                   !kvp.Value.ApiKey.Contains("your-api-key");

                response.AvailableProviders.Add(new AiProviderConfigDto
                {
                    ProviderName = kvp.Key,
                    Model = kvp.Value.Model,
                    IsConfigured = isConfigured
                });
            }

            return response;
        }

        public async Task UpdateActiveProviderAsync(Guid userId, string providerName)
        {
            // Validate the provider name exists/is supported
            var provider = _providerFactory.GetProvider(providerName);

            var config = new SystemConfigs
            {
                ConfigKey = "ActiveAiProvider",
                ConfigValue = provider.ProviderName,
                Description = "Currently active AI model provider for the system (Gemini, OpenAI, Claude).",
                UpdatedBy = userId
            };

            await _systemConfigRepository.SaveAsync(config);
        }

        public async Task<TestConnectionResponseDto> TestConnectionAsync(string providerName, string prompt)
        {
            var provider = _providerFactory.GetProvider(providerName);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var systemPrompt = "You are an API connection health check assistant. Keep response under 10 words.";
                var responseText = await provider.GenerateTextAsync(prompt, systemPrompt);
                stopwatch.Stop();

                return new TestConnectionResponseDto
                {
                    Success = true,
                    Message = "Successfully connected to provider API.",
                    ResponseText = responseText,
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new TestConnectionResponseDto
                {
                    Success = false,
                    Message = ex.Message,
                    ResponseText = string.Empty,
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
        }
    }
}
