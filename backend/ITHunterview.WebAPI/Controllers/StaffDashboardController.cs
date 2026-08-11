using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Dashboard;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace ITHunterview.WebAPI.Controllers;

[Route("api/staff/dashboard")]
[ApiController]
[Authorize(Policy = "StaffOnly")]
public class StaffDashboardController : ControllerBase
{
    private readonly IStaffDashboardUseCase _staffDashboardUseCase;

    public StaffDashboardController(IStaffDashboardUseCase staffDashboardUseCase)
    {
        _staffDashboardUseCase = staffDashboardUseCase;
    }

    [HttpGet]
    public async Task<ActionResult<ResponseBase<StaffDashboardResponseDto>>> GetDashboard([FromQuery] DashboardFilterRequest request)
    {
        var result = await _staffDashboardUseCase.GetDashboardAsync(request);
        return new ResponseBase<StaffDashboardResponseDto>(result);
    }
}
