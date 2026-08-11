namespace ITHunterview.Service.DTOs.Dashboard;

public class AdminDashboardResponseDto
{
    public decimal TotalRevenue { get; set; }
    public decimal RevenueGrowthPercentage { get; set; }
    
    public int TotalUsers { get; set; }
    public decimal UserGrowthPercentage { get; set; }
    
    public int AiTokensUsed { get; set; }
    public decimal TokensGrowthPercentage { get; set; }
    
    public int Transactions { get; set; }
    public decimal TransactionsGrowthPercentage { get; set; }

    public List<UserRevenueGrowthDto> UserRevenueGrowth { get; set; } = new();
    public List<TokenUsageDto> TokenUsage { get; set; } = new();
    public List<SubscriptionBreakdownDto> SubscriptionBreakdown { get; set; } = new();
}

public class UserRevenueGrowthDto
{
    public string Month { get; set; } = string.Empty;
    public int Users { get; set; }
    public decimal Revenue { get; set; }
}

public class TokenUsageDto
{
    public string Day { get; set; } = string.Empty;
    public int Tokens { get; set; }
}

public class SubscriptionBreakdownDto
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}
