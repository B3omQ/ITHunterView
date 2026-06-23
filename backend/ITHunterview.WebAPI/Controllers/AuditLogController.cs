using System;
using System.Threading.Tasks;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("api/v1/audit-logs")]
    [Authorize(Policy = "StaffOrAdmin")]
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogUseCase _auditLogUseCase;

        public AuditLogController(IAuditLogUseCase auditLogUseCase)
        {
            _auditLogUseCase = auditLogUseCase;
        }

        /// <summary>
        /// Xem lịch sử thao tác của các quản trị viên/hệ thống (Audit Trail)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPagedAuditLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] ActivityLogCategory? category = null,
            [FromQuery] ActivityLogStatus? status = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] Guid? userId = null,
            [FromQuery] string? operationType = null)
        {
            var result = await _auditLogUseCase.GetPagedAuditLogsAsync(
                page,
                pageSize,
                search,
                category,
                status,
                startDate,
                endDate,
                userId,
                operationType);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Dọn dẹp các bản ghi nhật ký kiểm toán cũ hơn số ngày lưu trữ chỉ định (Chỉ Admin)
        /// </summary>
        [HttpDelete("purge")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> PurgeAuditLogs([FromQuery] int daysRetention = 90)
        {
            var result = await _auditLogUseCase.PurgeAuditLogsAsync(daysRetention);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
