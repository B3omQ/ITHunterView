using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class MajorRepository : IMajorRepository
    {
        private readonly ITHunterviewContext _context;

        public MajorRepository(ITHunterviewContext context)
        {
            _context = context;
        }

        public Task<Majors?> GetByIdAsync(int id)
            => _context.Majors.FirstOrDefaultAsync(m => m.Id == id);

        public async Task<(List<Majors> Items, int Total)> GetPagedMajorsAsync(int page, int pageSize, string? search)
        {
            var query = _context.Majors.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim().ToLower();
                query = query.Where(m => m.Name.ToLower().Contains(normalizedSearch) || m.Code.ToLower().Contains(normalizedSearch));
            }

            var total = await query.CountAsync();
            var items = await query
                .Include(m => m.Parent)
                .OrderBy(m => m.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task AddAsync(Majors major)
        {
            _context.Majors.Add(major);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Majors major)
        {
            _context.Majors.Update(major);
            await _context.SaveChangesAsync();
        }

        public Task<Majors?> GetDeletedByIdAsync(int id)
            => _context.Majors.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == id && m.DeletedAt != null);

        public async Task DeleteAsync(Majors major, Guid userId)
        {
            major.DeletedAt = DateTime.UtcNow;
            major.UpdatedBy = userId;
            _context.Majors.Update(major);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            var normalizedInput = Utils.StringNormalizationHelper.NormalizeITTerm(name);
            var query = _context.Majors.AsQueryable();

            if (excludeId.HasValue)
            {
                query = query.Where(m => m.Id != excludeId.Value);
            }

            return await query.AnyAsync(m => m.NormalizedName == normalizedInput);
        }

        public async Task<bool> ExistsByCodeAsync(string code, int? excludeId = null)
        {
            var normalizedCode = code.Trim().ToLowerInvariant();
            var query = _context.Majors.AsQueryable();

            if (excludeId.HasValue)
            {
                query = query.Where(m => m.Id != excludeId.Value);
            }

            return await query.AnyAsync(m => m.Code.ToLower() == normalizedCode);
        }

        public Task<bool> IsMajorInUseAsync(int id)
            => _context.CandidateEducations.AnyAsync(e => e.MajorId == id);

        public Task<List<Majors>> GetAllActiveMajorsAsync()
            => _context.Majors.Include(m => m.Parent).ToListAsync();

        public Task<bool> HasChildrenAsync(int id)
            => _context.Majors.AnyAsync(m => m.ParentId == id);
    }
}
