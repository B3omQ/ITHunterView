using System;

namespace ITHunterview.Service.DTOs.FeatureUsage
{
    /// <summary>
    /// Describes what was consumed for one feature request so the caller can
    /// compensate it when the feature cannot be delivered.
    /// </summary>
    public class FeatureConsumptionResult
    {
        public int ChargedCoins { get; set; }
        public Guid? DeductTransactionId { get; set; }
        public Guid? UsageLogId { get; set; }
    }
}
