using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Wallet;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("api/admin/wallet")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminWalletController : ControllerBase
    {
        private readonly IWalletUseCase _walletUseCase;

        public AdminWalletController(IWalletUseCase walletUseCase)
        {
            _walletUseCase = walletUseCase;
        }

        [HttpGet("custom-coin-price")]
        public async Task<IActionResult> GetCustomCoinPrice()
        {
            var result = await _walletUseCase.GetCustomCoinTopupPriceAsync();
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("custom-coin-price")]
        public async Task<IActionResult> UpdateCustomCoinPrice([FromBody] CustomCoinTopupPriceDto dto)
        {
            var userIdClaim = User.FindFirstValue("userId");
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ResponseBase.Fail("Không xác định được tài khoản quản trị viên."));
            }

            var result = await _walletUseCase.UpdateCustomCoinTopupPriceAsync(dto, userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
