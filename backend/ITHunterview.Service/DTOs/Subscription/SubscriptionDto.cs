using System;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.Subscription
{
    public class SubscriptionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int DurationDays { get; set; }
        public FeaturesConfigDto FeaturesConfig { get; set; } = null!;
        public SubscriptionStatus Status { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsUsed { get; set; }
    }
}
