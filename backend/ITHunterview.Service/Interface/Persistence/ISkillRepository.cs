using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface ISkillRepository
    {
        Task<List<(Skills Skill, string CategoryName)>> GetActiveSkillsWithCategoryAsync();
    }
}
