using System;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Ai;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface IAiConfigUseCase
    {
        Task<AiConfigResponseDto> GetAiConfigAsync();
        Task UpdateAiConfigAsync(Guid userId, UpdateAiConfigRequestDto dto);
        Task<TestConnectionResponseDto> TestConnectionAsync(string providerName, string prompt);
        Task<AiUsageSummaryDto> GetAiUsageAnalyticsAsync(AiUsageFilterDto filter);
    }
}
