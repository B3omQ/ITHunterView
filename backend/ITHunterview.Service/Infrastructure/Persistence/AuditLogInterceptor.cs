using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class AuditLogInterceptor : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private static readonly HashSet<string> SensitiveFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "PasswordHash", "Password", "Token", "Secret", "PrivateKey", "AccessToken", "RefreshToken"
        };

        public AuditLogInterceptor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context == null)
            {
                return base.SavingChangesAsync(eventData, result, cancellationToken);
            }

            var context = eventData.Context;
            var entries = context.ChangeTracker.Entries()
                .Where(e => e.Entity.GetType() != typeof(UserActivityLogs) &&
                            (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
                .ToList();

            if (!entries.Any())
            {
                return base.SavingChangesAsync(eventData, result, cancellationToken);
            }

            // Trích xuất thông tin tác nhân từ HttpContext
            var httpContext = _httpContextAccessor.HttpContext;
            Guid? actorUserId = null;
            string actorEmail = "system";
            string actorRole = "system";
            string ipAddress = "unknown";
            string userAgent = "unknown";

            if (httpContext != null)
            {
                var userIdClaim = httpContext.User.FindFirst("userId")?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedId))
                {
                    actorUserId = parsedId;
                }
                actorEmail = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ??
                             httpContext.User.FindFirst("email")?.Value ?? "system";
                actorRole = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ??
                            httpContext.User.FindFirst("role")?.Value ?? "system";
                ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var rawUserAgent = httpContext.Request.Headers["User-Agent"].ToString();
                userAgent = string.IsNullOrEmpty(rawUserAgent) ? "unknown" : rawUserAgent;
                var fingerprint = httpContext.Request.Headers["X-Device-Fingerprint"].ToString();
                if (!string.IsNullOrEmpty(fingerprint))
                {
                    userAgent = $"{userAgent} [Fingerprint: {fingerprint}]";
                }
            }

            var logs = new List<UserActivityLogs>();

            foreach (var entry in entries)
            {
                var tableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name;
                var operationType = entry.State.ToString().ToUpper();
                if (operationType == "ADDED") operationType = "CREATE";
                if (operationType == "MODIFIED") operationType = "UPDATE";

                var snapshotDiff = GetSnapshotDiff(entry);

                var log = new UserActivityLogs
                {
                    Id = Guid.NewGuid(),
                    UserId = actorUserId,
                    ActorRole = actorRole,
                    ActionCategory = ActivityLogCategory.DATA_MUTATION,
                    ActorEmail = actorEmail,
                    Action = $"Thực hiện {operationType} trên bảng {tableName}",
                    Status = ActivityLogStatus.SUCCESS,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    CreatedAt = DateTime.UtcNow,
                    TableName = tableName,
                    OperationType = operationType,
                    SnapshotDiff = snapshotDiff
                };

                logs.Add(log);
            }

            if (logs.Any())
            {
                context.Set<UserActivityLogs>().AddRange(logs);
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private string GetSnapshotDiff(EntityEntry entry)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = false };

                if (entry.State == EntityState.Added)
                {
                    var values = entry.Properties.ToDictionary(
                        p => p.Metadata.Name,
                        p => MaskSensitiveData(p.Metadata.Name, p.CurrentValue)
                    );
                    return JsonSerializer.Serialize(new { values }, options);
                }
                else if (entry.State == EntityState.Deleted)
                {
                    var values = entry.Properties.ToDictionary(
                        p => p.Metadata.Name,
                        p => MaskSensitiveData(p.Metadata.Name, p.OriginalValue)
                    );
                    return JsonSerializer.Serialize(new { values }, options);
                }
                else if (entry.State == EntityState.Modified)
                {
                    var changes = entry.Properties
                        .Where(p => p.IsModified)
                        .ToDictionary(
                            p => p.Metadata.Name,
                            p => new
                            {
                                old = MaskSensitiveData(p.Metadata.Name, p.OriginalValue),
                                @new = MaskSensitiveData(p.Metadata.Name, p.CurrentValue)
                            }
                        );
                    return JsonSerializer.Serialize(new { changes }, options);
                }
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = "Không thể sinh snapshot diff: " + ex.Message });
            }
            return "{}";
        }

        private object? MaskSensitiveData(string fieldName, object? value)
        {
            if (value == null) return null;
            if (SensitiveFields.Contains(fieldName))
            {
                return "[PROTECTED]";
            }
            return value;
        }
    }
}
