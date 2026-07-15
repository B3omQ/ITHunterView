using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.MasterData;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("api/master-data/sfia-skills")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminSfiaSkillController : ControllerBase
    {
        private readonly ISfiaSkillUseCase _sfiaSkillUseCase;

        public AdminSfiaSkillController(ISfiaSkillUseCase sfiaSkillUseCase)
        {
            _sfiaSkillUseCase = sfiaSkillUseCase;
        }

        [HttpGet]
        public async Task<IActionResult> GetPagedSfiaSkills(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            var response = await _sfiaSkillUseCase.GetPagedSfiaSkillsAsync(page, pageSize, search);
            return Ok(new ResponseBase<PagedSfiaSkillResponseDto>(response));
        }

        [HttpPost]
        public async Task<IActionResult> CreateSfiaSkill([FromBody] CreateSfiaSkillDto dto)
        {
            var result = await _sfiaSkillUseCase.CreateSfiaSkillAsync(dto);
            return Ok(new ResponseBase<SfiaSkillResponseDto>(result, "SFIA Skill created successfully."));
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateSfiaSkill([FromRoute] Guid id, [FromBody] UpdateSfiaSkillDto dto)
        {
            var result = await _sfiaSkillUseCase.UpdateSfiaSkillAsync(id, dto);
            return Ok(new ResponseBase<SfiaSkillResponseDto>(result, "SFIA Skill updated successfully."));
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteSfiaSkill([FromRoute] Guid id)
        {
            await _sfiaSkillUseCase.DeleteSfiaSkillAsync(id);
            return Ok(new ResponseBase<bool>(true, "SFIA Skill deleted successfully."));
        }

        [HttpPost("import")]
        public async Task<IActionResult> ImportSfiaSkills(IFormFile file)
        {
            var totalImported = await _sfiaSkillUseCase.ImportSfiaSkillsAsync(file);
            return Ok(new ResponseBase<int>(totalImported, $"Import completed successfully. Processed {totalImported} skills."));
        }
    }
}
