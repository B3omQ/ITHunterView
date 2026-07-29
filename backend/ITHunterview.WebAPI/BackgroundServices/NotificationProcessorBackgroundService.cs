using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ITHunterview.WebAPI.BackgroundServices
{
    public class NotificationProcessorBackgroundService : BackgroundService
    {
        private readonly ILogger<NotificationProcessorBackgroundService> _logger;
        private readonly INotificationQueue _queue;
        private readonly IServiceProvider _serviceProvider;
        private const int BatchSize = 10000;

        public NotificationProcessorBackgroundService(
            ILogger<NotificationProcessorBackgroundService> logger,
            INotificationQueue queue,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _queue = queue;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Processor Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var request = await _queue.DequeueAsync(stoppingToken);

                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ITHunterviewContext>();

                    // 1. Fetch Target Role IDs (Send to EVERYONE: candidate, recruiter, staff, admin)
                    var targetRoleIds = await context.Roles
                        .Where(r => r.Name == "candidate" || r.Name == "recruiter" || r.Name == "staff" || r.Name == "admin")
                        .Select(r => r.Id)
                        .ToListAsync(stoppingToken);

                    // 2. Fetch all target user IDs
                    // For 1,000,000 users, we can just load the IDs into memory, which is roughly 16MB of Guids.
                    var targetUserIds = await context.Users
                        .AsNoTracking()
                        .Where(u => u.RoleId.HasValue && targetRoleIds.Contains(u.RoleId.Value) && u.Status == UserStatus.ACTIVE)
                        .Select(u => u.Id)
                        .ToListAsync(stoppingToken);

                    _logger.LogInformation($"Found {targetUserIds.Count} target users for system notification.");

                    var now = DateTime.UtcNow;

                    // 3. Process in batches
                    for (int i = 0; i < targetUserIds.Count; i += BatchSize)
                    {
                        var batchUserIds = targetUserIds.Skip(i).Take(BatchSize).ToList();

                        var notifications = batchUserIds.Select(userId => new Notifications
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId,
                            Title = request.Title,
                            Message = request.Message,
                            Type = request.Type,
                            IsRead = false,
                            CreatedAt = now
                        }).ToList();

                        await context.Notifications.AddRangeAsync(notifications, stoppingToken);
                        await context.SaveChangesAsync(stoppingToken);

                        // Optional: Clear tracker if it was tracking, but we used AsNoTracking for query
                        context.ChangeTracker.Clear();

                        _logger.LogInformation($"Processed batch of {notifications.Count} notifications. Total: {i + notifications.Count}");
                    }
                    
                    _logger.LogInformation("Finished processing system wide notification.");
                }
                catch (OperationCanceledException)
                {
                    // Prevent throwing if stopping token was canceled
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred processing notification queue.");
                }
            }
        }
    }
}
