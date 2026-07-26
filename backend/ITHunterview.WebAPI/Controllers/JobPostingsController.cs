using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Job;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobPostingsController : ControllerBase
    {
        private readonly IJobPostingsUseCase _jobPostingsUseCase;
        private readonly IUserUseCase _userUseCase;
        private readonly ICvJobMatchingUseCase _cvJobMatchingUseCase;
        private readonly IHardcodeCvJobMatchingUseCase _hardcodeCvJobMatchingUseCase;

        public JobPostingsController(
            IJobPostingsUseCase jobPostingsUseCase, 
            IUserUseCase userUseCase, 
            ICvJobMatchingUseCase cvJobMatchingUseCase,
            IHardcodeCvJobMatchingUseCase hardcodeCvJobMatchingUseCase)
        {
            _jobPostingsUseCase = jobPostingsUseCase;
            _userUseCase = userUseCase;
            _cvJobMatchingUseCase = cvJobMatchingUseCase;
            _hardcodeCvJobMatchingUseCase = hardcodeCvJobMatchingUseCase;
        }

        [HttpGet]
        public async Task<ActionResult<ResponseBase<PagedResult<JobPostingSummaryDto>>>> GetJobs(
            [FromQuery] string? search,
            [FromQuery] JobStatus? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 7)
        {
            Guid? recruiterId = null;

            if (User.Identity?.IsAuthenticated == true)
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
                if (role == "recruiter")
                {
                    var resolvedId = await ResolveRecruiterIdAsync();
                    if (resolvedId != Guid.Empty)
                    {
                        recruiterId = resolvedId;
                    }
                }
            }

            var result = await _jobPostingsUseCase.GetJobsAsync(search, status, page, pageSize, recruiterId);
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
        [Authorize(Policy = "RecruiterOnly")]
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
        [Authorize(Policy = "RecruiterOnly")]
        public async Task<ActionResult<ResponseBase<JobPostingDetailDto>>> UpdateJob(Guid id, [FromBody] UpdateJobPostingDto dto)
        {
            var recruiterId = await ResolveRecruiterIdAsync();
            if (recruiterId == Guid.Empty)
            {
                return Unauthorized();
            }

            var result = await _jobPostingsUseCase.UpdateJobAsync(id, dto, recruiterId);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPatch("{id}/close")]
        [Authorize(Policy = "RecruiterOnly")]
        public async Task<ActionResult<ResponseBase<bool>>> CloseJob(Guid id)
        {
            var recruiterId = await ResolveRecruiterIdAsync();
            if (recruiterId == Guid.Empty)
            {
                return Unauthorized();
            }

            var result = await _jobPostingsUseCase.CloseJobAsync(id, recruiterId);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost("{id:guid}/match-cvs")]
        [Authorize(Policy = "RecruiterOnly")]
        public async Task<ActionResult<ResponseBase<string>>> MatchCvs(Guid id)
        {
            try
            {
                var jobResult = await _jobPostingsUseCase.GetJobByIdAsync(id);
                if (!jobResult.Success)
                {
                    return NotFound(new ResponseBase<string>("Job not found"));
                }

                var recruiterId = await ResolveRecruiterIdAsync();
                if (recruiterId == Guid.Empty)
                {
                    return Unauthorized();
                }
                if (jobResult.Data!.RecruiterId != recruiterId)
                {
                    return Forbid();
                }

                var userIdStr = User.FindFirstValue("userId");
                if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                {
                    return Unauthorized();
                }

                await _cvJobMatchingUseCase.MatchJobWithAllCvsAsync(id, userId);
                return Ok(new ResponseBase<string>("Matching completed", "Job matched with CVs successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseBase<string>(null, ex.Message));
            }
        }

        [HttpPost("{id:guid}/match-cvs-hardcode")]
        [Authorize(Policy = "RecruiterOnly")]
        public async Task<ActionResult<ResponseBase<string>>> MatchCvsHardcode(Guid id)
        {
            try
            {
                var jobResult = await _jobPostingsUseCase.GetJobByIdAsync(id);
                if (!jobResult.Success)
                {
                    return NotFound(new ResponseBase<string>("Job not found"));
                }

                var recruiterId = await ResolveRecruiterIdAsync();
                if (recruiterId == Guid.Empty)
                {
                    return Unauthorized();
                }
                if (jobResult.Data!.RecruiterId != recruiterId)
                {
                    return Forbid();
                }

                var userIdStr = User.FindFirstValue("userId");
                if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                {
                    return Unauthorized();
                }

                await _hardcodeCvJobMatchingUseCase.MatchJobWithAllCvsHardcodeAsync(id, userId);
                return Ok(new ResponseBase<string>("Matching completed", "Job matched with CVs using Hardcode successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseBase<string>(null, ex.Message));
            }
        }

        [HttpGet("{id:guid}/matches")]
        [Authorize(Policy = "RecruiterOnly")]
        public async Task<ActionResult<ResponseBase<PagedResult<ITHunterview.Service.DTOs.Cv.Matching.MatchHistoryDto>>>> GetJobMatches(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var jobResult = await _jobPostingsUseCase.GetJobByIdAsync(id);
                if (!jobResult.Success)
                {
                    return NotFound(new ResponseBase<PagedResult<ITHunterview.Service.DTOs.Cv.Matching.MatchHistoryDto>>("Job not found"));
                }

                var recruiterId = await ResolveRecruiterIdAsync();
                if (recruiterId == Guid.Empty)
                {
                    return Unauthorized();
                }
                if (jobResult.Data!.RecruiterId != recruiterId)
                {
                    return Forbid();
                }

                var result = await _cvJobMatchingUseCase.GetJobMatchHistoryAsync(id, page, pageSize);
                return Ok(new ResponseBase<PagedResult<ITHunterview.Service.DTOs.Cv.Matching.MatchHistoryDto>>(result, "Job matches retrieved"));
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseBase<PagedResult<ITHunterview.Service.DTOs.Cv.Matching.MatchHistoryDto>>(null, ex.Message));
            }
        }

        [HttpPost("reparse-pending")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ResponseBase<string>>> ReparsePendingJobs([FromQuery] int limit = 50)
        {
            try
            {
                var result = await _jobPostingsUseCase.ReparsePendingJobsAsync(limit);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseBase<string>(null, ex.Message));
            }
        }

        private Task<Guid> ResolveRecruiterIdAsync()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                            ?? User.FindFirst("sub")?.Value;
            
            return _userUseCase.ResolveRecruiterIdAsync(userIdStr);
        }
    }
}
