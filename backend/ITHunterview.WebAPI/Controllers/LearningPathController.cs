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

        [HttpPost("generate")]
        public async Task<ActionResult<ResponseBase<LearningPathResponseDto>>> Generate([FromBody] GeneratePathRequestDto request)
        {
            var candidateId = GetUserId();
            var result = await _learningPathUseCase.GenerateLearningPathAsync(candidateId, request);
            return new ResponseBase<LearningPathResponseDto>(result);
        }

        [HttpPost("generate-from-history")]
        public async Task<ActionResult<ResponseBase<LearningPathResponseDto>>> GenerateFromHistory([FromBody] GenerateFromHistoryRequestDto request)
        {
            var candidateId = GetUserId();
            var result = await _learningPathUseCase.GenerateFromHistoryAsync(candidateId, request);
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

        private Guid GetUserId()
        {
            var claim = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(claim) || !Guid.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException("Token không hợp lệ.");
            return userId;
        }
    }
}
