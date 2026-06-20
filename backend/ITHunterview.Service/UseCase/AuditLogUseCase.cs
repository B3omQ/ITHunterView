using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.AuditLogs;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase
{
    public class AuditLogUseCase : IAuditLogUseCase
    {
        private readonly IAuditLogRepository _auditLogRepository;

        public AuditLogUseCase(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository;
        }

        public async Task<ResponseBase<PagedResult<AuditLogDto>>> GetPagedAuditLogsAsync(
            int page,
            int pageSize,
            string? search,
            ActivityLogCategory? category,
            ActivityLogStatus? status,
            DateTime? startDate,
            DateTime? endDate,
            Guid? userId,
            string? operationType)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            // Xử lý logic khoảng thời gian mặc định và xác thực
            if (!startDate.HasValue && !endDate.HasValue)
            {
                endDate = DateTime.UtcNow;
                startDate = endDate.Value.AddDays(-7);
            }
            else
            {
                // Nếu chỉ truyền một trong hai
                endDate ??= DateTime.UtcNow;
                startDate ??= endDate.Value.AddDays(-7);

                var duration = endDate.Value - startDate.Value;
                if (duration.TotalDays < 1.0 || duration.TotalDays > 30.0)
                {
                    return new ResponseBase<PagedResult<AuditLogDto>>("Khoảng thời gian truy xuất log phải từ 1 đến tối đa 30 ngày.");
                }
            }

            var (items, total) = await _auditLogRepository.GetPagedActivityLogsAsync(
                page,
                pageSize,
                search,
                category,
                status,
                startDate,
                endDate,
                userId,
                operationType);

            var dtos = items.Select(l => new AuditLogDto
            {
                Id = l.Id,
                UserId = l.UserId,
                ActorRole = l.ActorRole,
                ActionCategory = l.ActionCategory.ToString(),
                ActorEmail = l.ActorEmail,
                Action = l.Action,
                Status = l.Status.ToString(),
                IpAddress = l.IpAddress,
                UserAgent = l.UserAgent,
                CreatedAt = l.CreatedAt,
                TableName = l.TableName,
                OperationType = l.OperationType,
                SnapshotDiff = l.SnapshotDiff
            }).ToList();

            var result = new PagedResult<AuditLogDto>
            {
                Items = dtos,
                Total = total,
                Page = page,
                PageSize = pageSize
            };

            return new ResponseBase<PagedResult<AuditLogDto>>(result);
        }

        public async Task<ResponseBase<int>> PurgeAuditLogsAsync(int daysRetention)
        {
            if (daysRetention < 1)
            {
                return new ResponseBase<int>("Số ngày lưu giữ tối thiểu phải là 1 ngày.");
            }

            var cutoffDate = DateTime.UtcNow.AddDays(-daysRetention);
            int deletedCount = await _auditLogRepository.PurgeActivityLogsAsync(cutoffDate);

            return new ResponseBase<int>(deletedCount, $"Đã dọn dẹp {deletedCount} bản ghi nhật ký kiểm toán cũ hơn {daysRetention} ngày.");
        }
    }
}
