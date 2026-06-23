using ITHunterview.Domain.Entities;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class CandidateCertificationRepository : ICandidateCertificationRepository
    {
        private readonly ITHunterviewContext _context;

        public CandidateCertificationRepository(ITHunterviewContext context)
        {
            _context = context;
        }

        public Task<List<CandidateCertifications>> GetByUserIdAsync(Guid userId)
        {
            return _context.CandidateCertifications
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.IssueDate)
                .ToListAsync();
        }

        public Task<CandidateCertifications?> GetByIdAndUserIdAsync(Guid id, Guid userId)
        {
            return _context.CandidateCertifications
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        }

        public async Task<CandidateCertifications> CreateAsync(CandidateCertifications certification)
        {
            _context.CandidateCertifications.Add(certification);
            await _context.SaveChangesAsync();
            return certification;
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(Guid id, Guid userId)
        {
            var entity = await _context.CandidateCertifications
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (entity == null)
                return false;

            _context.CandidateCertifications.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
