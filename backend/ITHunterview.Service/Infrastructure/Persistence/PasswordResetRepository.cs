using System;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class PasswordResetRepository : IPasswordResetRepository
    {
        private readonly ITHunterviewContext _context;

        public PasswordResetRepository(ITHunterviewContext context)
        {
            _context = context;
        }

        public async Task AddTokenAsync(PasswordResets token)
        {
            _context.PasswordResets.Add(token);
            await _context.SaveChangesAsync();
        }

        public Task<PasswordResets?> GetByTokenAndEmailAsync(string token, string email)
            => _context.PasswordResets
                .Include(pr => pr.User)
                .FirstOrDefaultAsync(pr =>
                    pr.Token == token &&
                    !pr.IsUsed &&
                    pr.User.Email == email);

        public async Task MarkUsedAsync(Guid tokenId)
        {
            var token = await _context.PasswordResets.FindAsync(tokenId);
            if (token != null)
            {
                token.IsUsed = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}
