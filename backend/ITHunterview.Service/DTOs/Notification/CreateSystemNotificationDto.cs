using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.Notification
{
    public class CreateSystemNotificationDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        [Required]
        public NotificationType Type { get; set; }

        public string TargetType { get; set; } = "ALL"; // ALL | ROLE | USER | CUSTOM

        public string? TargetRole { get; set; } // candidate | recruiter | staff

        public List<Guid>? TargetUserIds { get; set; }

        public List<string>? TargetEmails { get; set; }
    }
}
