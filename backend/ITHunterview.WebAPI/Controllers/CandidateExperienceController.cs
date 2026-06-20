using System.Security.Claims;
using ITHunterview.Service.DTOs.CandidateProfile;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("api/v1/candidate/profile/experiences")]
    [Authorize(Roles = "candidate")]
    public class CandidateExperienceController : ControllerBase
    {
        private readonly ICandidateExperienceUseCase _expUseCase;

        public CandidateExperienceController(ICandidateExperienceUseCase expUseCase)
        {
            _expUseCase = expUseCase;
        }

        // ─── D. Tab Experience ────────────────────────────────────────────────

        /// <summary>GET /api/v1/candidate/profile/experiences</summary>
        [HttpGet]
        public async Task<ActionResult<ResponseBase<List<ExperienceResponseDto>>>> GetExperiences()
        {
            var userId = GetUserId();
            var result = await _expUseCase.GetExperiencesAsync(userId);
            return Ok(new ResponseBase<List<ExperienceResponseDto>>(result));
        }

        /// <summary>POST /api/v1/candidate/profile/experiences</summary>
        [HttpPost]
        public async Task<ActionResult<ResponseBase<ExperienceResponseDto>>> CreateExperience([FromBody] ExperienceUpsertRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();
            var result = await _expUseCase.CreateExperienceAsync(userId, request);
            return Ok(new ResponseBase<ExperienceResponseDto>(result));
        }

        /// <summary>PUT /api/v1/candidate/profile/experiences/{id}</summary>
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ResponseBase<ExperienceResponseDto>>> UpdateExperience(
            Guid id, [FromBody] ExperienceUpsertRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();
            var result = await _expUseCase.UpdateExperienceAsync(userId, id, request);
            return Ok(new ResponseBase<ExperienceResponseDto>(result));
        }

        /// <summary>DELETE /api/v1/candidate/profile/experiences/{id}</summary>
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ResponseBase<bool>>> DeleteExperience(Guid id)
        {
            var userId = GetUserId();
            var result = await _expUseCase.DeleteExperienceAsync(userId, id);
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
