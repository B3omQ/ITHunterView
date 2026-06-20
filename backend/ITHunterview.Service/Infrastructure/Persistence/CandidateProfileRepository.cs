using ITHunterview.Domain.Entities;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class CandidateProfileRepository : ICandidateProfileRepository
    {
        private readonly ITHunterviewContext _context;

        public CandidateProfileRepository(ITHunterviewContext context)
        {
            _context = context;
        }

        public Task<CandidateProfiles?> GetByUserIdAsync(Guid userId)
        {
            return _context.CandidateProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }

        public async Task<CandidateProfiles> CreateAsync(CandidateProfiles profile)
        {
            _context.CandidateProfiles.Add(profile);
            await _context.SaveChangesAsync();
            return profile;
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
