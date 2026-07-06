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

            // Handle default time range logic and validation
            if (!startDate.HasValue && !endDate.HasValue)
            {
                endDate = DateTime.UtcNow;
                startDate = endDate.Value.AddDays(-7);
            }
            else
            {
                // If only one of them is provided
                endDate ??= DateTime.UtcNow;
                startDate ??= endDate.Value.AddDays(-7);

                var duration = endDate.Value - startDate.Value;
                if (duration.TotalDays <= 0 || duration.TotalDays > 30.0)
                {
                    return new ResponseBase<PagedResult<AuditLogDto>>("The retrieval time range is too large. Please limit the search scope to 30 days to ensure performance.");
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
                return new ResponseBase<int>("The minimum retention days must be 1 day.");
            }

            var cutoffDate = DateTime.UtcNow.AddDays(-daysRetention);
            int deletedCount = await _auditLogRepository.PurgeActivityLogsAsync(cutoffDate);

            return new ResponseBase<int>(deletedCount, $"Successfully purged {deletedCount} audit log records older than {daysRetention} days.");
        }
    }
}
