using System.Text.Json;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.JobAnalysis;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers;

[ApiController]
[Route("api/jd-analysis")]
[AllowAnonymous]
public sealed class JdAnalysisController : ControllerBase
{
    private readonly IJdAnalysisTestUseCase _useCase;

    public JdAnalysisController(IJdAnalysisTestUseCase useCase)
    {
        _useCase = useCase;
    }

    [HttpPost("test")]
    public async Task<ActionResult<ResponseBase<JsonElement>>> Analyze(
        [FromBody] JdAnalysisTestRequestDto request,
        CancellationToken ct)
    {
        var result = await _useCase.AnalyzeAsync(request.JdText, ct);
        return Ok(new ResponseBase<JsonElement>(result));
    }
}
