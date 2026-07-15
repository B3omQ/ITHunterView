using System;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.Notification
{
    public class CreateNotificationDto
    {
        public Guid UserId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public NotificationType Type { get; set; }
    }
}
