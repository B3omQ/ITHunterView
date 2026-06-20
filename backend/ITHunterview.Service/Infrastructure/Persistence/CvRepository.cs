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
    }
}
