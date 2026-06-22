using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.JobSearch;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface IPublicJobUseCase
    {
        Task<PaginatedDataResponse<JobCardDto>> SearchJobsAsync(JobSearchQueryDto query);
    }
}
