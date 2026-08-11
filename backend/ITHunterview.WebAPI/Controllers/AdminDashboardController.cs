using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Dashboard;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace ITHunterview.WebAPI.Controllers;

    [Route("api/admin/dashboard")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class AdminDashboardController : ControllerBase
{
    private readonly IAdminDashboardUseCase _adminDashboardUseCase;

    public AdminDashboardController(IAdminDashboardUseCase adminDashboardUseCase)
    {
        _adminDashboardUseCase = adminDashboardUseCase;
    }

    [HttpGet]
    public async Task<ActionResult<ResponseBase<AdminDashboardResponseDto>>> GetDashboard([FromQuery] DashboardFilterRequest request)
    {
        var result = await _adminDashboardUseCase.GetDashboardAsync(request);
        return new ResponseBase<AdminDashboardResponseDto>(result);
    }
}
