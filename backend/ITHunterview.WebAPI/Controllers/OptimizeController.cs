using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers;

[Route("api/optimize-sessions")]
[ApiController]
[Authorize] // Assuming candidate needs to be authorized
public class OptimizeController : ControllerBase
{
    private readonly IOptimizeUseCase _optimizeUseCase;

    public OptimizeController(IOptimizeUseCase optimizeUseCase)
    {
        _optimizeUseCase = optimizeUseCase;
    }

    public class CreateOptimizeSessionRequest
    {
        public string? CvUrl { get; set; }
        public Guid? CvId { get; set; }
    }

    [HttpPost("match-sessions/{matchId}")]
    public async Task<ActionResult<ResponseBase<Guid>>> CreateSession(Guid matchId, [FromBody] CreateOptimizeSessionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CvUrl) && !request.CvId.HasValue) 
            return BadRequest("CvUrl or CvId is required.");
        
        var sessionId = await _optimizeUseCase.CreateSessionAsync(matchId, request.CvUrl, request.CvId);
        return new ResponseBase<Guid>(sessionId);
    }

    [HttpGet("{id}/suggestions")]
    public async Task<ActionResult<ResponseBase<object>>> GetSuggestions(Guid id)
    {
        var suggestions = await _optimizeUseCase.GetSuggestionsAsync(id);
        return new ResponseBase<object>(suggestions);
    }

    public class PatchSuggestionRequest
    {
        public required string Action { get; set; } // "accept", "edit", "skip"
        public string? EditedText { get; set; }
        public string? OriginalText { get; set; }
        public string? SuggestedText { get; set; }
    }

    [HttpPatch("{id}/suggestions/{suggestionId}")]
    public async Task<ActionResult<ResponseBase<object>>> ApplySuggestion(Guid id, string suggestionId, [FromBody] PatchSuggestionRequest request)
    {
        var result = await _optimizeUseCase.ApplySuggestionAsync(id, suggestionId, request.Action, request.EditedText, request.OriginalText, request.SuggestedText);
        return new ResponseBase<object>(result);
    }

    [HttpPost("{id}/generate")]
    public async Task<ActionResult<ResponseBase<string>>> GenerateFile(Guid id)
    {
        var fileUrl = await _optimizeUseCase.GenerateFinalFileAsync(id);
        return new ResponseBase<string>(fileUrl);
    }
}
