using ITHunterview.Domain.Entities;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class CandidateEducationRepository : ICandidateEducationRepository
    {
        private readonly ITHunterviewContext _context;

        public CandidateEducationRepository(ITHunterviewContext context)
        {
            _context = context;
        }

        public Task<List<CandidateEducations>> GetByUserIdAsync(Guid userId)
        {
            return _context.CandidateEducations
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.EndDate)
                .ThenByDescending(e => e.StartDate)
                .ToListAsync();
        }

        public Task<CandidateEducations?> GetByIdAndUserIdAsync(Guid id, Guid userId)
        {
            return _context.CandidateEducations
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        }

        public async Task<CandidateEducations> CreateAsync(CandidateEducations education)
        {
            _context.CandidateEducations.Add(education);
            await _context.SaveChangesAsync();
            return education;
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(Guid id, Guid userId)
        {
            var entity = await _context.CandidateEducations
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (entity == null)
                return false;

            _context.CandidateEducations.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public Task<bool> MajorExistsAsync(int majorId)
        {
            return _context.Majors.AnyAsync(m => m.Id == majorId);
        }

        public Task<List<Majors>> GetAllMajorsAsync()
        {
            return _context.Majors
                .OrderBy(m => m.Name)
                .ToListAsync();
        }
    }
}
