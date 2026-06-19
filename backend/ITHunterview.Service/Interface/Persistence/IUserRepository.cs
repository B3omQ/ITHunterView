using System;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface IUserRepository
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByIdAsync(Guid userId);
        Task<User?> GetUserWithRoleAsync(Guid userId);
        Task<User?> GetUserWithRoleByEmailAsync(string email);
        Task AddUserAsync(User user);
        Task UpdateUserAsync(User user);
    }
}
