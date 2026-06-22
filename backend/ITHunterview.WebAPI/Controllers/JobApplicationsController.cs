using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.JobApplication;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobApplicationsController : ControllerBase
    {
        private readonly IJobApplicationUseCase _jobApplicationUseCase;

        public JobApplicationsController(IJobApplicationUseCase jobApplicationUseCase)
        {
            _jobApplicationUseCase = jobApplicationUseCase;
        }

        [HttpGet("job-posting/{jobId:guid}/applicants")]
        // [Authorize] // Assuming recruiters are authorized, but keeping consistent with existing if authorization is handled via attributes
        public async Task<ActionResult<ResponseBase<PagedResult<ApplicantDto>>>> GetApplicantsByJobId(Guid jobId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _jobApplicationUseCase.GetApplicantsByJobIdAsync(jobId, page, pageSize);
            return Ok(new ResponseBase<PagedResult<ApplicantDto>>(result));
        }

        [HttpPost("apply")]
        [Authorize]
        public async Task<ActionResult<ResponseBase<bool>>> ApplyForJob([FromBody] CreateJobApplicationRequestDto request)
        {
            var userIdStr = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new ResponseBase<bool>(false, "Unauthorized."));
            }

            var result = await _jobApplicationUseCase.ApplyForJobAsync(userId, request);
            return Ok(new ResponseBase<bool>(result));
        }

        [HttpPut("{id:guid}/status")]
        [Authorize(Policy = "RecruiterOnly")] // Assuming only recruiters can update status
        public async Task<ActionResult<ResponseBase<bool>>> UpdateApplicationStatus(Guid id, [FromBody] UpdateApplicationStatusRequestDto request)
        {
            var result = await _jobApplicationUseCase.UpdateStatusAsync(id, request.Status);
            return Ok(new ResponseBase<bool>(result, "Status updated successfully"));
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = "RecruiterOnly")]
        public async Task<ActionResult<ResponseBase<JobApplicationDetailDto>>> GetApplicationDetail(Guid id)
        {
            var result = await _jobApplicationUseCase.GetApplicationDetailAsync(id);
            return Ok(new ResponseBase<JobApplicationDetailDto>(result));
        }
    }
}
