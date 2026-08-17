using ITHunterview.Service.Hubs;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ITHunterview.WebAPI.BackgroundServices;

public sealed class CandidateJobScanWorker(
    ICandidateJobScanQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<CandidateJobScanWorker> logger,
    IHubContext<NotificationHub> hubContext) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var request = await queue.DequeueAsync(stoppingToken);
                using var scope = scopeFactory.CreateScope();
                var useCase = scope.ServiceProvider.GetRequiredService<ICandidateJobScanUseCase>();
                try
                {
                    await useCase.ProcessRunAsync(request.RunId, stoppingToken);
                    await hubContext.Clients.Group(request.CandidateUserId.ToString()).SendAsync("ReceiveNotification", new { Type = "CandidateJobScanComplete", RunId = request.RunId }, stoppingToken);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Candidate scan failed for RunId {RunId}", request.RunId);
                    await hubContext.Clients.Group(request.CandidateUserId.ToString()).SendAsync("ReceiveNotification", new { Type = "CandidateJobScanError", RunId = request.RunId }, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception exception) { logger.LogError(exception, "Candidate scan worker dequeue failure"); }
        }
    }
}
