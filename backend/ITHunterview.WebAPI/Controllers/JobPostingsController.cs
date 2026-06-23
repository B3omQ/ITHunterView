using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Job;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobPostingsController : ControllerBase
    {
        private readonly IJobPostingsUseCase _jobPostingsUseCase;
        private readonly IUserUseCase _userUseCase;

        public JobPostingsController(IJobPostingsUseCase jobPostingsUseCase, IUserUseCase userUseCase)
        {
            _jobPostingsUseCase = jobPostingsUseCase;
            _userUseCase = userUseCase;
        }

        [HttpGet]
        public async Task<ActionResult<ResponseBase<PagedResult<JobPostingSummaryDto>>>> GetJobs(
            [FromQuery] string? search,
            [FromQuery] JobStatus? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 7)
        {
            var result = await _jobPostingsUseCase.GetJobsAsync(search, status, page, pageSize);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ResponseBase<JobPostingDetailDto>>> GetJobById(Guid id)
        {
            var result = await _jobPostingsUseCase.GetJobByIdAsync(id);
            if (!result.Success)
            {
                return NotFound(result);
            }
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<ResponseBase<JobPostingDetailDto>>> CreateJob([FromBody] CreateJobPostingDto dto)
        {
            var recruiterId = await ResolveRecruiterIdAsync();
            if (recruiterId == Guid.Empty)
            {
                return BadRequest(new ResponseBase<JobPostingDetailDto>("Could not resolve recruiter user."));
            }

            var result = await _jobPostingsUseCase.CreateJobAsync(dto, recruiterId);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ResponseBase<JobPostingDetailDto>>> UpdateJob(Guid id, [FromBody] UpdateJobPostingDto dto)
        {
            var result = await _jobPostingsUseCase.UpdateJobAsync(id, dto);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPatch("{id}/close")]
        public async Task<ActionResult<ResponseBase<bool>>> CloseJob(Guid id)
        {
            var result = await _jobPostingsUseCase.CloseJobAsync(id);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        private Task<Guid> ResolveRecruiterIdAsync()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                            ?? User.FindFirst("sub")?.Value;
            
            return _userUseCase.ResolveRecruiterIdAsync(userIdStr);
        }
    }
}
