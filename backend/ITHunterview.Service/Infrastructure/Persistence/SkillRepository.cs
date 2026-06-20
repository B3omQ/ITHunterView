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
    public class SkillRepository : ISkillRepository
    {
        private readonly ITHunterviewContext _context;

        public SkillRepository(ITHunterviewContext context)
        {
            _context = context;
        }

        public Task<Skills?> GetByIdAsync(int id)
            => _context.Skills.FirstOrDefaultAsync(s => s.Id == id);

        public async Task<(List<Skills> Items, int Total)> GetPagedSkillsAsync(
            int page, int pageSize, string? search, int? categoryId, SkillStatus? status)
        {
            var query = _context.Skills.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim().ToLower();
                query = query.Where(s => s.Name.ToLower().Contains(normalizedSearch));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(s => s.CategoryId == categoryId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(s => s.Status == status.Value);
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(s => s.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task AddAsync(Skills skill)
        {
            _context.Skills.Add(skill);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Skills skill)
        {
            _context.Skills.Update(skill);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Skills skill)
        {
            _context.Skills.Remove(skill);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            var normalizedInput = Utils.StringNormalizationHelper.NormalizeITTerm(name);
            var query = _context.Skills.AsQueryable();
            
            if (excludeId.HasValue)
            {
                query = query.Where(s => s.Id != excludeId.Value);
            }

            return await query.AnyAsync(s => s.NormalizedName == normalizedInput);
        }

        public async Task<bool> IsSkillInUseAsync(int id)
        {
            var inUserSkills = await _context.UserSkills.AnyAsync(us => us.SkillId == id);
            if (inUserSkills) return true;

            var inJobReqs = await _context.JobSkillRequirements.AnyAsync(jsr => jsr.SkillId == id);
            return inJobReqs;
        }

        public Task<int> CountUserSkillsAsync(int skillId)
        {
            return _context.UserSkills.CountAsync(us => us.SkillId == skillId);
        }

        public Task<int> CountJobRequirementsAsync(int skillId)
        {
            return _context.JobSkillRequirements.CountAsync(jsr => jsr.SkillId == skillId);
        }
    }
}
