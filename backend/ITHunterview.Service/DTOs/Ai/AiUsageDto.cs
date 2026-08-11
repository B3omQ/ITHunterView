using System;
using System.Collections.Generic;

namespace ITHunterview.Service.DTOs.Ai
{
    public class AiUsageFilterDto
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? ProviderName { get; set; }
        public string? FeatureCode { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class AiUsageSummaryDto
    {
        public long TotalTokens { get; set; }
        public long PromptTokens { get; set; }
        public long CompletionTokens { get; set; }
        public decimal TotalEstimatedCostUsd { get; set; }
        public int TotalRequests { get; set; }
        public double AvgLatencyMs { get; set; }

        public List<ProviderUsageBreakdownDto> ProviderBreakdown { get; set; } = new();
        public List<FeatureUsageBreakdownDto> FeatureBreakdown { get; set; } = new();
        public List<AiUsageLogItemDto> Logs { get; set; } = new();

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalLogRecords { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalLogRecords / (PageSize > 0 ? PageSize : 20));
    }

    public class ProviderUsageBreakdownDto
    {
        public string ProviderName { get; set; } = string.Empty;
        public long TotalTokens { get; set; }
        public decimal EstimatedCostUsd { get; set; }
        public int RequestCount { get; set; }
        public double Percentage { get; set; }
    }

    public class FeatureUsageBreakdownDto
    {
        public string FeatureCode { get; set; } = string.Empty;
        public string FeatureName { get; set; } = string.Empty;
        public long TotalTokens { get; set; }
        public decimal EstimatedCostUsd { get; set; }
        public int RequestCount { get; set; }
    }

    public class AiUsageLogItemDto
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string FeatureCode { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public long PromptTokens { get; set; }
        public long CompletionTokens { get; set; }
        public long TotalTokens { get; set; }
        public decimal EstimatedCostUsd { get; set; }
        public long LatencyMs { get; set; }
        public string Status { get; set; } = "SUCCESS";
    }
}
