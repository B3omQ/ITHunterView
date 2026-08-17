using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Job;
using ITHunterview.Service.DTOs.JobAnalysis;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.UseCase;
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
        private readonly IJobAnalysisUseCase _jobAnalysisUseCase;
        private readonly IRecruiterCvScanUseCase _recruiterCvScanUseCase;

        public JobPostingsController(
            IJobPostingsUseCase jobPostingsUseCase, 
            IUserUseCase userUseCase, 
            ICvJobMatchingUseCase cvJobMatchingUseCase,
            IHardcodeCvJobMatchingUseCase hardcodeCvJobMatchingUseCase,
            IJobAnalysisUseCase jobAnalysisUseCase,
            IRecruiterCvScanUseCase recruiterCvScanUseCase)
        {
            _jobPostingsUseCase = jobPostingsUseCase;
            _userUseCase = userUseCase;
            _cvJobMatchingUseCase = cvJobMatchingUseCase;
            _hardcodeCvJobMatchingUseCase = hardcodeCvJobMatchingUseCase;
            _jobAnalysisUseCase = jobAnalysisUseCase;
            _recruiterCvScanUseCase = recruiterCvScanUseCase;
        }

        [HttpGet]
        [Authorize(Policy = "RecruiterOnly")]
        public async Task<ActionResult<ResponseBase<PagedResult<JobPostingSummaryDto>>>> GetJobs(
            [FromQuery] string? search,
            [FromQuery] JobStatus? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 7)
        {
            var recruiterId = await ResolveRecruiterIdAsync();
            if (recruiterId == Guid.Empty)
            {
                return Unauthorized(new ResponseBase<PagedResult<JobPostingSummaryDto>>("Could not resolve recruiter user."));
            }

            var result = await _jobPostingsUseCase.GetJobsAsync(search, status, page, pageSize, recruiterId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "RecruiterOnly")]
        public async Task<ActionResult<ResponseBase<JobPostingDetailDto>>> GetJobById(Guid id)
        {
            var recruiterId = await ResolveRecruiterIdAsync();
            if (recruiterId == Guid.Empty)
            {
                return Unauthorized(new ResponseBase<JobPostingDetailDto>("Could not resolve recruiter user."));
            }

            var result = await _jobPostingsUseCase.GetJobByIdAsync(id);
            if (!result.Success || result.Data == null)
            {
                return NotFound(result);
            }

            if (result.Data.RecruiterId != recruiterId)
            {
                return Forbid();
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

        [HttpPost("{id:guid}/analysis")]
        [Authorize(Policy = "RecruiterOnly")]
        public async Task<IActionResult> RequestAnalysis(Guid id, [FromBody] AnalyzeJobRequestDto dto, CancellationToken ct)
        {
            var recruiterId = await ResolveRecruiterIdAsync();
            if (recruiterId == Guid.Empty) return Unauthorized();

            var statusDto = await _jobAnalysisUseCase.RequestAnalysisAsync(id, recruiterId, dto, ct);
            if (statusDto.IsReused)
            {
                var message = statusDto.IsQueued
                    ? "Reused the existing queued analysis run."
                    : "Reused the existing ready analysis run.";
                return Ok(new ResponseBase<JobAnalysisStatusDto>(statusDto, message));
            }
            return Accepted(new ResponseBase<JobAnalysisStatusDto>(statusDto, "Job analysis requested successfully."));
        }

        [HttpPost("{id:guid}/analysis/{runId:guid}/retry")]
        [Authorize(Policy = "RecruiterOnly")]
        public async Task<IActionResult> RetryAnalysis(Guid id, Guid runId, [FromBody] AnalyzeJobRequestDto dto, CancellationToken ct)
        {
            var recruiterId = await ResolveRecruiterIdAsync();
            if (recruiterId == Guid.Empty) return Unauthorized();

            var statusDto = await _jobAnalysisUseCase.RetryAnalysisAsync(id, runId, recruiterId, dto, ct);
            return Accepted(new ResponseBase<JobAnalysisStatusDto>(statusDto, "Job analysis retry requested successfully."));
        }

        [HttpGet("{id:guid}/analysis")]
        [Authorize(Policy = "RecruiterOnly")]
        public async Task<IActionResult> GetAnalysisPreview(Guid id, CancellationToken ct)
        {
            var recruiterId = await ResolveRecruiterIdAsync();
            if (recruiterId == Guid.Empty) return Unauthorized();

            var previewDto = await _jobAnalysisUseCase.GetPreviewAsync(id, recruiterId, ct);
            if (previewDto == null) return NotFound(new ResponseBase<JobAnalysisPreviewDto>("Job preview not found or access denied."));

            return Ok(new ResponseBase<JobAnalysisPreviewDto>(previewDto));
        }

        [HttpPut("{id:guid}/analysis/{runId:guid}/decisions")]
        [Authorize(Policy = "RecruiterOnly")]
        public async Task<IActionResult> UpdateDecisions(Guid id, Guid runId, [FromBody] UpdateJobSkillDecisionsDto dto, CancellationToken ct)
        {
            var recruiterId = await ResolveRecruiterIdAsync();
            if (recruiterId == Guid.Empty) return Unauthorized();

            var previewDto = await _jobAnalysisUseCase.UpdateDecisionsAsync(id, runId, recruiterId, dto, ct);
            return Ok(new ResponseBase<JobAnalysisPreviewDto>(previewDto, "Skill decisions updated successfully."));
        }

        [HttpPost("{id:guid}/finalize")]
        [Authorize(Policy = "RecruiterOnly")]
        public async Task<IActionResult> Finalize(Guid id, [FromBody] FinalizeJobRequestDto dto, CancellationToken ct)
        {
            var recruiterId = await ResolveRecruiterIdAsync();
            if (recruiterId == Guid.Empty) return Unauthorized();

            var finalizeDto = await _jobAnalysisUseCase.FinalizeAsync(id, recruiterId, dto, ct);
            return Ok(new ResponseBase<FinalizeJobResponseDto>(finalizeDto, "Job finalized successfully."));
        }
        
        [HttpPost("{id}/extend")]
        [HttpPatch("{id}/extend")]
        public async Task<ActionResult<ResponseBase<JobPostingDetailDto>>> ExtendJob(Guid id)
        {
            var recruiterId = await ResolveRecruiterIdAsync();
            if (recruiterId == Guid.Empty)
            {
                return BadRequest(new ResponseBase<JobPostingDetailDto>("Could not resolve recruiter user."));
            }

            var result = await _jobPostingsUseCase.ExtendJobAsync(id, recruiterId);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost("{id}/push-top")]
        [HttpPatch("{id}/push-top")]
        public async Task<ActionResult<ResponseBase<JobPostingDetailDto>>> PushTopJob(Guid id)
        {
            var recruiterId = await ResolveRecruiterIdAsync();
            if (recruiterId == Guid.Empty)
            {
                return BadRequest(new ResponseBase<JobPostingDetailDto>("Could not resolve recruiter user."));
            }

            var result = await _jobPostingsUseCase.PushTopJobAsync(id, recruiterId);
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
            return StatusCode(
                StatusCodes.Status410Gone,
                new ResponseBase<string>(null, "LEGACY_BULK_MATCHING_DISABLED: Use POST /api/jobpostings/{jobId}/match-cvs-hardcode."));
        }

        [HttpPost("{id:guid}/match-cvs-hardcode")]
        [Authorize(Policy = "RecruiterOnly")]
        public async Task<ActionResult<ResponseBase<ITHunterview.Service.DTOs.Cv.Matching.RecruiterCvScanRunDto>>> MatchCvsHardcode(
            Guid id,
            CancellationToken ct)
        {
            if (!TryResolveAuthenticatedUserId(out var recruiterUserId)) return Unauthorized();

            var result = await _recruiterCvScanUseCase.ScanAsync(recruiterUserId, id, ct);
            return Ok(new ResponseBase<ITHunterview.Service.DTOs.Cv.Matching.RecruiterCvScanRunDto>(
                result,
                "Recruiter CV scan completed."));
        }

        [HttpGet("{id:guid}/matches")]
        [Authorize(Policy = "RecruiterOnly")]
        public async Task<ActionResult<ResponseBase<PagedResult<ITHunterview.Service.DTOs.Cv.Matching.RecruiterCvScanResultDto>>>> GetJobMatches(
            Guid id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            if (!TryResolveAuthenticatedUserId(out var recruiterUserId)) return Unauthorized();

            var result = await _recruiterCvScanUseCase.GetLatestSuccessfulAsync(recruiterUserId, id, page, pageSize, ct);
            return Ok(new ResponseBase<PagedResult<ITHunterview.Service.DTOs.Cv.Matching.RecruiterCvScanResultDto>>(
                result,
                "Recruiter scan results retrieved."));
        }

        [HttpPost("reparse-pending")]
        [Authorize(Policy = "AdminOnly")]
        public ActionResult<ResponseBase<string>> ReparsePendingJobs([FromQuery] int limit = 50)
        {
            return StatusCode(StatusCodes.Status410Gone,
                new ResponseBase<string>(null, "LEGACY_REPARSE_DISABLED: Use POST /api/JobPostings/{id}/analysis for draft jobs."));
        }

        [HttpPost("unlock-candidate")]
        public async Task<ActionResult<ResponseBase<ITHunterview.Service.DTOs.Cv.Matching.UnlockCandidateResponseDto>>> UnlockCandidate([FromBody] ITHunterview.Service.DTOs.Cv.Matching.UnlockCandidateRequestDto dto)
        {
            try
            {
                var userIdStr = User.FindFirstValue("userId");
                if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                {
                    return Unauthorized();
                }

                var result = await _cvJobMatchingUseCase.UnlockCandidateCvAsync(userId, dto);
                if (result.Success)
                {
                    return Ok(new ResponseBase<ITHunterview.Service.DTOs.Cv.Matching.UnlockCandidateResponseDto>(result, result.Message));
                }
                return BadRequest(new ResponseBase<ITHunterview.Service.DTOs.Cv.Matching.UnlockCandidateResponseDto>(result, result.Message));
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseBase<ITHunterview.Service.DTOs.Cv.Matching.UnlockCandidateResponseDto>(null, ex.Message));
            }
        }

        private Task<Guid> ResolveRecruiterIdAsync()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                            ?? User.FindFirst("sub")?.Value;
            
            return _userUseCase.ResolveRecruiterIdAsync(userIdStr);
        }

        private bool TryResolveAuthenticatedUserId(out Guid userId)
        {
            var rawUserId = User.FindFirstValue("userId")
                            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value;
            return Guid.TryParse(rawUserId, out userId);
        }
    }
}
