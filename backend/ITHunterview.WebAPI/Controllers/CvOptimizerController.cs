using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.CvOptimizer;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers
{
    [Route("api/cv-optimizer")]
    [ApiController]
    [Authorize(Roles = "candidate")]
    public class CvOptimizerController : ControllerBase
    {
        private readonly ICvOptimizerUseCase _cvOptimizerUseCase;

        public CvOptimizerController(ICvOptimizerUseCase cvOptimizerUseCase)
        {
            _cvOptimizerUseCase = cvOptimizerUseCase;
        }

        [HttpPost("optimize")]
        public async Task<ActionResult<ResponseBase<CvOptimizationResponseDto>>> Optimize([FromBody] OptimizeCvRequestDto request)
        {
            var candidateId = GetUserId();
            var result = await _cvOptimizerUseCase.OptimizeCvAsync(candidateId, request);
            return new ResponseBase<CvOptimizationResponseDto>(result);
        }

        [HttpGet]
        public async Task<ActionResult<ResponseBase<List<CvOptimizationResponseDto>>>> GetMyOptimizations()
        {
            var candidateId = GetUserId();
            var result = await _cvOptimizerUseCase.GetMyOptimizationHistoryAsync(candidateId);
            return new ResponseBase<List<CvOptimizationResponseDto>>(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ResponseBase<CvOptimizationResponseDto>>> GetById(Guid id)
        {
            var candidateId = GetUserId();
            var result = await _cvOptimizerUseCase.GetOptimizationByIdAsync(candidateId, id);
            return new ResponseBase<CvOptimizationResponseDto>(result);
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
