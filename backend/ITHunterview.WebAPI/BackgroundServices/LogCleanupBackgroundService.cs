using System;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ITHunterview.WebAPI.BackgroundServices
{
    public class LogCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LogCleanupBackgroundService> _logger;
        private readonly IConfiguration _configuration;

        public LogCleanupBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<LogCleanupBackgroundService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Log Cleanup Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await DoCleanupAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing log cleanup.");
                }

                // Run every 24 hours
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }

            _logger.LogInformation("Log Cleanup Background Service is stopping.");
        }

        private async Task DoCleanupAsync()
        {
            var retentionDays = _configuration.GetValue<int>("LogSettings:RetentionDays", 30);
            _logger.LogInformation("Starting log cleanup. Retention days: {RetentionDays}", retentionDays);

            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

            using (var scope = _serviceProvider.CreateScope())
            {
                var actorProvider = scope.ServiceProvider.GetService<IActorProvider>();
                actorProvider?.SetActor(Guid.Empty, "system_background_service@ithunterview.com", "system", "localhost", "LogCleanupBackgroundService");

                var auditLogRepository = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
                var deletedCount = await auditLogRepository.PurgeActivityLogsAsync(cutoffDate);
                _logger.LogInformation("Successfully purged {Count} logs older than {CutoffDate}.", deletedCount, cutoffDate);
            }
        }
    }
}
