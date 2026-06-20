using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly ITHunterviewContext _context;

        public AuditLogRepository(ITHunterviewContext context)
        {
            _context = context;
        }

        public async Task AddActivityLogAsync(UserActivityLogs log)
        {
            _context.UserActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<(List<UserActivityLogs> Items, int Total)> GetPagedActivityLogsAsync(
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
            var query = _context.UserActivityLogs.AsNoTracking();

            if (category.HasValue)
            {
                query = query.Where(l => l.ActionCategory == category.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(l => l.Status == status.Value);
            }

            if (startDate.HasValue)
            {
                query = query.Where(l => l.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(l => l.CreatedAt <= endDate.Value);
            }

            if (userId.HasValue)
            {
                query = query.Where(l => l.UserId == userId.Value);
            }

            if (!string.IsNullOrWhiteSpace(operationType))
            {
                query = query.Where(l => EF.Functions.ILike(l.OperationType, operationType));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(l =>
                    EF.Functions.ILike(l.ActorEmail, $"%{search}%") ||
                    EF.Functions.ILike(l.Action, $"%{search}%") ||
                    EF.Functions.ILike(l.IpAddress, $"%{search}%") ||
                    EF.Functions.ILike(l.UserAgent, $"%{search}%") ||
                    EF.Functions.ILike(l.TableName, $"%{search}%")
                );
            }

            int total = await query.CountAsync();
            var items = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<int> PurgeActivityLogsAsync(DateTime cutoffDate)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Thiết lập biến session cho PostgreSQL bypass trigger trong transaction này
                await _context.Database.ExecuteSqlRawAsync("SET LOCAL app.allow_audit_log_delete = 'true';");
                
                var deletedCount = await _context.UserActivityLogs
                    .Where(log => log.CreatedAt < cutoffDate)
                    .ExecuteDeleteAsync();
                
                await transaction.CommitAsync();
                return deletedCount;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
