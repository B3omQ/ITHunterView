using System;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ITHunterview.WebAPI.BackgroundServices
{
    public class NotificationCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationCleanupBackgroundService> _logger;

        public NotificationCleanupBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<NotificationCleanupBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Cleanup Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await DoCleanupAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing notification cleanup.");
                }

                // Run every 24 hours
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }

            _logger.LogInformation("Notification Cleanup Background Service is stopping.");
        }

        private async Task DoCleanupAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting notification cleanup.");

            // Delete notifications older than 3 days
            var cutoffDate = DateTime.UtcNow.AddDays(-3);

            using (var scope = _serviceProvider.CreateScope())
            {
                var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
                var deletedCount = await notificationRepository.PurgeOldNotificationsAsync(cutoffDate);
                _logger.LogInformation("Successfully purged {Count} notifications older than {CutoffDate}.", deletedCount, cutoffDate);
            }
        }
    }
}
