using ITHunterview.Service.DTOs.Dashboard;

namespace ITHunterview.Service.Interface.UseCase;

public interface IAdminDashboardUseCase
{
    Task<AdminDashboardResponseDto> GetDashboardAsync(DashboardFilterRequest request);
}
