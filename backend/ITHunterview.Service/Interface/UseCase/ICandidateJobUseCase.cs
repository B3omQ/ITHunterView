using System;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.JobSearch;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface ICandidateJobUseCase
    {
        Task<PaginatedDataResponse<JobCardDto>> SearchJobsAsync(JobSearchQueryDto query, Guid userId);
        Task<PaginatedDataResponse<SavedJobDto>> GetSavedJobsAsync(Guid userId, int page, int pageSize);
        Task SaveJobAsync(Guid userId, Guid jobId);
        Task UnsaveJobAsync(Guid userId, Guid jobId);
        Task<ResponseBase<JobDetailViewDto>> GetJobDetailAsync(Guid jobId, Guid userId);
    }
}
