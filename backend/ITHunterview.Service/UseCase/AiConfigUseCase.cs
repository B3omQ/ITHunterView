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

using Microsoft.EntityFrameworkCore;
using ITHunterview.Service.Infrastructure.Persistence;

namespace ITHunterview.Service.UseCase
{
    public class AiConfigUseCase : IAiConfigUseCase
    {
        private readonly IAiProviderFactory _providerFactory;
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly AiSettings _settings;
        private readonly ITHunterviewContext _context;

        public AiConfigUseCase(
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
            var fromDate = filter.FromDate ?? DateTime.UtcNow.AddDays(-30);
            var toDate = filter.ToDate ?? DateTime.UtcNow;
            int page = filter.Page > 0 ? filter.Page : 1;
            int pageSize = filter.PageSize > 0 ? filter.PageSize : 20;

            var query = _context.AiApiUsageLogs
                .Where(x => x.CreatedAt >= fromDate && x.CreatedAt <= toDate);

            if (!string.IsNullOrWhiteSpace(filter.ProviderName) && filter.ProviderName != "ALL")
            {
                query = query.Where(x => x.ProviderName == filter.ProviderName);
            }

            var totalRecords = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            var logs = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(log => new
                {
                    Log = log,
                    UserEmail = _context.Users.Where(u => u.Id == log.UserId).Select(u => u.Email).FirstOrDefault() ?? "unknown"
                })
                .ToListAsync();

            var logList = logs.Select(x => new AiUsageLogItemDto
            {
                Id = x.Log.Id,
                CreatedAt = x.Log.CreatedAt,
                ProviderName = x.Log.ProviderName,
                Model = x.Log.ModelName,
                FeatureCode = string.IsNullOrWhiteSpace(x.Log.FeatureCode) ? "GENERAL" : x.Log.FeatureCode,
                UserEmail = x.UserEmail,
                PromptTokens = x.Log.PromptTokens ?? 0,
                CompletionTokens = x.Log.CompletionTokens ?? 0,
                TotalTokens = (x.Log.PromptTokens ?? 0) + (x.Log.CompletionTokens ?? 0),
                EstimatedCostUsd = x.Log.CostUsd ?? 0m,
                LatencyMs = x.Log.LatencyMs ?? 0,
                Status = string.IsNullOrWhiteSpace(x.Log.Status) ? "UNKNOWN" : x.Log.Status
            }).ToList();

            var allQuery = _context.AiApiUsageLogs.Where(x => x.CreatedAt >= fromDate && x.CreatedAt <= toDate);
            var totalTokens = await allQuery.SumAsync(x => (x.PromptTokens ?? 0) + (x.CompletionTokens ?? 0));
            var promptTokens = await allQuery.SumAsync(x => x.PromptTokens ?? 0);
            var compTokens = await allQuery.SumAsync(x => x.CompletionTokens ?? 0);
            var totalCost = await allQuery.SumAsync(x => x.CostUsd ?? 0m);
            var totalRequests = await allQuery.CountAsync();
            var avgLatency = totalRequests > 0 ? (int)await allQuery.AverageAsync(x => x.LatencyMs ?? 0) : 0;

            var providerStats = await allQuery
                .GroupBy(x => x.ProviderName)
                .Select(g => new
                {
                    ProviderName = g.Key,
                    TotalTokens = g.Sum(x => (x.PromptTokens ?? 0) + (x.CompletionTokens ?? 0)),
                    EstimatedCostUsd = g.Sum(x => x.CostUsd ?? 0m),
                    RequestCount = g.Count()
                }).ToListAsync();

            var featureStats = await allQuery
                .GroupBy(x => string.IsNullOrWhiteSpace(x.FeatureCode) ? "GENERAL" : x.FeatureCode)
                .Select(g => new
                {
                    FeatureCode = g.Key,
                    TotalTokens = g.Sum(x => (x.PromptTokens ?? 0) + (x.CompletionTokens ?? 0)),
                    EstimatedCostUsd = g.Sum(x => x.CostUsd ?? 0m),
                    RequestCount = g.Count()
                }).ToListAsync();

            var providerBreakdown = providerStats.Select(x => new ProviderUsageBreakdownDto
            {
                ProviderName = x.ProviderName,
                TotalTokens = x.TotalTokens,
                EstimatedCostUsd = x.EstimatedCostUsd,
                RequestCount = x.RequestCount,
                Percentage = totalTokens > 0 ? Math.Round((double)x.TotalTokens / totalTokens * 100, 2) : 0
            }).ToList();

            var featureBreakdown = featureStats.Select(x => new FeatureUsageBreakdownDto
            {
                FeatureCode = x.FeatureCode,
                FeatureName = x.FeatureCode, // Can map to friendly names if needed
                TotalTokens = x.TotalTokens,
                EstimatedCostUsd = x.EstimatedCostUsd,
                RequestCount = x.RequestCount
            }).ToList();

            return new AiUsageSummaryDto
            {
                TotalTokens = totalTokens,
                PromptTokens = promptTokens,
                CompletionTokens = compTokens,
                TotalEstimatedCostUsd = totalCost,
                TotalRequests = totalRequests,
                AvgLatencyMs = avgLatency,
                Page = page,
                PageSize = pageSize,
                TotalLogRecords = totalRecords,
                ProviderBreakdown = providerBreakdown,
                FeatureBreakdown = featureBreakdown,
                Logs = logList
            };
        }
    }
}
