using System.Threading.Tasks;
using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface ITokenRepository
    {
        Task AddRefreshTokenAsync(RefreshToken token);
    }
}
