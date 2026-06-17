using System;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface IUserRepository
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task AddUserAsync(User user);
    }
}
