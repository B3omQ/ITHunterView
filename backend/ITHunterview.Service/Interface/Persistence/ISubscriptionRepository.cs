using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface ISubscriptionRepository
    {
        Task<Subscriptions> CreateAsync(Subscriptions subscription);
        Task UpdateAsync(Subscriptions subscription);
        Task<Subscriptions?> GetByIdAsync(int id);
        Task<List<Subscriptions>> GetAllAsync(string? role, SubscriptionStatus? status);
        Task<bool> IsSubscriptionUsedAsync(int id);
        Task<(List<Subscriptions> Items, int TotalCount)> GetPagedAsync(string? role, SubscriptionStatus? status, int page, int pageSize);
        Task<int> UpdateUnusedSubscriptionAsync(Subscriptions subscription);
    }
}
