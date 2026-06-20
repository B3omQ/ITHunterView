using System.Security.Claims;
using ITHunterview.Service.DTOs.CandidateProfile;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("api/v1/candidate/profile/certifications")]
    [Authorize(Roles = "candidate")]
    public class CandidateCertificationController : ControllerBase
    {
        private readonly ICandidateCertificationUseCase _certUseCase;

        public CandidateCertificationController(ICandidateCertificationUseCase certUseCase)
        {
            _certUseCase = certUseCase;
        }

        // ─── F. Tab Education → Certifications ───────────────────────────────

        /// <summary>GET /api/v1/candidate/profile/certifications</summary>
        [HttpGet]
        public async Task<ActionResult<ResponseBase<List<CertificationResponseDto>>>> GetCertifications()
        {
            var userId = GetUserId();
            var result = await _certUseCase.GetCertificationsAsync(userId);
            return Ok(new ResponseBase<List<CertificationResponseDto>>(result));
        }

        /// <summary>POST /api/v1/candidate/profile/certifications</summary>
        [HttpPost]
        public async Task<ActionResult<ResponseBase<CertificationResponseDto>>> CreateCertification([FromBody] CertificationUpsertRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();
            var result = await _certUseCase.CreateCertificationAsync(userId, request);
            return Ok(new ResponseBase<CertificationResponseDto>(result));
        }

        /// <summary>PUT /api/v1/candidate/profile/certifications/{id}</summary>
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ResponseBase<CertificationResponseDto>>> UpdateCertification(
            Guid id, [FromBody] CertificationUpsertRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();
            var result = await _certUseCase.UpdateCertificationAsync(userId, id, request);
            return Ok(new ResponseBase<CertificationResponseDto>(result));
        }

        /// <summary>DELETE /api/v1/candidate/profile/certifications/{id}</summary>
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ResponseBase<bool>>> DeleteCertification(Guid id)
        {
            var userId = GetUserId();
            var result = await _certUseCase.DeleteCertificationAsync(userId, id);
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
