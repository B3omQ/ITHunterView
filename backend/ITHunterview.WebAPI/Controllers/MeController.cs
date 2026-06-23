using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.User;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("api/me")]
    [Authorize]
    public class MeController : ControllerBase
    {
        private readonly IUserUseCase _userUseCase;

        public MeController(IUserUseCase userUseCase)
        {
            _userUseCase = userUseCase;
        }

        /// <summary>
        /// Lấy thông tin người dùng hiện tại từ JWT token
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ResponseBase<UserMeDto>>> GetMe()
        {
            var userIdClaim = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var result = await _userUseCase.GetMeAsync(userId);
            return Ok(result);
        }
    }
}
