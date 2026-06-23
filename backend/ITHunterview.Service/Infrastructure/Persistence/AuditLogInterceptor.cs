using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Interface.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class AuditLogInterceptor : SaveChangesInterceptor
    {
        private readonly IActorProvider _actorProvider;
        private readonly IAuditLogQueue _auditLogQueue;

        private static readonly HashSet<string> SensitiveFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "PasswordHash", "Password", "Token", "Secret", "PrivateKey", "AccessToken", "RefreshToken",
            "OtpCode", "VerificationToken", "ResetToken", "SocialId", "VerifyToken"
        };

        private static readonly HashSet<Type> ExcludedTypes = new()
        {
            typeof(UserActivityLogs),
            typeof(RefreshToken),
            typeof(EmailVerificationTokens),
            typeof(PasswordResets),
            typeof(Notifications),
            typeof(SysEmailLogs),
            typeof(AiApiUsageLogs),
            typeof(UserWallets),
            typeof(CreditTransactions)
        };

        private readonly AsyncLocal<List<UserActivityLogs>> _pendingLogs = new();

        public AuditLogInterceptor(IActorProvider actorProvider, IAuditLogQueue auditLogQueue)
        {
            _actorProvider = actorProvider;
            _auditLogQueue = auditLogQueue;
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            if (eventData.Context == null)
            {
                return base.SavingChanges(eventData, result);
            }

            var context = eventData.Context;
            var entries = context.ChangeTracker.Entries()
                .Where(e => !ExcludedTypes.Contains(e.Entity.GetType()) &&
                            (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
                .ToList();

            if (!entries.Any())
            {
                return base.SavingChanges(eventData, result);
            }

            var actorUserId = _actorProvider.ActorUserId;
            var actorEmail = _actorProvider.ActorEmail;
            var actorRole = _actorProvider.ActorRole;
            var ipAddress = _actorProvider.IpAddress;
            var userAgent = _actorProvider.UserAgent;

            var logs = new List<UserActivityLogs>();
            var utcNow = DateTime.UtcNow;

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    var createdAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedAt");
                    if (createdAtProp != null && (createdAtProp.CurrentValue == null || (DateTime)createdAtProp.CurrentValue == default))
                        createdAtProp.CurrentValue = utcNow;

                    var createdByProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedBy");
                    if (createdByProp != null && createdByProp.CurrentValue == null && actorUserId.HasValue)
                        createdByProp.CurrentValue = actorUserId.Value;
                }
                else if (entry.State == EntityState.Modified)
                {
                    var updatedAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
                    if (updatedAtProp != null)
                        updatedAtProp.CurrentValue = utcNow;

                    var updatedByProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedBy");
                    if (updatedByProp != null && actorUserId.HasValue)
                        updatedByProp.CurrentValue = actorUserId.Value;
                }

                var tableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name;
                var operationType = entry.State.ToString().ToUpper();
                if (operationType == "ADDED") operationType = "CREATE";
                if (operationType == "MODIFIED") operationType = "UPDATE";
                if (operationType == "DELETED") operationType = "DELETE";

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
                _pendingLogs.Value = logs;
            }

            return base.SavingChanges(eventData, result);
        }

        public override int SavedChanges(
            SaveChangesCompletedEventData eventData,
            int result)
        {
            var logs = _pendingLogs.Value;
            if (logs != null)
            {
                foreach (var log in logs)
                {
                    log.Status = ActivityLogStatus.SUCCESS;
                    _auditLogQueue.QueueBackgroundWorkItem(log);
                }
                _pendingLogs.Value = null;
            }
            return base.SavedChanges(eventData, result);
        }

        public override void SaveChangesFailed(
            DbContextErrorEventData eventData)
        {
            var logs = _pendingLogs.Value;
            if (logs != null)
            {
                foreach (var log in logs)
                {
                    log.Status = ActivityLogStatus.FAIL;
                    log.Action = $"{log.Action} [Lỗi: {eventData.Exception.Message}]";
                    _auditLogQueue.QueueBackgroundWorkItem(log);
                }
                _pendingLogs.Value = null;
            }
            base.SaveChangesFailed(eventData);
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
                .Where(e => !ExcludedTypes.Contains(e.Entity.GetType()) &&
                            (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
                .ToList();

            if (!entries.Any())
            {
                return base.SavingChangesAsync(eventData, result, cancellationToken);
            }

            var actorUserId = _actorProvider.ActorUserId;
            var actorEmail = _actorProvider.ActorEmail;
            var actorRole = _actorProvider.ActorRole;
            var ipAddress = _actorProvider.IpAddress;
            var userAgent = _actorProvider.UserAgent;

            var logs = new List<UserActivityLogs>();
            var utcNow = DateTime.UtcNow;

            foreach (var entry in entries)
            {
                // Tự động gán các trường Audit (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)
                if (entry.State == EntityState.Added)
                {
                    var createdAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedAt");
                    if (createdAtProp != null && (createdAtProp.CurrentValue == null || (DateTime)createdAtProp.CurrentValue == default))
                        createdAtProp.CurrentValue = utcNow;

                    var createdByProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedBy");
                    if (createdByProp != null && createdByProp.CurrentValue == null && actorUserId.HasValue)
                        createdByProp.CurrentValue = actorUserId.Value;
                }
                else if (entry.State == EntityState.Modified)
                {
                    var updatedAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
                    if (updatedAtProp != null)
                        updatedAtProp.CurrentValue = utcNow;

                    var updatedByProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedBy");
                    if (updatedByProp != null && actorUserId.HasValue)
                        updatedByProp.CurrentValue = actorUserId.Value;
                }

                var tableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name;
                var operationType = entry.State.ToString().ToUpper();
                if (operationType == "ADDED") operationType = "CREATE";
                if (operationType == "MODIFIED") operationType = "UPDATE";
                if (operationType == "DELETED") operationType = "DELETE";

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
                _pendingLogs.Value = logs;
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            var logs = _pendingLogs.Value;
            if (logs != null)
            {
                foreach (var log in logs)
                {
                    log.Status = ActivityLogStatus.SUCCESS;
                    _auditLogQueue.QueueBackgroundWorkItem(log);
                }
                _pendingLogs.Value = null;
            }
            return base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        public override Task SaveChangesFailedAsync(
            DbContextErrorEventData eventData,
            CancellationToken cancellationToken = default)
        {
            var logs = _pendingLogs.Value;
            if (logs != null)
            {
                foreach (var log in logs)
                {
                    log.Status = ActivityLogStatus.FAIL;
                    log.Action = $"{log.Action} [Lỗi: {eventData.Exception.Message}]";
                    _auditLogQueue.QueueBackgroundWorkItem(log);
                }
                _pendingLogs.Value = null;
            }
            return base.SaveChangesFailedAsync(eventData, cancellationToken);
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
                    var keyValues = entry.Properties
                        .Where(p => p.Metadata.IsPrimaryKey())
                        .ToDictionary(
                            p => p.Metadata.Name,
                            p => MaskSensitiveData(p.Metadata.Name, p.CurrentValue)
                        );

                    var changes = entry.Properties
                        .Where(p => p.IsModified && !p.Metadata.IsPrimaryKey())
                        .ToDictionary(
                            p => p.Metadata.Name,
                            p => new
                            {
                                old = MaskSensitiveData(p.Metadata.Name, p.OriginalValue),
                                @new = MaskSensitiveData(p.Metadata.Name, p.CurrentValue)
                            }
                        );
                    return JsonSerializer.Serialize(new { keys = keyValues, changes }, options);
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

            if (value is string strValue && (strValue.StartsWith("{") || strValue.StartsWith("[")))
            {
                try
                {
                    var node = JsonNode.Parse(strValue);
                    if (node != null)
                    {
                        MaskJsonNode(node);
                        return node.ToJsonString();
                    }
                }
                catch
                {
                    // Fallback
                }
            }

            return value;
        }

        private void MaskJsonNode(JsonNode node)
        {
            if (node is JsonObject obj)
            {
                var propertiesToUpdate = new List<(string Key, JsonNode? Node)>();
                foreach (var kvp in obj)
                {
                    if (kvp.Value == null) continue;

                    if (SensitiveFields.Contains(kvp.Key))
                    {
                        propertiesToUpdate.Add((kvp.Key, JsonValue.Create("[PROTECTED]")));
                    }
                    else
                    {
                        MaskJsonNode(kvp.Value);
                    }
                }

                foreach (var prop in propertiesToUpdate)
                {
                    obj[prop.Key] = prop.Node;
                }
            }
            else if (node is JsonArray array)
            {
                foreach (var item in array)
                {
                    if (item != null)
                    {
                        MaskJsonNode(item);
                    }
                }
            }
        }
    }
}
