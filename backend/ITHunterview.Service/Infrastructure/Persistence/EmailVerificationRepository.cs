using System;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class EmailVerificationRepository : IEmailVerificationRepository
    {
        private readonly ITHunterviewContext _context;

        public EmailVerificationRepository(ITHunterviewContext context)
        {
            _context = context;
        }

        public async Task AddTokenAsync(EmailVerificationTokens token)
        {
            _context.EmailVerificationTokens.Add(token);
            await _context.SaveChangesAsync();
        }

        public Task<EmailVerificationTokens?> GetByTokenAsync(string token)
            => _context.EmailVerificationTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == token && !t.IsUsed);

        public async Task MarkUsedAsync(Guid tokenId)
        {
            var token = await _context.EmailVerificationTokens.FindAsync(tokenId);
            if (token != null)
            {
                token.IsUsed = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}
