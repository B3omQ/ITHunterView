using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Infrastructure
{
    public interface IAuditLogQueue
    {
        void QueueBackgroundWorkItem(UserActivityLogs logItem);
        ValueTask<UserActivityLogs> DequeueAsync(CancellationToken cancellationToken);
        ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken);
        bool TryDequeue(out UserActivityLogs? logItem);
    }
}
