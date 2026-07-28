using System;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Notification;
using ITHunterview.Service.Hubs;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.SignalR;

namespace ITHunterview.Service.UseCase
{
    public class NotificationUseCase : INotificationUseCase
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly ITHunterviewContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationUseCase(INotificationRepository notificationRepository, ITHunterviewContext context, IHubContext<NotificationHub> hubContext)
        {
            _notificationRepository = notificationRepository;
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<bool> CreateSystemWideNotificationAsync(CreateSystemNotificationDto request)
        {
            // Only send to candidates and recruiters
            var targetRoleIds = _context.Roles
                .Where(r => r.Name == "candidate" || r.Name == "recruiter")
                .Select(r => r.Id)
                .ToList();

            var targetUsers = _context.Users
                .Where(u => u.RoleId.HasValue && targetRoleIds.Contains(u.RoleId.Value) && u.Status == UserStatus.ACTIVE)
                .Select(u => u.Id)
                .ToList();

            var now = DateTime.UtcNow;
            var notifications = targetUsers.Select(userId => new Notifications
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = request.Title,
                Message = request.Message,
                Type = request.Type,
                IsRead = false,
                CreatedAt = now
            }).ToList();

            if (notifications.Any())
            {
                await _notificationRepository.AddNotificationsAsync(notifications);

                // Broadcast to all connected clients in candidate/recruiter groups
                var groupNames = targetUsers.Select(u => u.ToString()).ToList();
                await _hubContext.Clients.Groups(groupNames).SendAsync("ReceiveNotification");
            }

            return true;
        }

        public async Task<bool> CreateNotificationAsync(CreateNotificationDto request)
        {
            var notification = new Notifications
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Title = request.Title,
                Message = request.Message,
                Type = request.Type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddNotificationAsync(notification);
            return true;
        }

        public async Task<ITHunterview.Service.DTOs.Common.PaginatedDataResponse<NotificationDto>> GetUserNotificationsAsync(Guid userId, int pageIndex, int pageSize)
        {
            var (items, total) = await _notificationRepository.GetUserNotificationsAsync(userId, pageIndex, pageSize);

            var dtos = items.Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            }).ToList();

            return new ITHunterview.Service.DTOs.Common.PaginatedDataResponse<NotificationDto>
            {
                Data = dtos,
                Meta = new ITHunterview.Service.DTOs.Common.PaginationMeta
                {
                    CurrentPage = pageIndex,
                    PageSize = pageSize,
                    TotalItems = total,
                    TotalPages = (int)Math.Ceiling(total / (double)pageSize)
                }
            };
        }

        public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId)
        {
            var notification = await _notificationRepository.GetNotificationByIdAsync(notificationId);
            if (notification == null || notification.UserId != userId)
            {
                return false;
            }

            notification.IsRead = true;
            await _notificationRepository.UpdateNotificationAsync(notification);
            return true;
        }

        public async Task<ITHunterview.Service.DTOs.Common.PaginatedDataResponse<SystemNotificationDto>> GetSystemNotificationsForStaffAsync(int pageIndex, int pageSize, string? searchTerm = null)
        {
            var (items, total) = await _notificationRepository.GetSystemNotificationsGroupedAsync(pageIndex, pageSize, searchTerm);

            var dtos = items.Select(i => new SystemNotificationDto
            {
                Title = i.Title,
                Message = i.Message,
                CreatedAt = i.CreatedAt,
                IsHidden = i.IsHidden
            }).ToList();

            return new ITHunterview.Service.DTOs.Common.PaginatedDataResponse<SystemNotificationDto>
            {
                Data = dtos,
                Meta = new ITHunterview.Service.DTOs.Common.PaginationMeta
                {
                    CurrentPage = pageIndex,
                    PageSize = pageSize,
                    TotalItems = total,
                    TotalPages = (int)Math.Ceiling(total / (double)pageSize)
                }
            };
        }

        public async Task<bool> DeleteSystemNotificationAsync(string title, string message)
        {
            var deletedCount = await _notificationRepository.DeleteSystemNotificationsAsync(title, message);
            return deletedCount > 0;
        }
    }
}
