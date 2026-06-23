using System;

namespace ITHunterview.Service.DTOs.AuditLogs
{
    public class AuditLogDto
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string ActorRole { get; set; } = string.Empty;
        public string ActionCategory { get; set; } = string.Empty;
        public string ActorEmail { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        
        // Trực quan hóa dữ liệu kiểm toán CUD
        public string? TableName { get; set; }
        public string? OperationType { get; set; }
        public string? SnapshotDiff { get; set; }
    }
}
