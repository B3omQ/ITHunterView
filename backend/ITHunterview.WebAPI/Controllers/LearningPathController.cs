using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.LearningPath;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ITHunterview.Domain.Enums;

namespace ITHunterview.WebAPI.Controllers
{
    [Route("api/learning-paths")]
    [ApiController]
    [Authorize(Roles = "candidate")]
    public class LearningPathController : ControllerBase
    {
        private readonly ILearningPathUseCase _learningPathUseCase;

        public LearningPathController(ILearningPathUseCase learningPathUseCase)
        {
            _learningPathUseCase = learningPathUseCase;
        }

        [HttpGet("target-roles")]
        public async Task<ActionResult<ResponseBase<List<TargetRoleResponseDto>>>> GetTargetRoles()
        {
            var result = await _learningPathUseCase.GetTargetRolesAsync();
            return new ResponseBase<List<TargetRoleResponseDto>>(result);
        }

        [HttpPost("generate")]
        public async Task<ActionResult<ResponseBase<LearningPathResponseDto>>> Generate([FromBody] GeneratePathRequestDto request)
        {
            var candidateId = GetUserId();
            var result = await _learningPathUseCase.GenerateLearningPathAsync(candidateId, request);
            return new ResponseBase<LearningPathResponseDto>(result);
        }

        [HttpPost("generate-from-cv-jd")]
        public async Task<ActionResult<ResponseBase<LearningPathResponseDto>>> GenerateFromCvJd([FromBody] GenerateFromCvJdRequestDto request)
        {
            var candidateId = GetUserId();
            var result = await _learningPathUseCase.GenerateFromCvJdAsync(candidateId, request);
            return new ResponseBase<LearningPathResponseDto>(result);
        }

        [HttpPost("generate-from-interview")]
        public async Task<ActionResult<ResponseBase<LearningPathResponseDto>>> GenerateFromInterview([FromBody] GenerateFromInterviewRequestDto request)
        {
            var candidateId = GetUserId();
            var result = await _learningPathUseCase.GenerateFromInterviewAsync(candidateId, request);
            return new ResponseBase<LearningPathResponseDto>(result);
        }

        [HttpGet]
        public async Task<ActionResult<ResponseBase<List<LearningPathResponseDto>>>> GetMyLearningPaths()
        {
            var candidateId = GetUserId();
            var result = await _learningPathUseCase.GetMyLearningPathsAsync(candidateId);
            return new ResponseBase<List<LearningPathResponseDto>>(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ResponseBase<LearningPathResponseDto>>> GetById(Guid id)
        {
            var candidateId = GetUserId();
            var result = await _learningPathUseCase.GetLearningPathByIdAsync(candidateId, id);
            return new ResponseBase<LearningPathResponseDto>(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ResponseBase<string>>> Delete(Guid id)
        {
            var candidateId = GetUserId();
            await _learningPathUseCase.DeleteLearningPathAsync(candidateId, id);
            return new ResponseBase<string>("Learning path deleted successfully.");
        }

        [HttpPut("{id:guid}/modules/{moduleIndex:int}/tasks/{taskIndex:int}/toggle")]
        public async Task<ActionResult<ResponseBase<LearningPathResponseDto>>> ToggleTaskCompletion(Guid id, int moduleIndex, int taskIndex)
        {
            var candidateId = GetUserId();
            var result = await _learningPathUseCase.ToggleTaskCompletionAsync(candidateId, id, moduleIndex, taskIndex);
            return new ResponseBase<LearningPathResponseDto>(result);
        }

        [HttpGet("preview-context")]
        public async Task<ActionResult<ResponseBase<HistoryContextPreviewDto>>> PreviewHistoryContext([FromQuery] string type, [FromQuery] Guid? sourceId)
        {
            var candidateId = GetUserId();
            var result = await _learningPathUseCase.PreviewHistoryContextAsync(candidateId, type, sourceId);
            return new ResponseBase<HistoryContextPreviewDto>(result);
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(claim) || !Guid.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException("Token không hợp lệ.");
            return userId;
        }
    }
}
