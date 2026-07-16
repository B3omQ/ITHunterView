using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Wallet;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayOS;
using PayOS.Models;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("api/v1/wallet")]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly IWalletUseCase _walletUseCase;
        private readonly PayOSClient _payOS;

        public WalletController(IWalletUseCase walletUseCase, PayOSClient payOS)
        {
            _walletUseCase = walletUseCase;
            _payOS = payOS;
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return null;
            return userId;
        }

        /// <summary>
        /// Xem số dư ví hiện tại của Candidate
        /// </summary>
        [HttpGet("balance")]
        [Authorize(Policy = "CandidateOnly")]
        public async Task<IActionResult> GetWalletBalance()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var result = await _walletUseCase.GetWalletBalanceAsync(userId.Value);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Xem lịch sử giao dịch tiêu tốn/nạp coin của Candidate
        /// </summary>
        [HttpGet("transactions")]
        [Authorize(Policy = "CandidateOnly")]
        public async Task<IActionResult> GetWalletTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var result = await _walletUseCase.GetWalletTransactionsAsync(userId.Value, page, pageSize);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Candidate tạo yêu cầu thanh toán mua coin hoặc mua subscription
        /// </summary>
        [HttpPost("pay")]
        [Authorize(Policy = "CandidateOrRecruiter")]
        public async Task<IActionResult> CreatePaymentRequest([FromBody] CreatePaymentDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var result = await _walletUseCase.CreatePaymentRequestAsync(userId.Value, dto);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Xem lịch sử các giao dịch thanh toán của toàn hệ thống (Admin & Staff)
        /// </summary>
        [HttpGet("admin/payments")]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<IActionResult> GetPagedPayments([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _walletUseCase.GetPagedPaymentsAsync(page, pageSize);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Admin giả lập kết quả callback từ cổng thanh toán Momo/VNPay để cập nhật ví/subscription
        /// </summary>
        [HttpPost("admin/payments/simulate")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> SimulatePaymentCallback([FromBody] PaymentSimulationDto dto)
        {
            var actorUserId = GetCurrentUserId();
            if (actorUserId == null) return Unauthorized();

            var result = await _walletUseCase.ProcessPaymentCallbackAsync(actorUserId.Value, dto);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Webhook URL cung cấp cho PayOS
        /// </summary>
        [HttpPost("payos-webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> HandlePayOsWebhook([FromBody] PayOS.Models.Webhooks.Webhook webhookBody)
        {
            try
            {
                PayOS.Models.Webhooks.WebhookData data = await _payOS.Webhooks.VerifyAsync(webhookBody);

                if (data.Description == "Ma test" || data.Description == "VQRIO123")
                {
                    return Ok(new { success = true, message = "Test webhook ignored" });
                }

                await _walletUseCase.ProcessWebhookAsync(data.OrderCode, data.TransactionDateTime);

                return Ok(new { success = true, message = "Webhook processed" });
            }
            catch (Exception ex)
            {
                // PayOS yêu cầu trả về 200 kể cả khi xử lý lỗi nội bộ để tránh retry-loop
                return Ok(new { success = false, message = ex.Message });
            }
        }
    }
}
