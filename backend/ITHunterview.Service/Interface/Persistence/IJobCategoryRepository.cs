using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface IJobCategoryRepository
    {
        Task<List<JobCategories>> GetCategoriesAsync();
    }
}
