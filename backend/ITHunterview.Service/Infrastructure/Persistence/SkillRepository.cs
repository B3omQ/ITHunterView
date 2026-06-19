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

        public Task<List<Skills>> GetActiveSkillsAsync()
        {
            return _context.Skills
                .Where(s => s.Status == SkillStatus.ACTIVE)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }
    }
}
