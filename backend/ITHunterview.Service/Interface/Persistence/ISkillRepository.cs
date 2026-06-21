using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface ISkillRepository
    {
        Task<List<(Skills Skill, string CategoryName)>> GetActiveSkillsWithCategoryAsync();
        Task<Skills?> GetByIdAsync(int id);
        Task<(List<Skills> Items, int Total)> GetPagedSkillsAsync(int page, int pageSize, string? search, int? categoryId, SkillStatus? status);
        Task AddAsync(Skills skill);
        Task UpdateAsync(Skills skill);
        Task DeleteAsync(Skills skill);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
        Task<bool> IsSkillInUseAsync(int id);
        Task<int> CountUserSkillsAsync(int skillId);
        Task<int> CountJobRequirementsAsync(int skillId);
    }
}
