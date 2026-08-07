using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class CvRepository : ICvRepository
    {
        private readonly ITHunterviewContext _context;

        public CvRepository(ITHunterviewContext context)
        {
            _context = context;
        }

        public async Task<Cvs> CreateAsync(Cvs cv)
        {
            await _context.Cvs.AddAsync(cv);
            await _context.SaveChangesAsync();
            return cv;
        }

        public async Task<IEnumerable<Cvs>> GetByUserIdAsync(Guid userId)
        {
            return await _context.Cvs
                .Where(c => c.UserId == userId && c.DeletedAt == null)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Cvs?> GetByIdAsync(Guid id)
        {
            return await _context.Cvs
                .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);
        }

        public async Task UpdateAsync(Cvs cv)
        {
            _context.Cvs.Update(cv);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Cvs cv)
        {
            cv.DeletedAt = DateTime.UtcNow;
            _context.Cvs.Update(cv);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasPrimaryCvAsync(Guid userId)
        {
            return await _context.Cvs.AnyAsync(c => c.UserId == userId && c.IsPrimary && c.DeletedAt == null);
        }

        public async Task ResetPrimaryCvAsync(Guid userId)
        {
            var primaryCvs = await _context.Cvs
                .Where(c => c.UserId == userId && c.IsPrimary && c.DeletedAt == null)
                .ToListAsync();

            if (primaryCvs.Any())
            {
                foreach (var cv in primaryCvs)
                {
                    cv.IsPrimary = false;
                    cv.UpdatedAt = DateTime.UtcNow;
                }
                _context.Cvs.UpdateRange(primaryCvs);
                await _context.SaveChangesAsync();
            }
        }

        public async Task SetPrimaryCvAsync(Guid id, Guid userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Lock every active CV of this user before changing the primary flag.
                // This serializes concurrent "set primary" requests for one user.
                var cvs = await _context.Cvs
                    .FromSqlInterpolated($"SELECT * FROM cvs WHERE user_id = {userId} AND deleted_at IS NULL FOR UPDATE")
                    .ToListAsync();

                if (!cvs.Any(c => c.Id == id)) return; // Ensure CV exists and belongs to user

                var now = DateTime.UtcNow;

                // PostgreSQL checks the partial unique index immediately. Do not use
                // UpdateRange: EF can send the promotion before the demotion, briefly
                // creating two primary CVs and violating IX_cvs_user_id_is_primary.
                await _context.Cvs
                    .Where(c => c.UserId == userId && c.DeletedAt == null && c.IsPrimary)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(c => c.IsPrimary, false)
                        .SetProperty(c => c.UpdatedAt, now));

                var promotedRows = await _context.Cvs
                    .Where(c => c.Id == id && c.UserId == userId && c.DeletedAt == null)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(c => c.IsPrimary, true)
                        .SetProperty(c => c.UpdatedAt, now));

                if (promotedRows != 1)
                {
                    throw new KeyNotFoundException("CV not found or no longer belongs to the user.");
                }
                
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> TryLockCvForParsingAsync(Guid id)
        {
            var rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                "UPDATE cvs SET parse_status = 'PROCESSING', parse_error = NULL, analysis_quality = NULL, " +
                "analysis_coverage_json = NULL, analysis_diagnostics_json = NULL, updated_at = {0} " +
                "WHERE id = {1} AND parse_status = 'PENDING' AND deleted_at IS NULL",
                DateTime.UtcNow, id);
            return rowsAffected > 0;
        }
    }
}
