using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Subscription;
using ITHunterview.Service.DTOs.CoinConfig;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("api/admin/subscriptions")]
    [Authorize(Policy = "AdminOnly")]
    public class SubscriptionAdminController : ControllerBase
    {
        private readonly ISubscriptionAdminUseCase _subscriptionUseCase;
        private readonly ICoinConfigUseCase _coinConfigUseCase;

        public SubscriptionAdminController(
            ISubscriptionAdminUseCase subscriptionUseCase,
            ICoinConfigUseCase coinConfigUseCase)
        {
            _subscriptionUseCase = subscriptionUseCase;
            _coinConfigUseCase = coinConfigUseCase;
        }

        /// <summary>
        /// Lấy danh sách gói Subscription phân trang kèm bộ lọc
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPagedSubscriptions(
            [FromQuery] string? role = null,
            [FromQuery] SubscriptionStatus? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _subscriptionUseCase.GetPagedSubscriptionsAsync(role, status, page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết một gói Subscription
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetSubscriptionById(int id)
        {
            var result = await _subscriptionUseCase.GetSubscriptionByIdAsync(id);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// Tạo mới một gói Subscription (Mặc định ở trạng thái INACTIVE)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(ResponseBase.Fail($"Dữ liệu đầu vào không hợp lệ: {errors}"));
            }

            var userIdClaim = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var result = await _subscriptionUseCase.CreateSubscriptionAsync(dto, userId);
            if (!result.Success)
                return BadRequest(result);

            return CreatedAtAction(nameof(GetSubscriptionById), new { id = result.Data!.Id }, result);
        }

        /// <summary>
        /// Cập nhật thông tin gói Subscription (Chặn đổi thuộc tính nhạy cảm nếu gói đã bán)
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateSubscription(int id, [FromBody] UpdateSubscriptionDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(ResponseBase.Fail($"Dữ liệu đầu vào không hợp lệ: {errors}"));
            }

            var userIdClaim = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var result = await _subscriptionUseCase.UpdateSubscriptionAsync(id, dto, userId);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Cập nhật trạng thái hoạt động (ACTIVE/INACTIVE) của gói Subscription
        /// </summary>
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateSubscriptionStatusRequest request)
        {
            if (request == null || !Enum.IsDefined(typeof(SubscriptionStatus), request.Status))
            {
                return BadRequest(ResponseBase.Fail("Trạng thái không hợp lệ. Chỉ chấp nhận ACTIVE hoặc INACTIVE."));
            }

            var userIdClaim = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var result = await _subscriptionUseCase.UpdateStatusAsync(id, request.Status, userId);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Nhân bản (Duplicate) một gói Subscription hiện tại ở trạng thái INACTIVE để tiện cấu hình
        /// </summary>
        [HttpPost("{id:int}/duplicate")]
        public async Task<IActionResult> DuplicateSubscription(int id)
        {
            var userIdClaim = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var result = await _subscriptionUseCase.DuplicateSubscriptionAsync(id, userId);
            if (!result.Success)
                return BadRequest(result);

            return CreatedAtAction(nameof(GetSubscriptionById), new { id = result.Data!.Id }, result);
        }

        /// <summary>
        /// Lấy cấu hình Coin hiện tại (Chi phí tính năng & danh sách gói nạp Coin)
        /// </summary>
        [HttpGet("coin-config")]
        public async Task<IActionResult> GetCoinConfig()
        {
            var result = await _coinConfigUseCase.GetCoinConfigAsync();
            return Ok(result);
        }

        /// <summary>
        /// Cập nhật cấu hình Coin (Chi phí tính năng & danh sách gói nạp Coin)
        /// </summary>
        [HttpPut("coin-config")]
        public async Task<IActionResult> UpdateCoinConfig([FromBody] UpdateCoinConfigDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(ResponseBase.Fail($"Dữ liệu cấu hình không hợp lệ: {errors}"));
            }

            var userIdClaim = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var result = await _coinConfigUseCase.UpdateCoinConfigAsync(dto, userId);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
    }

    public class UpdateSubscriptionStatusRequest
    {
        public SubscriptionStatus Status { get; set; }
    }
}
