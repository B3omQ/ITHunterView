using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Job;
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
            var skills = await _skillRepository.GetActiveSkillsAsync();
            var dtos = skills.Select(s => new SkillDto
            {
                Id = s.Id,
                Name = s.Name
            }).ToList();

            return new ResponseBase<List<SkillDto>>(dtos);
        }
    }
}
