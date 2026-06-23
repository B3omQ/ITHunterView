using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Interface.Infrastructure;

namespace ITHunterview.Service.Infrastructure.Infrastructure
{
    public class AuditLogQueue : IAuditLogQueue
    {
        private readonly Channel<UserActivityLogs> _queue;

        public AuditLogQueue()
        {
            _queue = Channel.CreateBounded<UserActivityLogs>(new BoundedChannelOptions(10000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });
        }

        public void QueueBackgroundWorkItem(UserActivityLogs logItem)
        {
            if (logItem == null)
            {
                throw new ArgumentNullException(nameof(logItem));
            }

            _queue.Writer.TryWrite(logItem);
        }

        public ValueTask<UserActivityLogs> DequeueAsync(CancellationToken cancellationToken)
        {
            return _queue.Reader.ReadAsync(cancellationToken);
        }

        public ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken)
        {
            return _queue.Reader.WaitToReadAsync(cancellationToken);
        }

        public bool TryDequeue(out UserActivityLogs? logItem)
        {
            var success = _queue.Reader.TryRead(out var item);
            logItem = item;
            return success;
        }
    }
}
