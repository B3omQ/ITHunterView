using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ITHunterview.Domain.Enums;
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

        public async Task InvokeAsync(HttpContext context, IMemoryCache cache, IUserGovernanceUseCase userGovernanceUseCase)
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
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            await context.Response.WriteAsJsonAsync(new { message = "Tài khoản không hợp lệ hoặc đã bị xóa." });
                            return;
                        }
                        
                        status = dbStatus.Value;
                        cache.Set(cacheKey, status, TimeSpan.FromSeconds(30));
                    }

                    if (status == UserStatus.BANNED || status == UserStatus.INACTIVE)
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(new { message = "Tài khoản của bạn đã bị khóa hoặc ngừng hoạt động." });
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}
