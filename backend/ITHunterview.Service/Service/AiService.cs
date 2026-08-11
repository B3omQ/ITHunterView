using System;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Config;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace ITHunterview.Service.Service
{
    public class AiService : IAiService
    {
        private readonly IAiProviderFactory _providerFactory;
        private readonly Microsoft.Extensions.DependencyInjection.IServiceScopeFactory _scopeFactory;
        private readonly AiSettings _settings;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AiService(
            IAiProviderFactory providerFactory,
            Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory,
            IOptions<AiSettings> settings,
            IHttpContextAccessor httpContextAccessor)
        {
            _providerFactory = providerFactory;
            _scopeFactory = scopeFactory;
            _settings = settings.Value;
            _httpContextAccessor = httpContextAccessor;
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

        public async Task<string> GenerateTextAsync(string prompt, string systemPrompt = null, string providerName = null, string featureCode = "GENERAL_GENERATE")
            => await GenerateTextAsync(prompt, systemPrompt, providerName, null, CancellationToken.None, featureCode);

        public async Task<string> GenerateTextAsync(
            string prompt,
            string systemPrompt,
            string providerName,
            CancellationToken cancellationToken,
            string featureCode = "GENERAL_GENERATE")
            => await GenerateTextAsync(prompt, systemPrompt, providerName, null, cancellationToken, featureCode);

        public async Task<string> GenerateTextAsync(
            string prompt,
            string systemPrompt,
            string providerName,
            AiGenerationOptions? options,
            CancellationToken cancellationToken,
            string featureCode = "GENERAL_GENERATE")
        {
            cancellationToken.ThrowIfCancellationRequested();
            var activeProviderName = string.IsNullOrWhiteSpace(providerName)
                ? await GetActiveProviderNameAsync()
                : providerName;
            cancellationToken.ThrowIfCancellationRequested();
            var provider = _providerFactory.GetProvider(activeProviderName);

            Guid? userId = ITHunterview.Service.Utils.UserContext.CurrentUserId;
            
            if (userId == null)
            {
                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier") 
                               ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("userId")
                               ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");
                               
                if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedId))
                {
                    userId = parsedId;
                }
            }

            string result = null;
            string status = "SUCCESS";
            var stopwatch = Stopwatch.StartNew();

            try
            {
                result = options is null
                    ? await provider.GenerateTextAsync(prompt, systemPrompt, cancellationToken)
                    : await provider.GenerateTextAsync(prompt, systemPrompt, options, cancellationToken);
                return result;
            }
            catch
            {
                status = "ERROR";
                throw;
            }
            finally
            {
                stopwatch.Stop();
                var latencyMs = (int)stopwatch.ElapsedMilliseconds;

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ITHunterviewContext>();
                    
                    var pTokens = prompt?.Length / 4 ?? 0;
                    var cTokens = result?.Length / 4 ?? 0;
                    var tTokens = pTokens + cTokens;
                    var cost = Math.Round((decimal)tTokens * 0.000004m, 6);

                    var log = new AiApiUsageLogs
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        ProviderName = provider.ProviderName,
                        ModelName = provider.ProviderName, // You may want to fetch real model name here if available
                        FeatureCode = featureCode,
                        PromptTokens = pTokens,
                        CompletionTokens = cTokens,
                        CostUsd = cost,
                        LatencyMs = latencyMs,
                        Status = status,
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
