using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class UserRepository : ITHunterview.Service.Interface.Persistence.IUserRepository
    {
        private readonly ITHunterviewContext _context;

        public UserRepository(ITHunterviewContext context)
        {
            _context = context;
        }

        public Task<User?> GetUserByEmailAsync(string email)
        {
            return _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task AddUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }
    }
}
