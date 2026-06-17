using System.Threading.Tasks;
using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class TokenRepository : ITHunterview.Service.Interface.Persistence.ITokenRepository
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
    }
}
