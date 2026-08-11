using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Hubs;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.DTOs.Notification;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ITHunterview.WebAPI.BackgroundServices
{
    public class JobExpirationBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<JobExpirationBackgroundService> _logger;

        public JobExpirationBackgroundService(IServiceScopeFactory scopeFactory, ILogger<JobExpirationBackgroundService> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("JobExpirationBackgroundService started.");
            
            // Check every 5 minutes
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));

            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ITHunterviewContext>();
                    var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();
                    var notificationUseCase = scope.ServiceProvider.GetRequiredService<INotificationUseCase>();

                    var now = DateTime.UtcNow;
                    var expiredJobs = await context.JobPostings
                        .Where(j => j.Status == JobStatus.PUBLISHED && j.ExpiresAt.HasValue && j.ExpiresAt.Value < now)
                        .ToListAsync(stoppingToken);

                    if (expiredJobs.Any())
                    {
                        foreach (var job in expiredJobs)
                        {
                            job.Status = JobStatus.EXPIRED;
                            job.UpdatedAt = now;
                        }

                        context.JobPostings.UpdateRange(expiredJobs);
                        await context.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation($"Expired {expiredJobs.Count} jobs.");

                        foreach (var job in expiredJobs)
                        {
                            await hubContext.Clients.All.SendAsync("JobStatusChanged", job.Id, stoppingToken);
                            await notificationUseCase.CreateNotificationAsync(new CreateNotificationDto
                            {
                                UserId = job.RecruiterId,
                                Title = "Tin tuyển dụng hết hạn hiển thị",
                                Message = $"Tin tuyển dụng '{job.Title}' đã hết thời gian hiển thị 30 ngày trên hệ thống. Ứng viên sẽ không thể tìm thấy tin này nữa. Bạn có thể gia hạn hiển thị bằng cách sử dụng gói Extend Job.",
                                Type = NotificationType.SYSTEM
                            });
                        }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "JobExpirationBackgroundService error during execution.");
                }
            }

            _logger.LogInformation("JobExpirationBackgroundService stopping.");
        }
    }
}
