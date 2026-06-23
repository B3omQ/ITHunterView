using System;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.User;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface IUserUseCase
    {
        Task<ResponseBase<UserMeDto>> GetMeAsync(Guid userId);
        Task<Guid> ResolveRecruiterIdAsync(string? userIdStr);
    }
}
