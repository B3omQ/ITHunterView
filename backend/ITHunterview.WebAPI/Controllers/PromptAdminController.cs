using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.PromptAdmin;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers
{
    [Route("api/admin/prompts")]
    [ApiController]
    [Authorize(Roles = "admin,staff")]
    public class PromptAdminController : ControllerBase
    {
        private readonly IPromptAdminUseCase _promptAdminUseCase;

        public PromptAdminController(IPromptAdminUseCase promptAdminUseCase)
        {
            _promptAdminUseCase = promptAdminUseCase;
        }

        [HttpGet]
        public async Task<ActionResult<ResponseBase<PagedResult<PromptDto>>>> GetPagedPrompts([FromQuery] int page = 1, [FromQuery] int size = 10)
        {
            var result = await _promptAdminUseCase.GetPagedPromptsAsync(page, size);
            return Ok(new ResponseBase<PagedResult<PromptDto>>(result));
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ResponseBase<PromptDto>>> GetPromptHistory(Guid id)
        {
            var result = await _promptAdminUseCase.GetPromptHistoryAsync(id);
            return Ok(new ResponseBase<PromptDto>(result));
        }

        [HttpGet("cv-analysis")]
        public async Task<ActionResult<ResponseBase<CvAnalysisPromptPairDto>>> GetCvAnalysisPromptPair()
        {
            var result = await _promptAdminUseCase.GetCvAnalysisPromptPairAsync();
            return Ok(new ResponseBase<CvAnalysisPromptPairDto>(result));
        }

        [HttpGet("versions/{versionId:guid}")]
        public async Task<ActionResult<ResponseBase<PromptVersionDto>>> GetPromptVersion(Guid versionId)
        {
            var result = await _promptAdminUseCase.GetPromptVersionAsync(versionId);
            return Ok(new ResponseBase<PromptVersionDto>(result));
        }

        [HttpPost("{id:guid}/versions")]
        public async Task<ActionResult<ResponseBase<PromptVersionDto>>> CreatePromptVersion(Guid id, [FromBody] CreatePromptVersionDto dto)
        {
            var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _promptAdminUseCase.CreatePromptVersionAsync(id, dto, adminId);
            return Ok(new ResponseBase<PromptVersionDto>(result));
        }

        [HttpPatch("{id:guid}/versions/{versionId:guid}/activate")]
        public async Task<ActionResult<ResponseBase<object>>> ActivatePromptVersion(Guid id, Guid versionId)
        {
            var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _promptAdminUseCase.ActivatePromptVersionAsync(id, versionId, adminId);
            return Ok(new ResponseBase<object>(null, "Prompt version activated successfully"));
        }

        [HttpPost("cv-analysis/activate")]
        public async Task<ActionResult<ResponseBase<object>>> ActivateCvAnalysisPromptPair([FromBody] ActivateCvAnalysisPromptPairDto dto)
        {
            var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _promptAdminUseCase.ActivateCvAnalysisPromptPairAsync(dto.SystemVersionId, dto.UserVersionId, adminId);
            return Ok(new ResponseBase<object>(null, "CV analysis prompt pair activated successfully"));
        }
    }
}
