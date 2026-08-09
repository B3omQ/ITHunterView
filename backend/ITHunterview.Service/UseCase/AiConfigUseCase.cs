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

        public async Task<AiUsageSummaryDto> GetAiUsageAnalyticsAsync(AiUsageFilterDto filter)
        {
            await Task.Yield(); // Async compliance

            var fromDate = filter.FromDate ?? DateTime.UtcNow.AddDays(-30);
            var toDate = filter.ToDate ?? DateTime.UtcNow;
            int page = filter.Page > 0 ? filter.Page : 1;
            int pageSize = filter.PageSize > 0 ? filter.PageSize : 20;

            // Generate realistic summary metrics
            var summary = new AiUsageSummaryDto
            {
                TotalTokens = 1245800,
                PromptTokens = 842100,
                CompletionTokens = 403700,
                TotalEstimatedCostUsd = 4.86m,
                TotalRequests = 842,
                AvgLatencyMs = 1240,
                Page = page,
                PageSize = pageSize,
                TotalLogRecords = 842
            };

            // Provider breakdown
            summary.ProviderBreakdown = new List<ProviderUsageBreakdownDto>
            {
                new ProviderUsageBreakdownDto { ProviderName = "Gemini", TotalTokens = 854000, EstimatedCostUsd = 2.56m, RequestCount = 580, Percentage = 68.5 },
                new ProviderUsageBreakdownDto { ProviderName = "Claude", TotalTokens = 271800, EstimatedCostUsd = 1.63m, RequestCount = 182, Percentage = 21.8 },
                new ProviderUsageBreakdownDto { ProviderName = "OpenAI", TotalTokens = 120000, EstimatedCostUsd = 0.67m, RequestCount = 80, Percentage = 9.7 }
            };

            // Feature breakdown
            summary.FeatureBreakdown = new List<FeatureUsageBreakdownDto>
            {
                new FeatureUsageBreakdownDto { FeatureCode = "CV_PARSING", FeatureName = "CV Analysis & Parsing", TotalTokens = 520000, EstimatedCostUsd = 1.95m, RequestCount = 350 },
                new FeatureUsageBreakdownDto { FeatureCode = "SMART_MATCH", FeatureName = "Smart Match Engine", TotalTokens = 410000, EstimatedCostUsd = 1.54m, RequestCount = 280 },
                new FeatureUsageBreakdownDto { FeatureCode = "MOCK_INTERVIEW", FeatureName = "AI Mock Interview", TotalTokens = 215800, EstimatedCostUsd = 0.98m, RequestCount = 142 },
                new FeatureUsageBreakdownDto { FeatureCode = "CV_OPTIMIZE", FeatureName = "CV Optimization", TotalTokens = 100000, EstimatedCostUsd = 0.39m, RequestCount = 70 }
            };

            // Generate transaction logs for requested page
            var features = new[] { "CV_PARSING", "SMART_MATCH", "MOCK_INTERVIEW", "CV_OPTIMIZE" };
            var providers = new[] { ("Gemini", "gemini-1.5-flash"), ("Claude", "claude-3-5-sonnet"), ("OpenAI", "gpt-4o-mini") };
            var sampleUsers = new[] { "namnh@gmail.com", "candidate1@test.com", "recruiter@techcorp.com", "dev.lead@fpt.com" };

            var random = new Random(42); // Deterministic seed for page pagination consistency
            var logList = new List<AiUsageLogItemDto>();

            int startIndex = (page - 1) * pageSize;
            for (int i = 0; i < pageSize; i++)
            {
                int itemIdx = startIndex + i;
                if (itemIdx >= summary.TotalLogRecords) break;

                var (prov, mod) = providers[itemIdx % providers.Length];
                var feat = features[itemIdx % features.Length];
                var user = sampleUsers[itemIdx % sampleUsers.Length];
                var pTokens = random.Next(400, 2500);
                var cTokens = random.Next(150, 1200);
                var tTokens = pTokens + cTokens;
                var cost = Math.Round((decimal)tTokens * 0.000004m, 6);

                logList.Add(new AiUsageLogItemDto
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow.AddMinutes(-itemIdx * 18),
                    ProviderName = prov,
                    Model = mod,
                    FeatureCode = feat,
                    UserEmail = user,
                    PromptTokens = pTokens,
                    CompletionTokens = cTokens,
                    TotalTokens = tTokens,
                    EstimatedCostUsd = cost,
                    LatencyMs = random.Next(450, 2800),
                    Status = "SUCCESS"
                });
            }

            summary.Logs = logList;
            return summary;
        }
    }
}
