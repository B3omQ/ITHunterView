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
        /// Get paged list of users with advanced search filters
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
        /// Get detailed information and profile (candidate/recruiter/company) of a user
        /// </summary>
        [HttpGet("users/{id:guid}")]
        public async Task<IActionResult> GetUserDetail(Guid id)
        {
            var result = await _userUseCase.GetUserDetailAsync(id);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpPut("users/{id:guid}/status")]
        public async Task<IActionResult> UpdateUserStatus(Guid id, [FromBody] UpdateUserStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseBase.Fail("Invalid input data."));

            var result = await _userUseCase.UpdateUserStatusAsync(id, dto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Create a new Staff account (Admin Only)
        /// </summary>
        [HttpPost("staff")]
        [EnableRateLimiting("StaffCreationPolicy")]
        public async Task<IActionResult> CreateStaff([FromBody] CreateStaffDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseBase.Fail("Invalid input data."));

            var result = await _userUseCase.CreateStaffAccountAsync(dto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        
    }
}
