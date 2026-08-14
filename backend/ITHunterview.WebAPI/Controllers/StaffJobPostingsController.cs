using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Job;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Domain.Enums;

namespace ITHunterview.WebAPI.Controllers
{
    [Route("api/staff/job-postings")]
    [ApiController]
    [Authorize(Policy = "StaffOrAdmin")]
    public class StaffJobPostingsController : ControllerBase
    {
        private readonly IJobPostingsUseCase _jobPostingsUseCase;

        public StaffJobPostingsController(IJobPostingsUseCase jobPostingsUseCase)
        {
            _jobPostingsUseCase = jobPostingsUseCase;
        }

        [HttpGet]
        public async Task<ActionResult<ResponseBase<PagedResult<JobPostingSummaryDto>>>> GetJobs(
            [FromQuery] string? search,
            [FromQuery] JobStatus? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            // We pass null for recruiterId to get ALL jobs
            var result = await _jobPostingsUseCase.GetJobsAsync(search, status, page, pageSize, null);
            return Ok(result);
        }

        [HttpPost("{id:guid}/ban")]
        public async Task<ActionResult<ResponseBase<bool>>> BanJob(Guid id, [FromBody] BanJobRequestDto dto)
        {
            var result = await _jobPostingsUseCase.BanJobAsync(id, dto.Reason);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost("{id:guid}/unban")]
        public async Task<ActionResult<ResponseBase<bool>>> UnbanJob(Guid id)
        {
            var result = await _jobPostingsUseCase.UnbanJobAsync(id);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpDelete("seed-data")]
        public async Task<ActionResult<ResponseBase<bool>>> DeleteSeedJobs()
        {
            var result = await _jobPostingsUseCase.DeleteSeedJobsAsync();
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}
