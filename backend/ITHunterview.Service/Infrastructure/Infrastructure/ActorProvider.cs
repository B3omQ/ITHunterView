using System;
using System.Linq;
using System.Security.Claims;
using ITHunterview.Service.Interface.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace ITHunterview.Service.Infrastructure.Infrastructure
{
    public class ActorProvider : IActorProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private Guid? _overriddenUserId;
        private string? _overriddenEmail;
        private string? _overriddenRole;
        private string? _overriddenIp;
        private string? _overriddenUserAgent;
        private bool _isOverridden;

        public ActorProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? ActorUserId
        {
            get
            {
                if (_isOverridden) return _overriddenUserId;
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null) return null;

                var userIdClaim = httpContext.User.FindFirst("userId")?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedId))
                {
                    return parsedId;
                }
                return null;
            }
        }

        public string ActorEmail
        {
            get
            {
                if (_isOverridden) return _overriddenEmail ?? "system";
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null) return "system";

                return httpContext.User.FindFirst(ClaimTypes.Email)?.Value ??
                       httpContext.User.FindFirst("email")?.Value ?? "system";
            }
        }

        public string ActorRole
        {
            get
            {
                if (_isOverridden) return _overriddenRole ?? "system";
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null) return "system";

                return httpContext.User.FindFirst(ClaimTypes.Role)?.Value ??
                       httpContext.User.FindFirst("role")?.Value ?? "system";
            }
        }

        public string IpAddress
        {
            get
            {
                if (_isOverridden) return _overriddenIp ?? "unknown";
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null) return "unknown";

                var xForwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrEmpty(xForwardedFor))
                {
                    var ip = xForwardedFor.Split(',').FirstOrDefault()?.Trim();
                    if (!string.IsNullOrEmpty(ip)) return ip;
                }

                return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            }
        }

        public string UserAgent
        {
            get
            {
                if (_isOverridden) return _overriddenUserAgent ?? "unknown";
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null) return "unknown";

                var rawUserAgent = httpContext.Request.Headers["User-Agent"].ToString();
                var userAgent = string.IsNullOrEmpty(rawUserAgent) ? "unknown" : rawUserAgent;
                var fingerprint = httpContext.Request.Headers["X-Device-Fingerprint"].ToString();
                if (!string.IsNullOrEmpty(fingerprint))
                {
                    userAgent = $"{userAgent} [Fingerprint: {fingerprint}]";
                }
                return userAgent;
            }
        }

        public void SetActor(Guid userId, string email, string role, string ipAddress = "system", string userAgent = "system")
        {
            _overriddenUserId = userId;
            _overriddenEmail = email;
            _overriddenRole = role;
            _overriddenIp = ipAddress;
            _overriddenUserAgent = userAgent;
            _isOverridden = true;
        }
    }
}
