using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Notification;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationUseCase _notificationUseCase;

        public NotificationsController(INotificationUseCase notificationUseCase)
        {
            _notificationUseCase = notificationUseCase;
        }

        [HttpPost("system-wide")]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<ActionResult<ResponseBase<bool>>> CreateSystemWideNotification([FromBody] CreateSystemNotificationDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _notificationUseCase.CreateSystemWideNotificationAsync(request);

            return Ok(new ResponseBase<bool>
            {
                Success = true,
                Message = "System-wide notification created successfully",
                Data = result
            });
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<PaginatedDataResponse<NotificationDto>>> GetUserNotifications([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            var userIdStr = User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var result = await _notificationUseCase.GetUserNotificationsAsync(userId, pageIndex, pageSize);
            return Ok(result);
        }

        [HttpPut("{id}/read")]
        [Authorize]
        public async Task<ActionResult<ResponseBase<bool>>> MarkAsRead(Guid id)
        {
            var userIdStr = User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var result = await _notificationUseCase.MarkAsReadAsync(id, userId);
            if (!result) return NotFound(new ResponseBase<bool>(false, "Notification not found or access denied"));

            return Ok(new ResponseBase<bool>(true, "Notification marked as read"));
        }

        [HttpGet("system-wide")]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<ActionResult<PaginatedDataResponse<SystemNotificationDto>>> GetSystemWideNotifications([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _notificationUseCase.GetSystemNotificationsForStaffAsync(pageIndex, pageSize);
            return Ok(result);
        }

        [HttpDelete("system-wide")]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<ActionResult<ResponseBase<bool>>> DeleteSystemWideNotification([FromQuery] string title, [FromQuery] string message)
        {
            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(message))
                return BadRequest("Title and message are required.");

            var result = await _notificationUseCase.DeleteSystemNotificationAsync(title, message);
            return Ok(new ResponseBase<bool>(result, result ? "System notification deleted successfully" : "No matching system notification found"));
        }
    }
}
