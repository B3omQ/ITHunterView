using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("user_activity_logs")]
    public class UserActivityLogs
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("user_id")]
        public Guid? UserId { get; set; }

        [Column("actor_role")]
        [MaxLength(50)]
        public string ActorRole { get; set; }

        [Column("action_category")]
        public ActivityLogCategory ActionCategory { get; set; }

        [Column("actor_email")]
        [MaxLength(100)]
        public string ActorEmail { get; set; }

        [Column("action")]
        [MaxLength(255)]
        public string Action { get; set; }

        [Column("status")]
        public ActivityLogStatus Status { get; set; }

        [Column("ip_address")]
        [MaxLength(50)]
        public string IpAddress { get; set; }

        [Column("user_agent")]
        [MaxLength(500)]
        public string UserAgent { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("table_name")]
        [MaxLength(100)]
        public string? TableName { get; set; }

        [Column("operation_type")]
        [MaxLength(50)]
        public string? OperationType { get; set; }

        [Column("snapshot_diff", TypeName = "jsonb")]
        public string? SnapshotDiff { get; set; }

        public static UserActivityLogs Create(
            Guid? userId,
            string actorRole,
            ActivityLogCategory category,
            string actorEmail,
            string action,
            ActivityLogStatus status,
            string ipAddress,
            string userAgent,
            string? tableName = null,
            string? operationType = null,
            string? snapshotDiff = null)
        {
            var log = new UserActivityLogs
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ActorRole = actorRole ?? "anonymous",
                ActionCategory = category,
                ActorEmail = actorEmail ?? "unknown",
                Action = action ?? "unknown",
                Status = status,
                IpAddress = ipAddress ?? "unknown",
                UserAgent = userAgent ?? "unknown",
                CreatedAt = DateTime.UtcNow,
                TableName = tableName,
                OperationType = operationType,
                SnapshotDiff = snapshotDiff
            };
            log.Sanitize();
            return log;
        }

        public void Sanitize()
        {
            if (ActorRole != null && ActorRole.Length > 50) ActorRole = ActorRole.Substring(0, 47) + "...";
            if (ActorEmail != null && ActorEmail.Length > 100) ActorEmail = ActorEmail.Substring(0, 97) + "...";
            if (Action != null && Action.Length > 255) Action = Action.Substring(0, 252) + "...";
            if (IpAddress != null && IpAddress.Length > 50) IpAddress = IpAddress.Substring(0, 47) + "...";
            if (UserAgent != null && UserAgent.Length > 500) UserAgent = UserAgent.Substring(0, 497) + "...";
            if (TableName != null && TableName.Length > 100) TableName = TableName.Substring(0, 97) + "...";
            if (OperationType != null && OperationType.Length > 50) OperationType = OperationType.Substring(0, 47) + "...";
        }
    }
}

