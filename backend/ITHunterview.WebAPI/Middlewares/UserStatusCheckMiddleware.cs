using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Interface.Persistence;
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

        public async Task InvokeAsync(HttpContext context, IMemoryCache cache, IUserGovernanceUseCase userGovernanceUseCase, IAuditLogRepository auditLogRepository)
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
                            await LogBlockedUserAsync(context, userId, "DELETED", "Tài khoản không tồn tại hoặc đã bị xóa.", auditLogRepository);
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            await context.Response.WriteAsJsonAsync(new { message = "Tài khoản không hợp lệ hoặc đã bị xóa." });
                            return;
                        }
                        
                        status = dbStatus.Value;
                        cache.Set(cacheKey, status, TimeSpan.FromSeconds(30));
                    }

                    if (status == UserStatus.BANNED || status == UserStatus.INACTIVE)
                    {
                        var reason = status == UserStatus.BANNED ? "BANNED" : "INACTIVE";
                        await LogBlockedUserAsync(context, userId, reason, $"Tài khoản ở trạng thái {reason} cố gắng truy cập.", auditLogRepository);
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(new { message = "Tài khoản của bạn đã bị khóa hoặc ngừng hoạt động." });
                        return;
                    }
                }
            }

            await _next(context);
        }

        private async Task LogBlockedUserAsync(HttpContext context, Guid userId, string statusType, string actionDescription, IAuditLogRepository auditLogRepository)
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

                var log = new UserActivityLogs
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ActorRole = roleClaim,
                    ActionCategory = ActivityLogCategory.SECURITY,
                    ActorEmail = emailClaim,
                    Action = $"[CHẶN TRUY CẬP - {statusType}] {actionDescription}",
                    Status = ActivityLogStatus.FAIL,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    CreatedAt = DateTime.UtcNow
                };

                await auditLogRepository.AddActivityLogAsync(log);
            }
            catch
            {
                // Tránh ảnh hưởng đến tiến trình response
            }
        }
    }
}
