using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Infrastructure
{
    public interface IAuditLogQueue
    {
        ValueTask QueueBackgroundWorkItemAsync(UserActivityLogs logItem, CancellationToken cancellationToken = default);
        bool TryEnqueue(UserActivityLogs logItem);
        ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken);
        bool TryDequeue(out UserActivityLogs? logItem);
    }
}
