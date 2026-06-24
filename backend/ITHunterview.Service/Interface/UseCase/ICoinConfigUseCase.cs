using System;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.CoinConfig;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface ICoinConfigUseCase
    {
        Task<ResponseBase<UpdateCoinConfigDto>> GetCoinConfigAsync();
        Task<ResponseBase<UpdateCoinConfigDto>> UpdateCoinConfigAsync(UpdateCoinConfigDto dto, Guid actorUserId);
    }
}
