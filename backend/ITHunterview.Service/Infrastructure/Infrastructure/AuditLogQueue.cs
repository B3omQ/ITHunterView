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
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });
        }

        public async ValueTask QueueBackgroundWorkItemAsync(UserActivityLogs logItem, CancellationToken cancellationToken = default)
        {
            if (logItem == null)
            {
                throw new ArgumentNullException(nameof(logItem));
            }

            await _queue.Writer.WriteAsync(logItem, cancellationToken);
        }

        public bool TryEnqueue(UserActivityLogs logItem)
        {
            if (logItem == null)
            {
                return false;
            }

            return _queue.Writer.TryWrite(logItem);
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
