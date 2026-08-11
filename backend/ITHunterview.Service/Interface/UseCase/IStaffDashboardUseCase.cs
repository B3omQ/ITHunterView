using ITHunterview.Service.DTOs.Dashboard;

namespace ITHunterview.Service.Interface.UseCase;

public interface IStaffDashboardUseCase
{
    Task<StaffDashboardResponseDto> GetDashboardAsync(DashboardFilterRequest request);
}
