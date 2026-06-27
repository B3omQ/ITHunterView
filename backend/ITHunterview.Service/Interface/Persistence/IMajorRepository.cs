using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface IMajorRepository
    {
        Task<Majors?> GetByIdAsync(int id);
        Task<(List<Majors> Items, int Total)> GetPagedMajorsAsync(int page, int pageSize, string? search);
        Task AddAsync(Majors major);
        Task UpdateAsync(Majors major);
        Task<Majors?> GetDeletedByIdAsync(int id);
        Task DeleteAsync(Majors major, Guid userId);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
        Task<bool> ExistsByCodeAsync(string code, int? excludeId = null);
        Task<bool> IsMajorInUseAsync(int id);
        Task<List<Majors>> GetAllActiveMajorsAsync();
        Task<bool> HasChildrenAsync(int id);
    }
}
