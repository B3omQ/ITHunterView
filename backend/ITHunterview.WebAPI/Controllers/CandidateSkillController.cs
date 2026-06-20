using System.Security.Claims;
using ITHunterview.Service.DTOs.CandidateProfile;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Authorize(Roles = "Candidate")]
    public class CandidateSkillController : ControllerBase
    {
        private readonly ICandidateSkillUseCase _skillUseCase;

        public CandidateSkillController(ICandidateSkillUseCase skillUseCase)
        {
            _skillUseCase = skillUseCase;
        }

        // ─── C. Tab Skills ────────────────────────────────────────────────────

        /// <summary>GET /api/v1/candidate/profile/skills — List own skills</summary>
        [HttpGet("api/v1/candidate/profile/skills")]
        public async Task<ActionResult<ResponseBase<List<SkillResponseDto>>>> GetSkills()
        {
            var userId = GetUserId();
            var (skills, totalCount) = await _skillUseCase.GetSkillsAsync(userId);
            return Ok(new ResponseBase<List<SkillResponseDto>>(skills)
            {
                Message = $"totalCount:{totalCount}"
            });
        }

        /// <summary>GET /api/v1/skills/search?keyword=...&amp;excludeOwned=true — Master data autocomplete</summary>
        [HttpGet("api/v1/skills/search")]
        public async Task<ActionResult<ResponseBase<List<SkillSearchResponseDto>>>> SearchSkills(
            [FromQuery] string keyword = "",
            [FromQuery] bool excludeOwned = false)
        {
            var userId = GetUserId();
            var result = await _skillUseCase.SearchSkillsAsync(keyword, excludeOwned, userId);
            return Ok(new ResponseBase<List<SkillSearchResponseDto>>(result));
        }

        /// <summary>GET /api/v1/candidate/profile/skills/suggestions — Trending suggestions</summary>
        [HttpGet("api/v1/candidate/profile/skills/suggestions")]
        public async Task<ActionResult<ResponseBase<List<SkillResponseDto>>>> GetSuggestions()
        {
            var userId = GetUserId();
            var result = await _skillUseCase.GetSuggestionsAsync(userId);
            return Ok(new ResponseBase<List<SkillResponseDto>>(result));
        }

        /// <summary>POST /api/v1/candidate/profile/skills — Add a skill</summary>
        [HttpPost("api/v1/candidate/profile/skills")]
        public async Task<ActionResult<ResponseBase<SkillResponseDto>>> AddSkill([FromBody] SkillAddRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();
            var result = await _skillUseCase.AddSkillAsync(userId, request);
            return Ok(new ResponseBase<SkillResponseDto>(result));
        }

        /// <summary>DELETE /api/v1/candidate/profile/skills/{skillId} — Remove a skill chip</summary>
        [HttpDelete("api/v1/candidate/profile/skills/{skillId:int}")]
        public async Task<ActionResult<ResponseBase<bool>>> RemoveSkill(int skillId)
        {
            var userId = GetUserId();
            var result = await _skillUseCase.RemoveSkillAsync(userId, skillId);
            return Ok(new ResponseBase<bool>(result));
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        private Guid GetUserId()
        {
            var claim = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(claim) || !Guid.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException("Token không hợp lệ.");
            return userId;
        }
    }
}
