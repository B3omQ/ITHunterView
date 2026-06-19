using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("api/me")]
    [Authorize]
    public class MeController : ControllerBase
    {
        private readonly IUserRepository _userRepository;

        public MeController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        /// <summary>
        /// Lấy thông tin người dùng hiện tại từ JWT token
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMe()
        {
            var userIdClaim = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var user = await _userRepository.GetUserWithRoleAsync(userId);
            if (user == null)
                return NotFound(ResponseBase.Fail("Người dùng không tồn tại."));

            string fullName = user.Email;
            string? avatarUrl = null;

            if (user.CandidateProfile != null)
            {
                fullName = $"{user.CandidateProfile.FirstName} {user.CandidateProfile.LastName}".Trim();
                if (string.IsNullOrEmpty(fullName)) fullName = user.Email;
                avatarUrl = user.CandidateProfile.AvatarUrl;
            }
            else if (user.RecruiterProfile != null)
            {
                fullName = user.RecruiterProfile.FullName ?? user.Email;
                avatarUrl = user.RecruiterProfile.AvatarUrl;
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    userId = user.Id,
                    email = user.Email,
                    fullName,
                    role = user.Role?.Name,
                    avatarUrl,
                    status = user.Status.ToString(),
                    createdAt = user.CreatedAt
                }
            });
        }
    }
}
