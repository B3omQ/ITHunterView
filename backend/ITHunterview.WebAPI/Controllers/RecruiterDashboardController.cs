using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Dashboard;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ITHunterview.WebAPI.Controllers;

[Route("api/recruiter/dashboard")]
[ApiController]
[Authorize(Policy = "RecruiterOnly")]
public class RecruiterDashboardController : ControllerBase
{
    private readonly IRecruiterDashboardUseCase _recruiterDashboardUseCase;

    public RecruiterDashboardController(IRecruiterDashboardUseCase recruiterDashboardUseCase)
    {
        _recruiterDashboardUseCase = recruiterDashboardUseCase;
    }

    [HttpGet]
    public async Task<ActionResult<ResponseBase<RecruiterDashboardResponseDto>>> GetDashboard([FromQuery] DashboardFilterRequest request)
    {
        var userIdStr = User.FindFirstValue("userId") 
                        ?? User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var result = await _recruiterDashboardUseCase.GetDashboardAsync(request, userId);
        return new ResponseBase<RecruiterDashboardResponseDto>(result);
    }
}
