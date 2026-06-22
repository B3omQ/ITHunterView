using System;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.JobSearch;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface IUserSavedJobRepository
    {
        Task<PaginatedDataResponse<SavedJobDto>> GetSavedJobsAsync(Guid userId, int page, int pageSize);
        Task<bool> ExistsAsync(Guid userId, Guid jobId);
        Task AddAsync(UserSavedJobs userSavedJob);
        Task DeleteAsync(Guid userId, Guid jobId);
    }
}
