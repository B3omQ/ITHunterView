using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Auth;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthUseCase _authUseCase;

        public AuthController(IAuthUseCase authUseCase)
        {
            _authUseCase = authUseCase;
        }

        /// <summary>
        /// Đăng nhập bằng email và mật khẩu
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<ResponseBase<LoginResponseDto>>> Login([FromBody] LoginRequestDto request)
        {
            var result = await _authUseCase.LoginAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Đăng ký tài khoản mới (candidate hoặc recruiter)
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<ResponseBase<LoginResponseDto>>> Register([FromBody] RegisterRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authUseCase.RegisterAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Đăng nhập / đăng ký bằng Google
        /// </summary>
        [HttpPost("google")]
        public async Task<ActionResult<ResponseBase<LoginResponseDto>>> GoogleAuth([FromBody] GoogleAuthRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authUseCase.GoogleAuthAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Làm mới access token bằng refresh token
        /// </summary>
        [HttpPost("refresh-token")]
        public async Task<ActionResult<ResponseBase<LoginResponseDto>>> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            var result = await _authUseCase.RefreshTokenAsync(request);
            return result.Success ? Ok(result) : Unauthorized(result);
        }

        /// <summary>
        /// Đăng xuất (thu hồi refresh token)
        /// </summary>
        [Authorize]
        [HttpPost("logout")]
        public async Task<ActionResult<ResponseBase>> Logout([FromBody] LogoutRequestDto request)
        {
            var result = await _authUseCase.LogoutAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Đổi mật khẩu (yêu cầu đăng nhập)
        /// </summary>
        [Authorize]
        [HttpPost("change-password")]
        public async Task<ActionResult<ResponseBase>> ChangePassword([FromBody] ChangePasswordRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var result = await _authUseCase.ChangePasswordAsync(userId, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Gửi email quên mật khẩu
        /// </summary>
        [HttpPost("forgot-password")]
        public async Task<ActionResult<ResponseBase>> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authUseCase.ForgotPasswordAsync(request);
            return Ok(result); // Always 200 to prevent email enumeration
        }

        /// <summary>
        /// Đặt lại mật khẩu bằng token từ email
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<ActionResult<ResponseBase>> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authUseCase.ResetPasswordAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Xác thực email từ link trong email
        /// </summary>
        [HttpGet("verify-email")]
        public async Task<ActionResult<ResponseBase>> VerifyEmail([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
                return BadRequest(ResponseBase.Fail("Token không hợp lệ."));

            var result = await _authUseCase.VerifyEmailAsync(token);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Gửi lại email xác thực khi link cũ hết hạn
        /// </summary>
        [HttpPost("resend-verification")]
        public async Task<ActionResult<ResponseBase>> ResendVerification([FromBody] ForgotPasswordRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authUseCase.ResendVerificationEmailAsync(request.Email);
            return Ok(result); // Always 200 to prevent email enumeration
        }
    }
}
