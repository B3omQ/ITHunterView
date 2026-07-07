using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class SkillCategoryRepository : ISkillCategoryRepository
    {
        private readonly ITHunterviewContext _context;

        public SkillCategoryRepository(ITHunterviewContext context)
        {
            _context = context;
        }

        public Task<List<SkillCategories>> GetAllCategoriesAsync()
            => _context.SkillCategories.OrderBy(c => c.Name).ToListAsync();

        public Task<bool> CategoryExistsAsync(int id)
            => _context.SkillCategories.AnyAsync(c => c.Id == id);

        public Task<SkillCategories?> GetByIdAsync(int id)
            => _context.SkillCategories.FirstOrDefaultAsync(c => c.Id == id);

        public async Task<SkillCategories> AddAsync(SkillCategories category)
        {
            _context.SkillCategories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task UpdateAsync(SkillCategories category)
        {
            _context.SkillCategories.Update(category);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(SkillCategories category)
        {
            _context.SkillCategories.Remove(category);
            await _context.SaveChangesAsync();
        }
    }
}
