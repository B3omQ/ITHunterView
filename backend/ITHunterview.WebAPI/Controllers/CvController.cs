using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Cv;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("api/cvs")]
    [Authorize]
    public class CvController : ControllerBase
    {
        private readonly ICvUseCase _cvUseCase;
        private readonly ICvJobMatchingUseCase _cvJobMatchingUseCase;
        private readonly IHardcodeCvJobMatchingUseCase _hardcodeCvJobMatchingUseCase;
        private readonly ICvJdMatchingSubmissionUseCase _matchingSubmissionUseCase;
        private readonly ITHunterview.Service.Interface.Service.Matching.ICvTextExtractorService _cvTextExtractorService;

        public CvController(
            ICvUseCase cvUseCase, 
            ICvJobMatchingUseCase cvJobMatchingUseCase,
            IHardcodeCvJobMatchingUseCase hardcodeCvJobMatchingUseCase,
            ICvJdMatchingSubmissionUseCase matchingSubmissionUseCase,
            ITHunterview.Service.Interface.Service.Matching.ICvTextExtractorService cvTextExtractorService)
        {
            _cvUseCase = cvUseCase;
            _cvJobMatchingUseCase = cvJobMatchingUseCase;
            _hardcodeCvJobMatchingUseCase = hardcodeCvJobMatchingUseCase;
            _matchingSubmissionUseCase = matchingSubmissionUseCase;
            _cvTextExtractorService = cvTextExtractorService;
        }

        [HttpPost]
        public async Task<ActionResult<ResponseBase<CvResponseDto>>> CreateCv([FromBody] CreateCvRequestDto request)
        {
            var userIdStr = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized();
            }

            var cv = await _cvUseCase.CreateCvAsync(userId, request);
            return Ok(new ResponseBase<CvResponseDto>(cv, "CV created successfully"));
        }

        [HttpGet]
        public async Task<ActionResult<ResponseBase<IEnumerable<CvResponseDto>>>> GetMyCvs()
        {
            var userIdStr = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized();
            }

            var cvs = await _cvUseCase.GetMyCvsAsync(userId);
            return Ok(new ResponseBase<IEnumerable<CvResponseDto>>(cvs, "CVs retrieved successfully"));
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ResponseBase<CvResponseDto>>> GetCvById(Guid id)
        {
            var userIdStr = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized();
            }

            try
            {
                var cv = await _cvUseCase.GetCvByIdAsync(id, userId);
                return Ok(new ResponseBase<CvResponseDto>(cv, "CV retrieved successfully"));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ResponseBase<CvResponseDto>("CV not found"));
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ResponseBase<string>>> DeleteCv(Guid id)
        {
            var userIdStr = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized();
            }

            try
            {
                await _cvUseCase.DeleteCvAsync(id, userId);
                return Ok(new ResponseBase<string>("CV deleted successfully", "CV deleted successfully"));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ResponseBase<string>("CV not found"));
            }
        }

        [HttpPost("match-jd")]
        [Authorize(Policy = "CandidateOnly")]
        [RequestSizeLimit(524288)]
        public async Task<ActionResult<ResponseBase<Guid>>> MatchJd(
            [FromBody] ITHunterview.Service.DTOs.Cv.Matching.MatchingRequestDto request,
            CancellationToken ct)
        {
            var userIdStr = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized();
            }

            var idempotencyKey = Request.Headers["Idempotency-Key"].ToString();
            try
            {
                var result = await _matchingSubmissionUseCase.SubmitAsync(userId, request, idempotencyKey, ct);

                return Accepted(new ResponseBase<Guid>(result.JobId, result.IsExisting
                    ? "Existing matching job returned"
                    : "Matching job accepted"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ResponseBase<Guid>(ex.Message));
                /*
                if (consumption != null)
                {
                    await _featureUsageUseCase.RefundFeatureUsageAsync(
                        userId,
                        consumption,
                        "Hoàn Coin do không thể tạo yêu cầu CV-JD matching.");
                }
                throw;*/
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ResponseBase<Guid>("Matching source not found"));
            }
            catch (InvalidOperationException ex) when (ex.Message == "IDEMPOTENCY_KEY_REUSED")
            {
                return Conflict(new ResponseBase<Guid>(ex.Message));
            }
        }

        [HttpPost("extract-text")]
        [Authorize(Policy = "CandidateOnly")]
        public async Task<ActionResult<ResponseBase<string>>> ExtractText(Microsoft.AspNetCore.Http.IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest(new ResponseBase<string>("", "No file provided"));
            try
            {
                using var ms = new System.IO.MemoryStream();
                await file.CopyToAsync(ms);
                var rawText = await _cvTextExtractorService.ExtractTextFromBytesAsync(ms.ToArray(), file.ContentType, file.FileName);
                return Ok(new ResponseBase<string>(rawText, "CV text extracted successfully"));
            }
            catch (Exception ex)

            {
                return BadRequest(new ResponseBase<string>("", ex.Message));
            }
        }

        [HttpGet("match-results/{jobId:guid}")]
        public async Task<ActionResult<ResponseBase<ITHunterview.Service.DTOs.Cv.Matching.MatchingResultDto>>> GetMatchResult(Guid jobId)
        {
            var userIdStr = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized();
            }

            var result = await _cvJobMatchingUseCase.GetMatchingResultAsync(jobId, userId);
            if (result == null)
            {
                return NotFound(new ResponseBase<ITHunterview.Service.DTOs.Cv.Matching.MatchingResultDto>("Job not found"));
            }

            return Ok(new ResponseBase<ITHunterview.Service.DTOs.Cv.Matching.MatchingResultDto>(result, "Result retrieved"));
        }

        [HttpPost("{id:guid}/match-jobs")]
        public async Task<ActionResult<ResponseBase<string>>> MatchJobs(Guid id)
        {
            var userIdStr = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized();
            }

            try
            {
                // Optionally verify that CV belongs to user
                await _cvUseCase.GetCvByIdAsync(id, userId);
                await _cvJobMatchingUseCase.MatchCvWithAllJobsAsync(id, userId);
                return Ok(new ResponseBase<string>("Matching completed", "CV matched with jobs successfully"));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ResponseBase<string>("CV not found"));
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseBase<string>(null, ex.Message));
            }
        }

        [HttpGet("match-history")]
        public async Task<ActionResult<ResponseBase<ITHunterview.Service.DTOs.Common.PagedResult<ITHunterview.Service.DTOs.Cv.Matching.MatchHistoryDto>>>> GetMatchHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] Guid? cvId = null)
        {
            var userIdStr = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized();
            }

            var result = await _cvJobMatchingUseCase.GetMatchHistoryAsync(userId, page, pageSize, cvId);
            return Ok(new ResponseBase<ITHunterview.Service.DTOs.Common.PagedResult<ITHunterview.Service.DTOs.Cv.Matching.MatchHistoryDto>>(result, "Match history retrieved"));
        }

        [HttpDelete("match-history/{jobId:guid}")]
        public async Task<ActionResult<ResponseBase<string>>> DeleteMatchHistory(Guid jobId)
        {
            var userIdStr = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized();
            }

            try
            {
                await _cvJobMatchingUseCase.DeleteMatchHistoryAsync(jobId, userId);
                return Ok(new ResponseBase<string>("Match history deleted successfully", "Match history deleted successfully"));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ResponseBase<string>("Match history not found"));
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseBase<string>(null, ex.Message));
            }
        }
        [HttpPost("{id:guid}/match-jobs-hardcode")]
        public async Task<ActionResult<ResponseBase<string>>> MatchJobsHardcode(Guid id)
        {
            var userIdStr = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized();
            }

            try
            {
                // Verify that CV belongs to user
                await _cvUseCase.GetCvByIdAsync(id, userId);
                await _hardcodeCvJobMatchingUseCase.MatchCvWithAllJobsHardcodeAsync(id, userId);
                return Ok(new ResponseBase<string>("Matching completed", "CV matched with jobs using Hardcode successfully"));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ResponseBase<string>("CV not found"));
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseBase<string>(null, ex.Message));
            }
        }

        [HttpPut("{id:guid}/primary")]
        public async Task<ActionResult<ResponseBase<string>>> SetPrimaryCv(Guid id)
        {
            var userIdStr = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized();
            }

            try
            {
                await _cvUseCase.SetPrimaryCvAsync(id, userId);
                return Ok(new ResponseBase<string>("Primary CV updated successfully", "Success"));
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseBase<string>(null, ex.Message));
            }
        }
    }
}
