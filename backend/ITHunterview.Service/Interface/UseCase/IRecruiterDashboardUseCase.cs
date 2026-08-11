using ITHunterview.Service.DTOs.Dashboard;

namespace ITHunterview.Service.Interface.UseCase;

public interface IRecruiterDashboardUseCase
{
    Task<RecruiterDashboardResponseDto> GetDashboardAsync(DashboardFilterRequest request, Guid recruiterId);
}
