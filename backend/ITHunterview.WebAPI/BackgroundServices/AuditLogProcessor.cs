using System;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ITHunterview.WebAPI.BackgroundServices
{
    public class AuditLogProcessor : BackgroundService
    {
        private readonly IAuditLogQueue _auditLogQueue;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AuditLogProcessor> _logger;

        public AuditLogProcessor(
            IAuditLogQueue auditLogQueue,
            IServiceProvider serviceProvider,
            ILogger<AuditLogProcessor> logger)
        {
            _auditLogQueue = auditLogQueue;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Audit Log Processor Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var logItem = await _auditLogQueue.DequeueAsync(stoppingToken);

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ITHunterviewContext>();
                        context.UserActivityLogs.Add(logItem);
                        await context.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Service is stopping
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing audit log insertion.");
                }
            }

            _logger.LogInformation("Audit Log Processor Background Service is stopping.");
        }
    }
}
