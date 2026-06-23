using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;

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
        Task<(List<User> Items, int Total)> GetPagedUsersAsync(int page, int pageSize, string? search, int? roleId, UserStatus? status);
        Task<User?> GetUserDetailWithCompanyAsync(Guid userId);
        Task<bool> RoleExistsAsync(int roleId);
        Task<string?> GetRoleNameAsync(int roleId);
    }
}
