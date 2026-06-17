using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Auth;
using ITHunterview.Service.DTOs.Common;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface IAuthUseCase
    {
        Task<ResponseBase<LoginResponseDto>> LoginAsync(LoginRequestDto request);
    }
}
