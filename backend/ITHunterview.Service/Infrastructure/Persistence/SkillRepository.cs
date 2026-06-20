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

        public async Task<List<(Skills Skill, string CategoryName)>> GetActiveSkillsWithCategoryAsync()
        {
            var query = from s in _context.Skills
                        join c in _context.SkillCategories on s.CategoryId equals c.Id into sc
                        from category in sc.DefaultIfEmpty()
                        where s.Status == SkillStatus.ACTIVE
                        select new { s, CategoryName = category != null ? category.Name : string.Empty };

            var results = await query.ToListAsync();
            return results.Select(r => (r.s, r.CategoryName)).ToList();
        }
    }
}
