using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Interface.Infrastructure;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace ITHunterview.WebAPI.Middlewares
{
    public class UserStatusCheckMiddleware
    {
        private readonly RequestDelegate _next;

        public UserStatusCheckMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IMemoryCache cache, IUserGovernanceUseCase userGovernanceUseCase, IAuditLogQueue auditLogQueue)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userIdStr = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("userId");
                if (Guid.TryParse(userIdStr, out var userId))
                {
                    var cacheKey = $"user-status-{userId}";
                    
                    if (!cache.TryGetValue<UserStatus>(cacheKey, out var status))
                    {
                        var dbStatus = await userGovernanceUseCase.GetUserStatusAsync(userId);
                        if (dbStatus == null)
                        {
                            LogBlockedUser(context, userId, "DELETED", "Account does not exist or has been deleted.", auditLogQueue);
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            await context.Response.WriteAsJsonAsync(new { message = "Invalid account or account has been deleted." });
                            return;
                        }
                        
                        
                        status = dbStatus.Value;
                        cache.Set(cacheKey, status, TimeSpan.FromSeconds(30));
                    }

                    if (status == UserStatus.BANNED || status == UserStatus.INACTIVE)
                    {
                        var reason = status == UserStatus.BANNED ? "BANNED" : "INACTIVE";
                        LogBlockedUser(context, userId, reason, $"Account in status {reason} attempted to access.", auditLogQueue);
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(new { message = "Your account has been banned or deactivated." });
                        return;
                    }
                }
            }

            await _next(context);
        }

        private void LogBlockedUser(HttpContext context, Guid userId, string statusType, string actionDescription, IAuditLogQueue auditLogQueue)
        {
            try
            {
                var emailClaim = context.User.FindFirst(ClaimTypes.Email)?.Value ?? context.User.FindFirst("email")?.Value ?? "unknown";
                var roleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value ?? context.User.FindFirst("role")?.Value ?? "anonymous";
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var userAgent = context.Request.Headers["User-Agent"].ToString();
                userAgent = string.IsNullOrEmpty(userAgent) ? "unknown" : userAgent;
                var fingerprint = context.Request.Headers["X-Device-Fingerprint"].ToString();
                if (!string.IsNullOrEmpty(fingerprint))
                {
                    userAgent = $"{userAgent} [Fingerprint: {fingerprint}]";
                }

                var log = UserActivityLogs.Create(
                    userId: userId,
                    actorRole: roleClaim,
                    category: ActivityLogCategory.SECURITY,
                    actorEmail: emailClaim,
                    action: $"[ACCESS BLOCKED - {statusType}] {actionDescription}",
                    status: ActivityLogStatus.FAIL,
                    ipAddress: ipAddress,
                    userAgent: userAgent
                );

                auditLogQueue.TryEnqueue(log);
            }
            catch
            {
                // Avoid affecting the response process
            }
        }
    }
}
