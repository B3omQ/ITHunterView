using System;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Ai;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface IAiConfigUseCase
    {
        Task<AiConfigResponseDto> GetAiConfigAsync();
        Task UpdateActiveProviderAsync(Guid userId, string providerName);
        Task<TestConnectionResponseDto> TestConnectionAsync(string providerName, string prompt);
    }
}
