using System;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.JobSearch;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface IJobSearchRepository
    {
        Task<PaginatedDataResponse<JobCardDto>> SearchJobsAsync(JobSearchQueryDto query, Guid? userId = null);
    }
}
