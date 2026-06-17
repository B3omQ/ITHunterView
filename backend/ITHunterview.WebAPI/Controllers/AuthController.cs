using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Auth;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthUseCase _authUseCase;

        public AuthController(IAuthUseCase authUseCase)
        {
            _authUseCase = authUseCase;
        }

        [HttpPost("login")]
        public async Task<ActionResult<ResponseBase<LoginResponseDto>>> Login([FromBody] LoginRequestDto request)
        {
            var result = await _authUseCase.LoginAsync(request);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("seed")]
        public async Task<IActionResult> SeedUser([FromServices] ITHunterview.Service.Interface.Persistence.IUserRepository userRepository)
        {
            var user = new ITHunterview.Domain.Entities.User
            {
                Email = "test@example.com",
                FullName = "Test User",
                PasswordHash = ITHunterview.Service.Utils.PasswordHasher.HashPassword("123456")
            };
            await userRepository.AddUserAsync(user);
            return Ok("User seeded. Email: test@example.com, Password: 123456");
        }
    }
}
