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
                    bool hasItems = false;
                    if (batch.Any())
                    {
                        var timeSinceLastFlush = DateTime.UtcNow - lastFlushTime;
                        var timeLeft = flushInterval - timeSinceLastFlush;
                        if (timeLeft <= TimeSpan.Zero)
                        {
                            await FlushBatchAsync(batch, stoppingToken);
                            lastFlushTime = DateTime.UtcNow;
                            timeLeft = flushInterval;
                        }

                        using (var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken))
                        {
                            timeoutCts.CancelAfter(timeLeft);
                            try
                            {
                                hasItems = await _auditLogQueue.WaitToReadAsync(timeoutCts.Token);
                            }
                            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                            {
                                await FlushBatchAsync(batch, stoppingToken);
                                lastFlushTime = DateTime.UtcNow;
                            }
                        }
                    }
                    else
                    {
                        hasItems = await _auditLogQueue.WaitToReadAsync(stoppingToken);
                    }

                    if (hasItems)
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
            if (batch == null || !batch.Any()) return;

            foreach (var log in batch)
            {
                log.Sanitize();
            }

            const int maxRetryAttempts = 3;
            int attempt = 0;
            bool success = false;

            while (attempt < maxRetryAttempts && !success)
            {
                attempt++;
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ITHunterviewContext>();
                        context.UserActivityLogs.AddRange(batch);
                        await context.SaveChangesAsync(cancellationToken);
                        success = true;
                    }
                }
                catch (Exception ex)
                {
                    if (IsTransientException(ex) && attempt < maxRetryAttempts)
                    {
                        var delayMs = (int)Math.Pow(2, attempt) * 100; // Exponential Backoff: 200ms, 400ms, 800ms
                        _logger.LogWarning(ex, "Transient database error on batch flush attempt {Attempt}. Retrying in {Delay}ms...", attempt, delayMs);
                        try
                        {
                            await Task.Delay(delayMs, cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }
                    else
                    {
                        // Hard exception or max attempts reached, drop out of batch retry loop and process individually
                        break;
                    }
                }
            }

            if (success)
            {
                batch.Clear();
                return;
            }

            _logger.LogWarning("Batch flush failed. Processing {Count} audit logs individually to isolate failures.", batch.Count);
            var transientFailures = new List<UserActivityLogs>();

            foreach (var log in batch)
            {
                bool logSuccess = false;
                attempt = 0;

                while (attempt < maxRetryAttempts && !logSuccess)
                {
                    attempt++;
                    try
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var context = scope.ServiceProvider.GetRequiredService<ITHunterviewContext>();
                            context.UserActivityLogs.Add(log);
                            await context.SaveChangesAsync(cancellationToken);
                            logSuccess = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (IsTransientException(ex))
                        {
                            if (attempt < maxRetryAttempts)
                            {
                                var delayMs = (int)Math.Pow(2, attempt) * 50; // Smaller backoff for individual retry
                                _logger.LogWarning(ex, "Transient database error on individual entry attempt {Attempt}. Retrying in {Delay}ms...", attempt, delayMs);
                                try
                                {
                                    await Task.Delay(delayMs, cancellationToken);
                                }
                                catch (OperationCanceledException)
                                {
                                    transientFailures.Add(log);
                                    break;
                                }
                            }
                            else
                            {
                                _logger.LogError(ex, "Transient database error persisted after {Attempts} retries for individual entry. Re-queueing...", maxRetryAttempts);
                                transientFailures.Add(log);
                                break;
                            }
                        }
                        else
                        {
                            _logger.LogError(ex, "Hard database constraint or formatting error for individual audit entry. Dropping item. Actor: {ActorEmail}, Action: {Action}", log.ActorEmail, log.Action);
                            break;
                        }
                    }
                }
            }

            if (transientFailures.Any())
            {
                _logger.LogWarning("Re-queuing {Count} transient-failure audit logs back to queue.", transientFailures.Count);
                foreach (var failedLog in transientFailures)
                {
                    _auditLogQueue.TryEnqueue(failedLog);
                }
            }

            batch.Clear();
        }

        private bool IsTransientException(Exception? ex)
        {
            if (ex == null) return false;

            if (ex is TimeoutException || 
                ex is System.Net.Sockets.SocketException || 
                ex is System.IO.IOException)
            {
                return true;
            }

            var type = ex.GetType();
            var typeName = type.FullName ?? "";

            if (typeName.Contains("NpgsqlException") || typeName.Contains("PostgresException"))
            {
                var isTransientProp = type.GetProperty("IsTransient");
                if (isTransientProp != null && isTransientProp.PropertyType == typeof(bool))
                {
                    var isTransient = (bool?)isTransientProp.GetValue(ex);
                    if (isTransient == true) return true;
                }

                var sqlStateProp = type.GetProperty("SqlState");
                if (sqlStateProp != null && sqlStateProp.PropertyType == typeof(string))
                {
                    var sqlState = (string?)sqlStateProp.GetValue(ex);
                    if (sqlState != null)
                    {
                        if (sqlState.StartsWith("08") || 
                            sqlState.StartsWith("57P") || 
                            sqlState.StartsWith("58"))
                        {
                            return true;
                        }
                    }
                }
            }

            return IsTransientException(ex.InnerException);
        }
    }
}
