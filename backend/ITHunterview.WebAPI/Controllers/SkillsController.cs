using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.MasterData;
using ITHunterview.Service.DTOs.Skill;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SkillsController : ControllerBase
    {
        private readonly ISkillsUseCase _skillsUseCase;
        private readonly ISkillUseCase _skillUseCase;

        public SkillsController(ISkillsUseCase skillsUseCase, ISkillUseCase skillUseCase)
        {
            _skillsUseCase = skillsUseCase;
            _skillUseCase = skillUseCase;
        }

        [HttpGet]
        public async Task<ActionResult<ResponseBase<List<ITHunterview.Service.DTOs.Skill.SkillDto>>>> GetActiveSkills()
        {
            var result = await _skillsUseCase.GetActiveSkillsAsync();
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "RecruiterOnly")]
        public async Task<IActionResult> CreateSkill([FromBody] CreateSkillDto dto)
        {
            var userIdClaim = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var response = await _skillUseCase.CreateSkillAsync(dto, userId);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }
    }
}
