using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly ITHunterviewContext _context;

        public NotificationRepository(ITHunterviewContext context)
        {
            _context = context;
        }

        public async Task AddNotificationAsync(Notifications notification)
        {
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task AddNotificationsAsync(IEnumerable<Notifications> notifications)
        {
            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();
        }

        public async Task<int> PurgeOldNotificationsAsync(DateTime cutoffDate)
        {
            return await _context.Notifications
                .Where(n => n.CreatedAt < cutoffDate)
                .ExecuteDeleteAsync();
        }

        public async Task<(IEnumerable<Notifications> items, int total)> GetUserNotificationsAsync(Guid userId, int pageIndex, int pageSize)
        {
            var query = _context.Notifications.Where(n => n.UserId == userId);
            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<Notifications?> GetNotificationByIdAsync(Guid id)
        {
            return await _context.Notifications.FindAsync(id);
        }

        public async Task UpdateNotificationAsync(Notifications notification)
        {
            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<(IEnumerable<(string Title, string Message, DateTime CreatedAt)> items, int total)> GetSystemNotificationsGroupedAsync(int pageIndex, int pageSize)
        {
            var query = _context.Notifications
                .Where(n => n.Type == ITHunterview.Domain.Enums.NotificationType.SYSTEM)
                .GroupBy(n => new { n.Title, n.Message, n.CreatedAt })
                .Select(g => new { g.Key.Title, g.Key.Message, g.Key.CreatedAt });

            var total = await query.CountAsync();
            var groupedItems = await query
                .OrderByDescending(g => g.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = groupedItems.Select(g => (g.Title, g.Message, g.CreatedAt)).ToList();

            return (items, total);
        }

        public async Task<int> DeleteSystemNotificationsAsync(string title, string message)
        {
            return await _context.Notifications
                .Where(n => n.Type == ITHunterview.Domain.Enums.NotificationType.SYSTEM && n.Title == title && n.Message == message)
                .ExecuteDeleteAsync();
        }
    }
}
