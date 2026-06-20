using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface IAuditLogRepository
    {
        Task AddActivityLogAsync(UserActivityLogs log);
        Task<(List<UserActivityLogs> Items, int Total)> GetPagedActivityLogsAsync(
            int page,
            int pageSize,
            string? search,
            ActivityLogCategory? category,
            ActivityLogStatus? status,
            DateTime? startDate,
            DateTime? endDate,
            Guid? userId,
            string? operationType);
        Task<int> PurgeActivityLogsAsync(DateTime cutoffDate);
    }
}
