using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Notification;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface INotificationUseCase
    {
        Task<bool> CreateSystemWideNotificationAsync(CreateSystemNotificationDto request);
        Task<bool> CreateNotificationAsync(CreateNotificationDto request);
        Task<ITHunterview.Service.DTOs.Common.PaginatedDataResponse<NotificationDto>> GetUserNotificationsAsync(System.Guid userId, int pageIndex, int pageSize);
        Task<bool> MarkAsReadAsync(System.Guid notificationId, System.Guid userId);
        Task<ITHunterview.Service.DTOs.Common.PaginatedDataResponse<SystemNotificationDto>> GetSystemNotificationsForStaffAsync(int pageIndex, int pageSize, string? searchTerm = null);
        Task<bool> DeleteSystemNotificationAsync(string title, string message);
    }
}
