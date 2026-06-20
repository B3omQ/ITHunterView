using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface ISkillCategoryRepository
    {
        Task<List<SkillCategories>> GetAllCategoriesAsync();
        Task<bool> CategoryExistsAsync(int id);
    }
}
