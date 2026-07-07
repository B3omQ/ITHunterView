using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface ISkillCategoryRepository
    {
        Task<List<SkillCategories>> GetAllCategoriesAsync();
        Task<bool> CategoryExistsAsync(int id);
        Task<SkillCategories?> GetByIdAsync(int id);
        Task<SkillCategories> AddAsync(SkillCategories category);
        Task UpdateAsync(SkillCategories category);
        Task DeleteAsync(SkillCategories category);
    }
}
