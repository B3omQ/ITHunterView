using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.MasterData;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("api/master-data")]
    [Authorize(Policy = "AdminOnly")]
    public class MasterDataController : ControllerBase
    {
        private readonly ISkillUseCase _skillUseCase;
        private readonly IMajorUseCase _majorUseCase;

        public MasterDataController(ISkillUseCase skillUseCase, IMajorUseCase majorUseCase)
        {
            _skillUseCase = skillUseCase;
            _majorUseCase = majorUseCase;
        }

        // ─── SKILLS ─────────────────────────────────────────────────────────────

        [HttpGet("skills")]
        public async Task<IActionResult> GetPagedSkills(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] SkillStatus? status = null)
        {
            var response = await _skillUseCase.GetPagedSkillsAsync(page, pageSize, search, categoryId, status);
            return Ok(response);
        }

        [HttpGet("skills/categories")]
        public async Task<IActionResult> GetSkillCategories()
        {
            var response = await _skillUseCase.GetAllCategoriesAsync();
            return Ok(response);
        }

        [HttpPost("skills")]
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

        [HttpPut("skills/{id}")]
        public async Task<IActionResult> UpdateSkill([FromRoute] int id, [FromBody] UpdateSkillDto dto)
        {
            var userIdClaim = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var response = await _skillUseCase.UpdateSkillAsync(id, dto, userId);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpPatch("skills/{id}/status")]
        public async Task<IActionResult> UpdateSkillStatus([FromRoute] int id, [FromBody] UpdateSkillStatusDto dto)
        {
            var userIdClaim = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var response = await _skillUseCase.UpdateSkillStatusAsync(id, dto, userId);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpDelete("skills/{id}")]
        public async Task<IActionResult> DeleteSkill([FromRoute] int id)
        {
            var response = await _skillUseCase.DeleteSkillAsync(id);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        // ─── MAJORS ─────────────────────────────────────────────────────────────

        [HttpGet("majors")]
        public async Task<IActionResult> GetPagedMajors(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            var response = await _majorUseCase.GetPagedMajorsAsync(page, pageSize, search);
            return Ok(response);
        }

        [HttpGet("majors/tree")]
        public async Task<IActionResult> GetMajorTree(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            var response = await _majorUseCase.GetMajorTreeAsync(page, pageSize, search);
            return Ok(response);
        }

        [HttpPost("majors")]
        public async Task<IActionResult> CreateMajor([FromBody] CreateMajorDto dto)
        {
            var userIdClaim = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var response = await _majorUseCase.CreateMajorAsync(dto, userId);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpPut("majors/{id}")]
        public async Task<IActionResult> UpdateMajor([FromRoute] int id, [FromBody] UpdateMajorDto dto)
        {
            var userIdClaim = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var response = await _majorUseCase.UpdateMajorAsync(id, dto, userId);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpDelete("majors/{id}")]
        public async Task<IActionResult> DeleteMajor([FromRoute] int id)
        {
            var userIdClaim = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var response = await _majorUseCase.DeleteMajorAsync(id, userId);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpPost("majors/{id}/restore")]
        public async Task<IActionResult> RestoreMajor([FromRoute] int id)
        {
            var userIdClaim = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var response = await _majorUseCase.RestoreMajorAsync(id, userId);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }
    }
}
