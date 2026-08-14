using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Job;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface IJobPostingsUseCase
    {
        Task<ResponseBase<PagedResult<JobPostingSummaryDto>>> GetJobsAsync(
            string? search, 
            JobStatus? status, 
            int page, 
            int pageSize,
            Guid? recruiterId = null);
        Task<ResponseBase<JobPostingDetailDto>> GetJobByIdAsync(Guid id);
        Task<ResponseBase<JobPostingDetailDto>> CreateJobAsync(CreateJobPostingDto dto, Guid recruiterId);
        Task<ResponseBase<JobPostingDetailDto>> UpdateJobAsync(Guid id, UpdateJobPostingDto dto, Guid recruiterId);
        Task<ResponseBase<bool>> CloseJobAsync(Guid id, Guid recruiterId);
        Task<ResponseBase<string>> ReparsePendingJobsAsync(int limit = 50);
        Task<ResponseBase<JobPostingDetailDto>> ExtendJobAsync(Guid id, Guid recruiterId);
        Task<ResponseBase<JobPostingDetailDto>> PushTopJobAsync(Guid id, Guid recruiterId);
        Task<ResponseBase<bool>> BanJobAsync(Guid id, string reason);
        Task<ResponseBase<bool>> UnbanJobAsync(Guid id);
        Task<ResponseBase<bool>> DeleteSeedJobsAsync();
    }
}
