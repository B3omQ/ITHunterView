using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Skill;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SkillsController : ControllerBase
    {
        private readonly ISkillsUseCase _skillsUseCase;

        public SkillsController(ISkillsUseCase skillsUseCase)
        {
            _skillsUseCase = skillsUseCase;
        }

        [HttpGet]
        public async Task<ActionResult<ResponseBase<List<SkillDto>>>> GetActiveSkills()
        {
            var result = await _skillsUseCase.GetActiveSkillsAsync();
            return Ok(result);
        }
    }
}
