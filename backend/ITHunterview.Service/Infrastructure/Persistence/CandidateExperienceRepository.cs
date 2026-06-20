using ITHunterview.Domain.Entities;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class CandidateExperienceRepository : ICandidateExperienceRepository
    {
        private readonly ITHunterviewContext _context;

        public CandidateExperienceRepository(ITHunterviewContext context)
        {
            _context = context;
        }

        public Task<List<CandidateExperiences>> GetByUserIdAsync(Guid userId)
        {
            return _context.CandidateExperiences
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.IsCurrent)
                .ThenByDescending(e => e.StartDate)
                .ToListAsync();
        }

        public Task<CandidateExperiences?> GetByIdAndUserIdAsync(Guid id, Guid userId)
        {
            return _context.CandidateExperiences
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        }

        public async Task<CandidateExperiences> CreateAsync(CandidateExperiences experience)
        {
            _context.CandidateExperiences.Add(experience);
            await _context.SaveChangesAsync();
            return experience;
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(Guid id, Guid userId)
        {
            var entity = await _context.CandidateExperiences
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (entity == null)
                return false;

            _context.CandidateExperiences.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public Task<bool> CompanyExistsAsync(Guid companyId)
        {
            return _context.Companies.AnyAsync(c => c.Id == companyId);
        }
    }
}
