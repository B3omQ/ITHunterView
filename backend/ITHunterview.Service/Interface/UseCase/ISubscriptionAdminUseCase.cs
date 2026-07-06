using System;
using System.Threading.Tasks;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Subscription;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface ISubscriptionAdminUseCase
    {
        Task<ResponseBase<PagedResult<SubscriptionDto>>> GetPagedSubscriptionsAsync(string? role, SubscriptionStatus? status, int page, int pageSize);
        Task<ResponseBase<SubscriptionDto>> GetSubscriptionByIdAsync(int id);
        Task<ResponseBase<SubscriptionDto>> CreateSubscriptionAsync(CreateSubscriptionDto dto, Guid userId);
        Task<ResponseBase<SubscriptionDto>> UpdateSubscriptionAsync(int id, UpdateSubscriptionDto dto, Guid userId);
        Task<ResponseBase<SubscriptionDto>> UpdateStatusAsync(int id, SubscriptionStatus status, Guid userId);
        Task<ResponseBase<SubscriptionDto>> DuplicateSubscriptionAsync(int id, Guid userId);
    }
}
