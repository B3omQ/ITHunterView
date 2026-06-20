using System;
using System.Threading.Tasks;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.AuditLogs;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface IAuditLogUseCase
    {
        Task<ResponseBase<PagedResult<AuditLogDto>>> GetPagedAuditLogsAsync(
            int page,
            int pageSize,
            string? search,
            ActivityLogCategory? category,
            ActivityLogStatus? status,
            DateTime? startDate,
            DateTime? endDate,
            Guid? userId,
            string? operationType);

        Task<ResponseBase<int>> PurgeAuditLogsAsync(int daysRetention);
    }
}
