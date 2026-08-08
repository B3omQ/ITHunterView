using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.UseCase;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ITHunterview.WebAPI.BackgroundServices;

public sealed class CvJdMatchingWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CvJdMatchingWorker> _logger;
    private readonly string _workerId = $"cv-jd:{Environment.MachineName}:{Guid.NewGuid():N}";

    public CvJdMatchingWorker(IServiceScopeFactory scopeFactory, ILogger<CvJdMatchingWorker> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Durable CV-JD matching worker started with worker id {WorkerId}.", _workerId);
        using var timer = new PeriodicTimer(PollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var recoveryScope = _scopeFactory.CreateScope())
                {
                    var worker = recoveryScope.ServiceProvider.GetRequiredService<ICvJdMatchingWorkerUseCase>();
                    await worker.RecoverExpiredLeasesAsync(DateTime.UtcNow, stoppingToken);
                    var claims = await worker.ClaimRunnableJobsAsync(
                        4,
                        _workerId,
                        DateTime.UtcNow,
                        CvJdMatchingWorkerUseCase.LeaseDuration,
                        stoppingToken);

                    var tasks = claims.Select(claim => ProcessClaimAsync(claim.JobId, claim.LeaseToken, stoppingToken));
                    await Task.WhenAll(tasks);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Durable CV-JD matching worker polling cycle failed.");
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("Durable CV-JD matching worker stopping.");
    }

    private async Task ProcessClaimAsync(Guid jobId, Guid leaseToken, CancellationToken stoppingToken)
    {
        try
        {
            using var processScope = _scopeFactory.CreateScope();
            var worker = processScope.ServiceProvider.GetRequiredService<ICvJdMatchingWorkerUseCase>();
            await worker.ProcessClaimedJobAsync(jobId, _workerId, leaseToken, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutdown leaves the lease for restart recovery.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Durable CV-JD matching job {JobId} processing cycle failed.", jobId);
        }
    }
}
