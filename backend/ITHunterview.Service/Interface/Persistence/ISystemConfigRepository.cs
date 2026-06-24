using System.Threading.Tasks;
using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface ISystemConfigRepository
    {
        Task<SystemConfigs?> GetByKeyAsync(string key);
        Task SaveAsync(SystemConfigs config);
    }
}
