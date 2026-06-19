using System;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class UserRepository : IUserRepository
    {
        private readonly ITHunterviewContext _context;

        public UserRepository(ITHunterviewContext context)
        {
            _context = context;
        }

        public Task<User?> GetUserByEmailAsync(string email)
            => _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        public Task<User?> GetUserByIdAsync(Guid userId)
            => _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        public Task<User?> GetUserWithRoleAsync(Guid userId)
            => _context.Users
                .Include(u => u.Role)
                .Include(u => u.CandidateProfile)
                .Include(u => u.RecruiterProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

        public Task<User?> GetUserWithRoleByEmailAsync(string email)
            => _context.Users
                .Include(u => u.Role)
                .Include(u => u.CandidateProfile)
                .Include(u => u.RecruiterProfile)
                .FirstOrDefaultAsync(u => u.Email == email);

        public async Task AddUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}
