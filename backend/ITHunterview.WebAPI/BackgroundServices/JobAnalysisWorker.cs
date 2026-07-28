using System;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ITHunterview.WebAPI.BackgroundServices
{
    public class JobAnalysisWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<JobAnalysisWorker> _logger;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(3, 3);

        public JobAnalysisWorker(IServiceScopeFactory scopeFactory, ILogger<JobAnalysisWorker> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("JobAnalysisWorker started.");
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));

            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<IJobAnalysisRepository>();
                    var claimedRunIds = await repo.ClaimPendingRunIdsAsync(5, stoppingToken);

                    if (claimedRunIds.Count == 0) continue;

                    foreach (var runId in claimedRunIds)
                    {
                        await _semaphore.WaitAsync(stoppingToken);
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                using var processScope = _scopeFactory.CreateScope();
                                var processor = processScope.ServiceProvider.GetRequiredService<IJobAnalysisProcessor>();
                                await processor.ProcessAsync(runId, stoppingToken);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"JobAnalysisWorker: Exception processing run {runId}");
                            }
                            finally
                            {
                                _semaphore.Release();
                            }
                        }, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "JobAnalysisWorker error during polling tick.");
                }
            }

            _logger.LogInformation("JobAnalysisWorker stopping.");
        }
    }
}
