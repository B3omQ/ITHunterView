using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class CandidateSkillRepository : ICandidateSkillRepository
    {
        private readonly ITHunterviewContext _context;

        public CandidateSkillRepository(ITHunterviewContext context)
        {
            _context = context;
        }

        public Task<List<UserSkills>> GetByUserIdAsync(Guid userId)
        {
            return _context.UserSkills
                .Include(us => us.Skill)
                .Where(us => us.UserId == userId)
                .ToListAsync();
        }

        public Task<UserSkills?> GetUserSkillAsync(Guid userId, int skillId)
        {
            return _context.UserSkills
                .FirstOrDefaultAsync(us => us.UserId == userId && us.SkillId == skillId);
        }

        public async Task<UserSkills> AddAsync(UserSkills userSkill)
        {
            _context.UserSkills.Add(userSkill);
            await _context.SaveChangesAsync();
            return userSkill;
        }

        public async Task<bool> RemoveAsync(Guid userId, int skillId)
        {
            var entity = await _context.UserSkills
                .FirstOrDefaultAsync(us => us.UserId == userId && us.SkillId == skillId);

            if (entity == null)
                return false;

            _context.UserSkills.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public Task<List<Skills>> SearchMasterSkillsAsync(string keyword, List<int> excludeIds, int limit = 20)
        {
            return _context.Skills
                .Where(s => s.Status == SkillStatus.ACTIVE
                         && s.Name.ToLower().Contains(keyword.ToLower())
                         && !excludeIds.Contains(s.Id))
                .OrderBy(s => s.Name)
                .Take(limit)
                .ToListAsync();
        }

        public Task<Skills?> GetMasterSkillByIdAsync(int skillId)
        {
            return _context.Skills.FirstOrDefaultAsync(s => s.Id == skillId);
        }
    }
}
