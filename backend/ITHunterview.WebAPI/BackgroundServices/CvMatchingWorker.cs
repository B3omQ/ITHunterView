using System;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Service.Hubs;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ITHunterview.WebAPI.BackgroundServices
{
    public class CvMatchingWorker : BackgroundService
    {
        private readonly ICvMatchingQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CvMatchingWorker> _logger;
        private readonly IHubContext<NotificationHub> _hubContext;

        public CvMatchingWorker(
            ICvMatchingQueue queue, 
            IServiceScopeFactory scopeFactory, 
            ILogger<CvMatchingWorker> logger,
            IHubContext<NotificationHub> hubContext)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CvMatchingWorker is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var request = await _queue.DequeueAsync(stoppingToken);

                    // Process the matching in the background
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            using var scope = _scopeFactory.CreateScope();
                            if (request.IsHardcode)
                            {
                                var hardcodeUseCase = scope.ServiceProvider.GetRequiredService<IHardcodeCvJobMatchingUseCase>();
                                await hardcodeUseCase.MatchCvWithAllJobsHardcodeAsync(request.CvId, request.UserId);
                            }
                            else
                            {
                                var matchingUseCase = scope.ServiceProvider.GetRequiredService<ICvJobMatchingUseCase>();
                                await matchingUseCase.MatchCvWithAllJobsAsync(request.CvId, request.UserId);
                            }

                            // Notify frontend via SignalR
                            await _hubContext.Clients.Group(request.UserId.ToString()).SendAsync("ReceiveNotification", new
                            {
                                Type = "CvMatchComplete",
                                CvId = request.CvId,
                                Title = "Smart Match Complete",
                                Message = "CV Matching has completed successfully. Your results are ready."
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error occurred processing CV match request for CV {CvId}", request.CvId);
                            // Notify frontend about the error with actual message
                            var errorMessage = ex is NullReferenceException
                                ? "An internal error occurred during matching. The CV or job data may be incomplete."
                                : ex.Message;
                            await _hubContext.Clients.Group(request.UserId.ToString()).SendAsync("ReceiveNotification", new
                            {
                                Type = "CvMatchError",
                                CvId = request.CvId,
                                Title = "Smart Match Failed",
                                Message = errorMessage
                            });
                        }
                    }, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Prevent throwing if stoppingToken was signaled
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing CvMatchingWorker.");
                }
            }

            _logger.LogInformation("CvMatchingWorker is stopping.");
        }
    }
}
