using System;
using System.Threading.Tasks;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.UserGovernance;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface IUserGovernanceUseCase
    {
        Task<ResponseBase<PagedResult<UserDto>>> GetPagedUsersAsync(int page, int pageSize, string? search, int? roleId, UserStatus? status);
        Task<ResponseBase<UserDetailDto>> GetUserDetailAsync(Guid userId);
        Task<ResponseBase<bool>> UpdateUserStatusAsync(Guid targetUserId, UpdateUserStatusDto dto, Guid actorUserId, string actorEmail, string actorRole, string ipAddress, string userAgent);
        Task<ResponseBase<bool>> UpdateUserRoleAsync(Guid targetUserId, UpdateUserRoleDto dto, Guid actorUserId, string actorEmail, string actorRole, string ipAddress, string userAgent);
        Task<ResponseBase<PagedResult<UserActivityLogDto>>> GetPagedActivityLogsAsync(int page, int pageSize, string? search, ActivityLogCategory? category, ActivityLogStatus? status);
        Task<UserStatus?> GetUserStatusAsync(Guid userId);
    }
}
