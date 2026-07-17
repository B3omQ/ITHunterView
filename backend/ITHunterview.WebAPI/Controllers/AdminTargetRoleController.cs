using System;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.LearningPath;
using ITHunterview.Service.DTOs.MasterData;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("api/master-data/target-roles")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminTargetRoleController : ControllerBase
    {
        private readonly ITargetRoleUseCase _targetRoleUseCase;

        public AdminTargetRoleController(ITargetRoleUseCase targetRoleUseCase)
        {
            _targetRoleUseCase = targetRoleUseCase;
        }

        [HttpGet]
        public async Task<IActionResult> GetPagedRoles(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            var response = await _targetRoleUseCase.GetPagedRolesAsync(page, pageSize, search);
            return Ok(new ResponseBase<PagedTargetRoleResponseDto>(response));
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] CreateTargetRoleTemplateDto dto)
        {
            var response = await _targetRoleUseCase.CreateRoleAsync(dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateRole([FromRoute] Guid id, [FromBody] UpdateTargetRoleTemplateDto dto)
        {
            var response = await _targetRoleUseCase.UpdateRoleAsync(id, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteRole([FromRoute] Guid id)
        {
            var response = await _targetRoleUseCase.DeleteRoleAsync(id);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        
        [HttpPost("import")]
        public async Task<IActionResult> ImportTargetRoles(Microsoft.AspNetCore.Http.IFormFile file)
        {
            var response = await _targetRoleUseCase.ImportTargetRolesAsync(file);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        
        [HttpGet("sfia-skills")]
        public async Task<IActionResult> GetAllSfiaSkills()
        {
            var response = await _targetRoleUseCase.GetAllSfiaSkillsAsync();
            return Ok(new ResponseBase<System.Collections.Generic.List<SfiaSkillDto>>(response));
        }
    }
}
