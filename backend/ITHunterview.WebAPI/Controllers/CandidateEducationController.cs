using System.Security.Claims;
using ITHunterview.Service.DTOs.CandidateProfile;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.MasterData;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Authorize(Roles = "candidate")]
    public class CandidateEducationController : ControllerBase
    {
        private readonly ICandidateEducationUseCase _eduUseCase;

        public CandidateEducationController(ICandidateEducationUseCase eduUseCase)
        {
            _eduUseCase = eduUseCase;
        }

        // ─── Master data — Majors ─────────────────────────────────────────────

        /// <summary>GET /api/v1/majors — Dropdown data for education form</summary>
        [HttpGet("api/v1/majors")]
        public async Task<ActionResult<ResponseBase<List<MajorResponseDto>>>> GetMajors()
        {
            var result = await _eduUseCase.GetAllMajorsAsync();
            return Ok(new ResponseBase<List<MajorResponseDto>>(result));
        }

        // ─── E. Tab Education ─────────────────────────────────────────────────

        /// <summary>GET /api/v1/candidate/profile/educations</summary>
        [HttpGet("api/v1/candidate/profile/educations")]
        public async Task<ActionResult<ResponseBase<List<EducationResponseDto>>>> GetEducations()
        {
            var userId = GetUserId();
            var result = await _eduUseCase.GetEducationsAsync(userId);
            return Ok(new ResponseBase<List<EducationResponseDto>>(result));
        }

        /// <summary>POST /api/v1/candidate/profile/educations</summary>
        [HttpPost("api/v1/candidate/profile/educations")]
        public async Task<ActionResult<ResponseBase<EducationResponseDto>>> CreateEducation([FromBody] EducationUpsertRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();
            var result = await _eduUseCase.CreateEducationAsync(userId, request);
            return Ok(new ResponseBase<EducationResponseDto>(result));
        }

        /// <summary>PUT /api/v1/candidate/profile/educations/{id}</summary>
        [HttpPut("api/v1/candidate/profile/educations/{id:guid}")]
        public async Task<ActionResult<ResponseBase<EducationResponseDto>>> UpdateEducation(
            Guid id, [FromBody] EducationUpsertRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();
            var result = await _eduUseCase.UpdateEducationAsync(userId, id, request);
            return Ok(new ResponseBase<EducationResponseDto>(result));
        }

        /// <summary>DELETE /api/v1/candidate/profile/educations/{id}</summary>
        [HttpDelete("api/v1/candidate/profile/educations/{id:guid}")]
        public async Task<ActionResult<ResponseBase<bool>>> DeleteEducation(Guid id)
        {
            var userId = GetUserId();
            var result = await _eduUseCase.DeleteEducationAsync(userId, id);
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
