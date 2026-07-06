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
                query = query.Where(l => EF.Functions.ILike(l.OperationType ?? "", operationType));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(l =>
                    EF.Functions.ILike(l.ActorEmail ?? "", $"%{search}%") ||
                    EF.Functions.ILike(l.Action ?? "", $"%{search}%") ||
                    EF.Functions.ILike(l.IpAddress ?? "", $"%{search}%") ||
                    EF.Functions.ILike(l.TableName ?? "", $"%{search}%")
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
            int totalDeleted = 0;
            bool hasMore = true;

            while (hasMore)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Set session variable for PostgreSQL to bypass trigger in this transaction
                    await _context.Database.ExecuteSqlRawAsync("SET LOCAL app.allow_audit_log_delete = 'true';");
                    
                    // Delete in small batches of 5000 to prevent table locking
                    var deletedCount = await _context.Database.ExecuteSqlRawAsync(
                        "DELETE FROM user_activity_logs WHERE id IN (SELECT id FROM user_activity_logs WHERE created_at < {0} LIMIT 5000)", 
                        cutoffDate);

                    await transaction.CommitAsync();

                    totalDeleted += deletedCount;

                    if (deletedCount < 5000)
                    {
                        hasMore = false;
                    }
                    else
                    {
                        // Brief pause to allow concurrent database operations to execute
                        await Task.Delay(50);
                    }
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }

            return totalDeleted;
        }
    }
}
