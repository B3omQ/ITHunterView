using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
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

            var batch = new List<UserActivityLogs>();
            const int batchSize = 50;
            var flushInterval = TimeSpan.FromSeconds(2);
            var lastFlushTime = DateTime.UtcNow;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (await _auditLogQueue.WaitToReadAsync(stoppingToken))
                    {
                        while (_auditLogQueue.TryDequeue(out var logItem))
                        {
                            if (logItem != null)
                            {
                                batch.Add(logItem);
                                if (batch.Count >= batchSize)
                                {
                                    await FlushBatchAsync(batch, stoppingToken);
                                    lastFlushTime = DateTime.UtcNow;
                                }
                            }
                        }
                    }

                    if (batch.Any() && DateTime.UtcNow - lastFlushTime >= flushInterval)
                    {
                        await FlushBatchAsync(batch, stoppingToken);
                        lastFlushTime = DateTime.UtcNow;
                    }

                    await Task.Delay(100, stoppingToken);
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

            // Flush remaining logs on shutdown
            if (batch.Any())
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await FlushBatchAsync(batch, cts.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred flushing remaining logs during shutdown.");
                }
            }

            _logger.LogInformation("Audit Log Processor Background Service is stopping.");
        }

        private async Task FlushBatchAsync(List<UserActivityLogs> batch, CancellationToken cancellationToken)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ITHunterviewContext>();
                    context.UserActivityLogs.AddRange(batch);
                    await context.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to flush a batch of {Count} audit logs to database.", batch.Count);
            }
            finally
            {
                batch.Clear();
            }
        }
    }
}
