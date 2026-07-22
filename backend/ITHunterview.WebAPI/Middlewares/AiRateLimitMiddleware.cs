using System;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading.Tasks;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ITHunterview.WebAPI.Middlewares
{
    public class AiRateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AiRateLimitMiddleware> _logger;
        private readonly IMemoryCache _cache;
        
        // Default RPM if not set in DB
        private const int DefaultRequestsPerMinute = 60;
        
        public AiRateLimitMiddleware(RequestDelegate next, ILogger<AiRateLimitMiddleware> logger, IMemoryCache cache)
        {
            _next = next;
            _logger = logger;
            _cache = cache;
        }

        public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
        {
            // Only apply rate limit to specific AI endpoints (e.g., generate)
            // Test connection is usually for Admin and might not need strict limit, but we can apply it broadly to /api/ai
            if (!context.Request.Path.StartsWithSegments("/api/ai/generate", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            // Get userId or IP
            var userId = context.User.FindFirstValue("userId");
            var clientId = !string.IsNullOrEmpty(userId) ? userId : context.Connection.RemoteIpAddress?.ToString() ?? "unknown_client";
            var cacheKey = $"AiRateLimit_Count_{clientId}";

            // Fetch the dynamic rate limit from DB or Cache
            var rateLimitCacheKey = "AiRateLimit_Value";
            if (!_cache.TryGetValue(rateLimitCacheKey, out int currentLimit))
            {
                using var scope = serviceProvider.CreateScope();
                var configRepo = scope.ServiceProvider.GetRequiredService<ISystemConfigRepository>();
                var limitConfig = await configRepo.GetByKeyAsync("AiRateLimit");
                
                if (limitConfig != null && int.TryParse(limitConfig.ConfigValue, out int parsedLimit))
                {
                    currentLimit = parsedLimit;
                }
                else
                {
                    currentLimit = DefaultRequestsPerMinute;
                }
                
                // Cache the rate limit setting for 5 minutes
                _cache.Set(rateLimitCacheKey, currentLimit, TimeSpan.FromMinutes(5));
            }

            // Track request count
            var count = _cache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                return 0;
            });

            if (count >= currentLimit)
            {
                _logger.LogWarning($"AI Rate limit exceeded for client {clientId}. Limit: {currentLimit} RPM.");
                
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"success\": false, \"message\": \"Rate limit exceeded. Please try again later.\"}");
                return;
            }

            // Increment and save back
            _cache.Set(cacheKey, count + 1, TimeSpan.FromMinutes(1));

            await _next(context);
        }
    }
}
