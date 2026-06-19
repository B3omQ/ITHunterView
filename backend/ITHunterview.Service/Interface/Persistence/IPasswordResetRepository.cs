using System;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface IPasswordResetRepository
    {
        Task AddTokenAsync(PasswordResets token);
        Task<PasswordResets?> GetByTokenAndEmailAsync(string token, string email);
        Task MarkUsedAsync(Guid tokenId);
    }
}
