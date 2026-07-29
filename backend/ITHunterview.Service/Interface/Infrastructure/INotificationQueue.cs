using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Notification;

namespace ITHunterview.Service.Interface.Infrastructure
{
    public interface INotificationQueue
    {
        ValueTask QueueSystemNotificationAsync(CreateSystemNotificationDto request, CancellationToken cancellationToken = default);
        ValueTask<CreateSystemNotificationDto> DequeueAsync(CancellationToken cancellationToken);
    }
}
