using System;

namespace ITHunterview.Service.DTOs.Notification
{
    public class SystemNotificationDto
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
