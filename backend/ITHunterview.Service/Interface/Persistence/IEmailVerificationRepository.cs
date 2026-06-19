using System;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface IEmailVerificationRepository
    {
        Task AddTokenAsync(EmailVerificationTokens token);
        Task<EmailVerificationTokens?> GetByTokenAsync(string token);
        Task MarkUsedAsync(Guid tokenId);
    }
}
