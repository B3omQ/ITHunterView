using System;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class TokenRepository : ITokenRepository
    {
        private readonly ITHunterviewContext _context;

        public TokenRepository(ITHunterviewContext context)
        {
            _context = context;
        }

        public async Task AddRefreshTokenAsync(RefreshToken token)
        {
            _context.RefreshTokens.Add(token);
            await _context.SaveChangesAsync();
        }

        public Task<RefreshToken?> GetRefreshTokenAsync(string token)
            => _context.RefreshTokens
                .Include(t => t.User)
                    .ThenInclude(u => u.Role)
                .Include(t => t.User)
                    .ThenInclude(u => u.CandidateProfile)
                .Include(t => t.User)
                    .ThenInclude(u => u.RecruiterProfile)
                .FirstOrDefaultAsync(t => t.Token == token && !t.IsRevoked);

        public async Task RevokeRefreshTokenAsync(string token)
        {
            var existing = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == token);
            if (existing != null)
            {
                existing.IsRevoked = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task RevokeAllUserRefreshTokensAsync(Guid userId)
        {
            var tokens = await _context.RefreshTokens
                .Where(t => t.UserId == userId && !t.IsRevoked)
                .ToListAsync();

            foreach (var token in tokens)
                token.IsRevoked = true;

            await _context.SaveChangesAsync();
        }
    }
}
