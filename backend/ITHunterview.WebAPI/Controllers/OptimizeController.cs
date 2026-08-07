using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Optimize;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers;

[Route("api/optimize-sessions")]
[ApiController]
[Authorize]
public class OptimizeController : ControllerBase
{
    private readonly IOptimizeUseCase _optimizeUseCase;
    private readonly ICandidateFeatureUsageUseCase _featureUsageUseCase;

    public OptimizeController(IOptimizeUseCase optimizeUseCase, ICandidateFeatureUsageUseCase featureUsageUseCase)
    {
        _optimizeUseCase = optimizeUseCase;
        _featureUsageUseCase = featureUsageUseCase;
    }

    [HttpPost]
    public async Task<ActionResult<ResponseBase<CvOptimizationResultDto>>> CreateSession([FromBody] CreateOptimizeSessionRequest request)
    {
        var userIdStr = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.CvUrl) && !request.CvId.HasValue) 
            return BadRequest(new ResponseBase<CvOptimizationResultDto>(null!, "CvUrl or CvId is required."));
        
        try
        {
            await _featureUsageUseCase.TryConsumeFeatureAsync(userId, "CvOptimize");
            var result = await _optimizeUseCase.CreateSessionAndAnalyzeAsync(userId, request.CvUrl, request.CvId);
            return new ResponseBase<CvOptimizationResultDto>(result);
        }
        catch (InvalidOperationException ex)
        {
            return Ok(new ResponseBase<CvOptimizationResultDto>(null!, ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(new ResponseBase<CvOptimizationResultDto>(null!, ex.Message));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ResponseBase<CvOptimizationResultDto>>> GetSessionResult(Guid id)
    {
        var result = await _optimizeUseCase.GetSessionResultAsync(id);
        return new ResponseBase<CvOptimizationResultDto>(result);
    }

    [HttpGet("{id:guid}/preview")]
    public async Task<ActionResult<ResponseBase<string?>>> GetPreview(Guid id)
    {
        var base64Image = await _optimizeUseCase.GeneratePreviewAsync(id);
        return new ResponseBase<string?>(base64Image);
    }

    [HttpPost("{id:guid}/generate")]
    public async Task<ActionResult<ResponseBase<string>>> GenerateFile(Guid id)
    {
        var fileUrl = await _optimizeUseCase.GenerateFinalFileAsync(id);
        return new ResponseBase<string>(fileUrl);
    }

    [HttpGet("history")]
    public async Task<ActionResult<ResponseBase<PagedResult<OptimizeHistoryItemDto>>>> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 6)
    {
        var userIdStr = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var result = await _optimizeUseCase.GetUserHistoryAsync(userId, page, pageSize);
        return new ResponseBase<PagedResult<OptimizeHistoryItemDto>>(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ResponseBase<bool>>> DeleteSession(Guid id)
    {
        var userIdStr = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        await _optimizeUseCase.DeleteSessionAsync(userId, id);
        return new ResponseBase<bool>(true);
    }
}
