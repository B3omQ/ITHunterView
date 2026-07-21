using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ITHunterview.WebAPI.BackgroundServices
{
    public class PaymentCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PaymentCleanupBackgroundService> _logger;

        public PaymentCleanupBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<PaymentCleanupBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Payment Cleanup Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await DoCleanupAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing payment cleanup.");
                }

                // Run every 10 minutes
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }

            _logger.LogInformation("Payment Cleanup Background Service is stopping.");
        }

        private async Task DoCleanupAsync(CancellationToken stoppingToken)
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-30);
            _logger.LogInformation("Starting payment cleanup. Cutoff time: {CutoffTime}", cutoffTime);

            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ITHunterview.Service.Infrastructure.Persistence.ITHunterviewContext>();

                using (var transaction = await context.Database.BeginTransactionAsync(stoppingToken))
                {
                    try
                    {
                        var pendingPayments = await context.Payments
                            .Where(p => p.Status == PaymentStatus.PENDING && p.CreatedAt < cutoffTime)
                            .ToListAsync(stoppingToken);

                        if (pendingPayments.Any())
                        {
                            foreach (var payment in pendingPayments)
                            {
                                payment.Status = PaymentStatus.FAILED;
                                payment.UpdatedAt = DateTime.UtcNow;
                            }

                            context.Payments.UpdateRange(pendingPayments);
                            var updatedCount = await context.SaveChangesAsync(stoppingToken);
                            await transaction.CommitAsync(stoppingToken);
                            _logger.LogInformation("Successfully failed {Count} pending payments older than 30 minutes.", updatedCount);
                        }
                        else
                        {
                            _logger.LogInformation("No pending payments found older than 30 minutes.");
                        }
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync(stoppingToken);
                        throw;
                    }
                }
            }
        }
    }
}
