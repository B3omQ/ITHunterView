using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface INotificationRepository
    {
        Task AddNotificationAsync(Notifications notification);
        Task AddNotificationsAsync(IEnumerable<Notifications> notifications);
        Task<int> PurgeOldNotificationsAsync(DateTime cutoffDate);

        Task<(IEnumerable<Notifications> items, int total)> GetUserNotificationsAsync(Guid userId, int pageIndex, int pageSize);
        Task<Notifications?> GetNotificationByIdAsync(Guid id);
        Task UpdateNotificationAsync(Notifications notification);
        
        Task<(IEnumerable<(string Title, string Message, DateTime CreatedAt, bool IsHidden)> items, int total)> GetSystemNotificationsGroupedAsync(int pageIndex, int pageSize, string? searchTerm = null);
        Task<int> DeleteSystemNotificationsAsync(string title, string message);
    }
}
