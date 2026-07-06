using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

        private static readonly ConditionalWeakTable<DbContext, List<AuditEntry>> PendingLogsTable = new();

        private class AuditEntry
        {
            public EntityEntry Entry { get; set; } = null!;
            public UserActivityLogs Log { get; set; } = null!;
            public EntityState OriginalState { get; set; }
        }

        public AuditLogInterceptor(IActorProvider actorProvider, IAuditLogQueue auditLogQueue)
        {
            _actorProvider = actorProvider;
            _auditLogQueue = auditLogQueue;
        }

        private List<AuditEntry> PrepareAuditLogs(DbContext context, DateTime utcNow)
        {
            var entries = context.ChangeTracker.Entries()
                .Where(e => !ExcludedTypes.Contains(e.Entity.GetType()) &&
                            (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
                .ToList();

            if (!entries.Any())
            {
                return new List<AuditEntry>();
            }

            var actorUserId = _actorProvider.ActorUserId;
            var actorEmail = _actorProvider.ActorEmail;
            var actorRole = _actorProvider.ActorRole;
            var ipAddress = _actorProvider.IpAddress;
            var userAgent = _actorProvider.UserAgent;

            var auditEntries = new List<AuditEntry>();

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

                // Only generate snapshot diff immediately for Modified and Deleted.
                // For Added, we defer generation to get the database-generated primary key.
                string? snapshotDiff = null;
                string actionMsg;
                if (entry.State != EntityState.Added)
                {
                    snapshotDiff = GetSnapshotDiff(entry);
                    var pkValues = entry.Properties
                        .Where(p => p.Metadata.IsPrimaryKey())
                        .Select(p => p.CurrentValue?.ToString() ?? p.OriginalValue?.ToString())
                        .ToList();
                    var pkStr = pkValues.Any() ? string.Join(", ", pkValues) : "unknown";
                    actionMsg = $"Executed {operationType} operation on table {tableName} (PK: {pkStr})";
                }
                else
                {
                    actionMsg = $"Executed {operationType} operation on table {tableName}";
                }

                var log = UserActivityLogs.Create(
                    userId: actorUserId,
                    actorRole: actorRole,
                    category: ActivityLogCategory.DATA_MUTATION,
                    actorEmail: actorEmail,
                    action: actionMsg,
                    status: ActivityLogStatus.SUCCESS,
                    ipAddress: ipAddress,
                    userAgent: userAgent,
                    tableName: tableName,
                    operationType: operationType,
                    snapshotDiff: snapshotDiff);

                auditEntries.Add(new AuditEntry
                {
                    Entry = entry,
                    Log = log,
                    OriginalState = entry.State
                });
            }

            return auditEntries;
        }

        private void TrackPendingLogs(DbContext context)
        {
            var auditEntries = PrepareAuditLogs(context, DateTime.UtcNow);
            if (auditEntries.Any())
            {
                PendingLogsTable.AddOrUpdate(context, auditEntries);
            }
        }

        private void CompleteAndEnqueueLogs(DbContext context, ActivityLogStatus status, string? errorMessage = null)
        {
            try
            {
                if (PendingLogsTable.TryGetValue(context, out var auditEntries))
                {
                    foreach (var audit in auditEntries)
                    {
                        var log = audit.Log;
                        log.Status = status;

                        if (status == ActivityLogStatus.SUCCESS)
                        {
                            if (audit.OriginalState == EntityState.Added)
                            {
                                // Generate snapshot diff now that database has generated the ID
                                log.SnapshotDiff = GetSnapshotDiff(audit.Entry, audit.OriginalState);

                                // Append real primary key values to log Action
                                var pkValues = audit.Entry.Properties
                                    .Where(p => p.Metadata.IsPrimaryKey())
                                    .Select(p => p.CurrentValue?.ToString())
                                    .ToList();
                                var pkStr = pkValues.Any() ? string.Join(", ", pkValues) : "unknown";
                                log.Action = $"Executed CREATE operation on table {log.TableName} (PK: {pkStr})";
                            }
                        }
                        else
                        {
                            log.Action = $"{log.Action} [Error: {errorMessage}]";
                        }

                        // Ensure fields are sanitized after modification to avoid database string truncation error
                        log.Sanitize();

                        _auditLogQueue.TryEnqueue(log);
                    }
                    PendingLogsTable.Remove(context);
                }
            }
            catch (Exception ex)
            {
                // Safety net to prevent auditing failures from crashing the main business flow
                Console.WriteLine($"[AUDIT ERROR] Failed to complete and enqueue audit logs: {ex}");
            }
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            if (eventData.Context != null)
            {
                TrackPendingLogs(eventData.Context);
            }

            return base.SavingChanges(eventData, result);
        }

        public override int SavedChanges(
            SaveChangesCompletedEventData eventData,
            int result)
        {
            if (eventData.Context != null)
            {
                CompleteAndEnqueueLogs(eventData.Context, ActivityLogStatus.SUCCESS);
            }
            return base.SavedChanges(eventData, result);
        }

        public override void SaveChangesFailed(
            DbContextErrorEventData eventData)
        {
            if (eventData.Context != null)
            {
                CompleteAndEnqueueLogs(eventData.Context, ActivityLogStatus.FAIL, eventData.Exception.Message);
            }
            base.SaveChangesFailed(eventData);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context != null)
            {
                TrackPendingLogs(eventData.Context);
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context != null)
            {
                CompleteAndEnqueueLogs(eventData.Context, ActivityLogStatus.SUCCESS);
            }
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        public override async Task SaveChangesFailedAsync(
            DbContextErrorEventData eventData,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context != null)
            {
                CompleteAndEnqueueLogs(eventData.Context, ActivityLogStatus.FAIL, eventData.Exception.Message);
            }
            await base.SaveChangesFailedAsync(eventData, cancellationToken);
        }

        private string GetSnapshotDiff(EntityEntry entry, EntityState? stateOverride = null)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = false };
                var state = stateOverride ?? entry.State;

                if (state == EntityState.Added)
                {
                    var values = entry.Properties.ToDictionary(
                        p => p.Metadata.Name,
                        p => MaskSensitiveData(p.Metadata.Name, p.CurrentValue)
                    );
                    return JsonSerializer.Serialize(new { values }, options);
                }
                else if (state == EntityState.Deleted)
                {
                    var values = entry.Properties.ToDictionary(
                        p => p.Metadata.Name,
                        p => MaskSensitiveData(p.Metadata.Name, p.OriginalValue)
                    );
                    return JsonSerializer.Serialize(new { values }, options);
                }
                else if (state == EntityState.Modified)
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
                return JsonSerializer.Serialize(new { error = "Failed to generate snapshot diff: " + ex.Message });
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

        private void MaskJsonNode(JsonNode rootNode)
        {
            var stack = new Stack<JsonNode>();
            stack.Push(rootNode);

            while (stack.Count > 0)
            {
                var node = stack.Pop();

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
                            stack.Push(kvp.Value);
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
                            stack.Push(item);
                        }
                    }
                }
            }
        }
    }
}
