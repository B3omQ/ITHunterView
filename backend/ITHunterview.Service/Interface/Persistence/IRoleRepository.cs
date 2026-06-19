using System.Threading.Tasks;
using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface IRoleRepository
    {
        Task<Roles?> GetByNameAsync(string roleName);
        Task<Roles?> GetByIdAsync(int id);
    }
}
