using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Skill;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase
{
    public class SkillsUseCase : ISkillsUseCase
    {
        private readonly ISkillRepository _skillRepository;

        public SkillsUseCase(ISkillRepository skillRepository)
        {
            _skillRepository = skillRepository;
        }

        public async Task<ResponseBase<List<SkillDto>>> GetActiveSkillsAsync()
        {
            var skillsWithCategory = await _skillRepository.GetActiveSkillsWithCategoryAsync();
            var dtos = skillsWithCategory.Select(sc => new SkillDto
            {
                Id = sc.Skill.Id,
                Name = sc.Skill.Name,
                CategoryId = sc.Skill.CategoryId,
                CategoryName = sc.CategoryName
            }).ToList();

            return new ResponseBase<List<SkillDto>>(dtos);
        }
    }
}
