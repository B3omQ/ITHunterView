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

            var rateLimitConfig = await _systemConfigRepository.GetByKeyAsync("AiRateLimit");
            int rpm = 60; // Default
            if (rateLimitConfig != null && int.TryParse(rateLimitConfig.ConfigValue, out var parsed))
            {
                rpm = parsed;
            }

            var response = new AiConfigResponseDto
            {
                ActiveProvider = activeProvider,
                RequestsPerMinute = rpm
            };

            foreach (var kvp in _settings.Providers)
            {
                // Check if API key is in DB first
                var dbKeyConfig = await _systemConfigRepository.GetByKeyAsync($"AiApiKey_{kvp.Key}");
                var apiKey = dbKeyConfig?.ConfigValue ?? kvp.Value.ApiKey;

                var isConfigured = !string.IsNullOrEmpty(apiKey) && 
                                   !apiKey.StartsWith("YOUR_") && 
                                   !apiKey.Contains("your-api-key");

                string preview = "";
                if (isConfigured && apiKey.Length > 8)
                {
                    preview = apiKey.Substring(0, 3) + "***" + apiKey.Substring(apiKey.Length - 4);
                }

                response.AvailableProviders.Add(new AiProviderConfigDto
                {
                    ProviderName = kvp.Key,
                    Model = kvp.Value.Model,
                    IsConfigured = isConfigured,
                    ApiKeyPreview = preview
                });
            }

            return response;
        }

        public async Task UpdateAiConfigAsync(Guid userId, UpdateAiConfigRequestDto dto)
        {
            // Validate the provider name exists/is supported
            var provider = _providerFactory.GetProvider(dto.ProviderName);

            // Update Active Provider
            var config = new SystemConfigs
            {
                ConfigKey = "ActiveAiProvider",
                ConfigValue = provider.ProviderName,
                Description = "Currently active AI model provider for the system (Gemini, OpenAI, Claude).",
                UpdatedBy = userId
            };
            await _systemConfigRepository.SaveAsync(config);

            // Update Rate Limit
            if (dto.RequestsPerMinute > 0)
            {
                var rateLimitConfig = new SystemConfigs
                {
                    ConfigKey = "AiRateLimit",
                    ConfigValue = dto.RequestsPerMinute.ToString(),
                    Description = "Number of AI requests allowed per minute per user/IP.",
                    UpdatedBy = userId
                };
                await _systemConfigRepository.SaveAsync(rateLimitConfig);
            }

            // Update API Key if provided
            if (!string.IsNullOrWhiteSpace(dto.ApiKey) && !dto.ApiKey.Contains("***"))
            {
                var apiKeyConfig = new SystemConfigs
                {
                    ConfigKey = $"AiApiKey_{provider.ProviderName}",
                    ConfigValue = dto.ApiKey,
                    Description = $"API Key for {provider.ProviderName}",
                    UpdatedBy = userId
                };
                await _systemConfigRepository.SaveAsync(apiKeyConfig);
            }
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
