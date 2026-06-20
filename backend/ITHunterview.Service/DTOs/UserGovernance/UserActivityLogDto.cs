using System;

namespace ITHunterview.Service.DTOs.UserGovernance
{
    public class UserActivityLogDto
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
    }
}
