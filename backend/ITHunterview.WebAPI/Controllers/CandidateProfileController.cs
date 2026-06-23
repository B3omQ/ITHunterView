using System.Security.Claims;
using ITHunterview.Service.DTOs.CandidateProfile;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("api/v1/candidate/profile")]
    [Authorize(Roles = "candidate")]
    public class CandidateProfileController : ControllerBase
    {
        private readonly ICandidateProfileUseCase _profileUseCase;

        public CandidateProfileController(ICandidateProfileUseCase profileUseCase)
        {
            _profileUseCase = profileUseCase;
        }

        // ─── A. Profile Header ────────────────────────────────────────────────

        /// <summary>GET /api/v1/candidate/profile/summary — Profile header data (computed)</summary>
        [HttpGet("summary")]
        public async Task<ActionResult<ResponseBase<ProfileSummaryResponseDto>>> GetSummary()
        {
            var userId = GetUserId();
            var result = await _profileUseCase.GetProfileSummaryAsync(userId);
            return Ok(new ResponseBase<ProfileSummaryResponseDto>(result));
        }

        /// <summary>PATCH /api/v1/candidate/profile/visibility — Toggle recruiter visibility</summary>
        [HttpPatch("visibility")]
        public async Task<ActionResult<ResponseBase<bool>>> SetVisibility([FromBody] ProfileVisibilityRequestDto request)
        {
            var userId = GetUserId();
            var result = await _profileUseCase.SetVisibilityAsync(userId, request.IsVisibleToRecruiters);
            return Ok(new ResponseBase<bool>(result));
        }

        /// <summary>POST /api/v1/candidate/profile/avatar — Upload/replace avatar</summary>
        [HttpPost("avatar")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ResponseBase<AvatarUploadResponseDto>>> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new ResponseBase<AvatarUploadResponseDto>("Vui lòng chọn ảnh để upload."));

            var userId = GetUserId();
            await using var stream = file.OpenReadStream();
            var result = await _profileUseCase.UploadAvatarAsync(userId, stream, file.FileName, file.ContentType, file.Length);
            return Ok(new ResponseBase<AvatarUploadResponseDto>(result));
        }

        // ─── B. Tab Personal Info ─────────────────────────────────────────────

        /// <summary>GET /api/v1/candidate/profile/personal-info</summary>
        [HttpGet("personal-info")]
        public async Task<ActionResult<ResponseBase<PersonalInfoResponseDto>>> GetPersonalInfo()
        {
            var userId = GetUserId();
            var result = await _profileUseCase.GetPersonalInfoAsync(userId);
            return Ok(new ResponseBase<PersonalInfoResponseDto>(result));
        }

        /// <summary>PUT /api/v1/candidate/profile/personal-info — Save all personal info fields</summary>
        [HttpPut("personal-info")]
        public async Task<ActionResult<ResponseBase<PersonalInfoResponseDto>>> UpdatePersonalInfo([FromBody] PersonalInfoUpdateRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();
            var result = await _profileUseCase.UpdatePersonalInfoAsync(userId, request);
            return Ok(new ResponseBase<PersonalInfoResponseDto>(result));
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
