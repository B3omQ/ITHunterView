using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
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

        public async Task<(List<User> Items, int Total)> GetPagedUsersAsync(int page, int pageSize, string? search, int? roleId, UserStatus? status)
        {
            var query = _context.Users
                .Include(u => u.CandidateProfile)
                .Include(u => u.RecruiterProfile)
                    .ThenInclude(rp => rp!.Company)
                .AsNoTracking();

            if (roleId.HasValue)
            {
                query = query.Where(u => u.RoleId == roleId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(u => u.Status == status.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u =>
                    EF.Functions.ILike(u.Email, $"%{search}%") ||
                    (u.CandidateProfile != null && EF.Functions.ILike(u.CandidateProfile.FirstName + " " + u.CandidateProfile.LastName, $"%{search}%")) ||
                    (u.RecruiterProfile != null && u.RecruiterProfile.FullName != null && EF.Functions.ILike(u.RecruiterProfile.FullName, $"%{search}%")) ||
                    (u.RecruiterProfile != null && u.RecruiterProfile.Company != null && u.RecruiterProfile.Company.Name != null && EF.Functions.ILike(u.RecruiterProfile.Company.Name, $"%{search}%"))
                );
            }

            int total = await query.CountAsync();
            var items = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public Task<User?> GetUserDetailWithCompanyAsync(Guid userId)
        {
            return _context.Users
                .Include(u => u.CandidateProfile)
                .Include(u => u.RecruiterProfile)
                    .ThenInclude(rp => rp!.Company)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public Task<bool> RoleExistsAsync(int roleId)
            => _context.Roles.AnyAsync(r => r.Id == roleId);

        public async Task<string?> GetRoleNameAsync(int roleId)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId);
            return role?.Name;
        }
    }
}
