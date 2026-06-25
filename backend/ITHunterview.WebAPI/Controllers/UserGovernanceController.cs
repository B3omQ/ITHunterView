using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.UserGovernance;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("api/user-governance")]
    [Authorize(Policy = "AdminOnly")]
    public class UserGovernanceController : ControllerBase
    {
        private readonly IUserGovernanceUseCase _userUseCase;

        public UserGovernanceController(IUserGovernanceUseCase userUseCase)
        {
            _userUseCase = userUseCase;
        }

        /// <summary>
        /// Lấy danh sách người dùng phân trang kèm bộ lọc tìm kiếm nâng cao
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetPagedUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] int? roleId = null,
            [FromQuery] UserStatus? status = null)
        {
            var result = await _userUseCase.GetPagedUsersAsync(page, pageSize, search, roleId, status);
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết thông tin và hồ sơ (candidate/recruiter/company) của một người dùng
        /// </summary>
        [HttpGet("users/{id:guid}")]
        public async Task<IActionResult> GetUserDetail(Guid id)
        {
            var result = await _userUseCase.GetUserDetailAsync(id);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// Cập nhật trạng thái hoạt động (Active/Inactive/Banned) của người dùng
        /// </summary>
        [HttpPut("users/{id:guid}/status")]
        public async Task<IActionResult> UpdateUserStatus(Guid id, [FromBody] UpdateUserStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseBase.Fail("Invalid input data."));

            var actorIdClaim = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(actorIdClaim) || !Guid.TryParse(actorIdClaim, out var actorId))
                return Unauthorized();

            var actorEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email") ?? "unknown";
            var actorRole = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? "unknown";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = Request.Headers["User-Agent"].ToString() ?? "unknown";

            var result = await _userUseCase.UpdateUserStatusAsync(
                id, 
                dto, 
                actorId, 
                actorEmail, 
                actorRole, 
                ipAddress, 
                userAgent);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }



        /// <summary>
        /// Tạo tài khoản Staff mới (Chỉ Admin)
        /// </summary>
        [HttpPost("staff")]
        [EnableRateLimiting("StaffCreationPolicy")]
        public async Task<IActionResult> CreateStaff([FromBody] CreateStaffDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseBase.Fail("Invalid input data."));

            var actorIdClaim = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(actorIdClaim) || !Guid.TryParse(actorIdClaim, out var actorId))
                return Unauthorized();

            var actorEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email") ?? "unknown";
            var actorRole = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? "unknown";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = Request.Headers["User-Agent"].ToString() ?? "unknown";

            var result = await _userUseCase.CreateStaffAccountAsync(
                dto, 
                actorId, 
                actorEmail, 
                actorRole, 
                ipAddress, 
                userAgent);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Tạo tài khoản Staff mới (Chỉ Admin)
        /// </summary>
        [HttpPost("staff")]
        public async Task<IActionResult> CreateStaff([FromBody] CreateStaffDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseBase.Fail("Dữ liệu đầu vào không hợp lệ."));

            var actorIdClaim = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(actorIdClaim) || !Guid.TryParse(actorIdClaim, out var actorId))
                return Unauthorized();

            var actorEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email") ?? "unknown";
            var actorRole = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? "unknown";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = Request.Headers["User-Agent"].ToString() ?? "unknown";

            var result = await _userUseCase.CreateStaffAccountAsync(
                dto, 
                actorId, 
                actorEmail, 
                actorRole, 
                ipAddress, 
                userAgent);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
    }
}
