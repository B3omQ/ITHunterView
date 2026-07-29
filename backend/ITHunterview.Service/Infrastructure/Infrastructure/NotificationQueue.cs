using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Notification;
using ITHunterview.Service.Interface.Infrastructure;

namespace ITHunterview.Service.Infrastructure.Infrastructure
{
    public class NotificationQueue : INotificationQueue
    {
        private readonly Channel<CreateSystemNotificationDto> _queue;

        public NotificationQueue()
        {
            var options = new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<CreateSystemNotificationDto>(options);
        }

        public async ValueTask QueueSystemNotificationAsync(CreateSystemNotificationDto request, CancellationToken cancellationToken = default)
        {
            await _queue.Writer.WriteAsync(request, cancellationToken);
        }

        public async ValueTask<CreateSystemNotificationDto> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}
