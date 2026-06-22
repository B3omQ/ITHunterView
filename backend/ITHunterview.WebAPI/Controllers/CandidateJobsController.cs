using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.JobSearch;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("candidate")]
    public class CandidateJobsController : ControllerBase
    {
        private readonly ICandidateJobUseCase _candidateJobUseCase;

        public CandidateJobsController(ICandidateJobUseCase candidateJobUseCase)
        {
            _candidateJobUseCase = candidateJobUseCase;
        }

        [HttpGet("jobs")]
        public async Task<ActionResult<PaginatedDataResponse<JobCardDto>>> GetJobs([FromQuery] JobSearchQueryDto query)
        {
            var userId = GetUserId();
            var result = await _candidateJobUseCase.SearchJobsAsync(query, userId);
            return Ok(result);
        }

        [HttpGet("jobs/{id}")]
        public async Task<ActionResult<ResponseBase<JobDetailViewDto>>> GetJobDetail(Guid id)
        {
            var userId = GetUserId();
            var result = await _candidateJobUseCase.GetJobDetailAsync(id, userId);
            if (!result.Success)
            {
                return NotFound(result);
            }
            return Ok(result);
        }

        [HttpGet("saved-jobs")]
        public async Task<ActionResult<PaginatedDataResponse<SavedJobDto>>> GetSavedJobs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = GetUserId();
            var result = await _candidateJobUseCase.GetSavedJobsAsync(userId, page, pageSize);
            return Ok(result);
        }

        [HttpPost("saved-jobs")]
        public async Task<ActionResult> SaveJob([FromBody] SaveJobRequestDto request)
        {
            var userId = GetUserId();
            await _candidateJobUseCase.SaveJobAsync(userId, request.JobId);
            return StatusCode(201, new { message = "Job saved successfully" });
        }

        [HttpDelete("saved-jobs/{jobId}")]
        public async Task<ActionResult> UnsaveJob(Guid jobId)
        {
            var userId = GetUserId();
            await _candidateJobUseCase.UnsaveJobAsync(userId, jobId);
            return NoContent();
        }

        private Guid GetUserId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                            ?? User.FindFirst("sub")?.Value;
            
            if (Guid.TryParse(userIdStr, out var userId))
            {
                return userId;
            }

            throw new UnauthorizedAccessException("Invalid user token.");
        }
    }
}
