using System;
using System.Collections.Generic;
using System.Security.Claims;
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

        public CvController(ICvUseCase cvUseCase, ICvJobMatchingUseCase cvJobMatchingUseCase)
        {
            _cvUseCase = cvUseCase;
            _cvJobMatchingUseCase = cvJobMatchingUseCase;
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
        public async Task<ActionResult<CvJobMatchScoreResponseDto>> MatchJd([FromBody] MatchJdRequestDto request)
        {
            var userIdStr = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized();
            }

            try
            {
                Guid finalCvId = request.CvId ?? Guid.Empty;

                // Nếu user không chọn CV có sẵn mà dùng URL (upload) hoặc Text (paste), tự tạo CV mới cho họ
                if (finalCvId == Guid.Empty && (!string.IsNullOrEmpty(request.CvUrl) || !string.IsNullOrEmpty(request.CvText)))
                {
                    var createDto = new CreateCvRequestDto
                    {
                        FileUrl = request.CvUrl ?? string.Empty,
                        ParsedData = request.CvText ?? string.Empty,
                        FileName = !string.IsNullOrEmpty(request.CvUrl) ? "Uploaded CV" : "Pasted CV",
                        FileType = "application/pdf"
                    };
                    var createdCv = await _cvUseCase.CreateCvAsync(userId, createDto);
                    finalCvId = createdCv.Id;
                }

                // TODO: Gọi service AI parse & tính điểm thật (CvJobMatchingUseCase)
                await Task.Delay(2000); // Giả lập processing time

                // Trả về mock data cho giai đoạn 1 (Plumbing)
                var mockResponse = new CvJobMatchScoreResponseDto
                {
                    Id = Guid.NewGuid(),
                    CvId = finalCvId,
                    JobId = request.JobId,
                    OverallScore = 85.5m,
                    SkillMatchScore = 90.0m,
                    ExperienceMatchScore = 80.0m,
                    DomainMatchScore = 85.0m,
                    MatchDetails = "{\"MatchedSkills\": [\"React\", \"TypeScript\"], \"MissingSkills\": [\"Node.js\"]}"
                };

                return Ok(mockResponse);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
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
                await _cvJobMatchingUseCase.MatchCvWithAllJobsAsync(id);
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
    }
}
