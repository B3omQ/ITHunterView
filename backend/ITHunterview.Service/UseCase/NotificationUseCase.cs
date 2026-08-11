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
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.UseCase
{
    public class NotificationUseCase : INotificationUseCase
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly ITHunterviewContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ITHunterview.Service.Interface.Infrastructure.INotificationQueue _notificationQueue;

        public NotificationUseCase(
            INotificationRepository notificationRepository, 
            ITHunterviewContext context, 
            IHubContext<NotificationHub> hubContext,
            ITHunterview.Service.Interface.Infrastructure.INotificationQueue notificationQueue)
        {
            _notificationRepository = notificationRepository;
            _context = context;
            _hubContext = hubContext;
            _notificationQueue = notificationQueue;
        }

        public async Task<bool> CreateSystemWideNotificationAsync(CreateSystemNotificationDto request)
        {
            // If TargetEmails is provided but TargetUserIds is empty, resolve emails to user IDs
            if ((request.TargetUserIds == null || request.TargetUserIds.Count == 0) &&
                request.TargetEmails != null && request.TargetEmails.Count > 0)
            {
                var cleanEmails = request.TargetEmails
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Select(e => e.Trim().ToLower())
                    .Distinct()
                    .ToList();

                if (cleanEmails.Count > 0)
                {
                    request.TargetUserIds = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(_context.Users
                        .AsNoTracking()
                        .Where(u => cleanEmails.Contains(u.Email.ToLower()))
                        .Select(u => u.Id));
                }
            }

            // 1. Enqueue database insert payload to be processed in background
            await _notificationQueue.QueueSystemNotificationAsync(request);

            // Give the background worker a tiny head start (500ms)
            await Task.Delay(500);

            // 2. Real-time SignalR dispatch based on TargetType
            var targetType = request.TargetType?.ToUpperInvariant() ?? "ALL";

            if ((targetType == "USER" || targetType == "CUSTOM") && request.TargetUserIds != null && request.TargetUserIds.Count > 0)
            {
                var userGroupNames = request.TargetUserIds.Select(id => id.ToString()).ToList();
                await _hubContext.Clients.Groups(userGroupNames).SendAsync("ReceiveNotification");
            }
            else if (targetType == "ROLE" && !string.IsNullOrWhiteSpace(request.TargetRole))
            {
                var roleGroupName = $"Role_{request.TargetRole.Trim().ToLower()}";
                await _hubContext.Clients.Group(roleGroupName).SendAsync("ReceiveNotification");
            }
            else // ALL
            {
                var groups = new List<string> { "Role_candidate", "Role_recruiter", "Role_staff" };
                await _hubContext.Clients.Groups(groups).SendAsync("ReceiveNotification");
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
