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
            int pageSize);
        Task<ResponseBase<JobPostingDetailDto>> GetJobByIdAsync(Guid id);
        Task<ResponseBase<JobPostingDetailDto>> CreateJobAsync(CreateJobPostingDto dto, Guid recruiterId);
        Task<ResponseBase<JobPostingDetailDto>> UpdateJobAsync(Guid id, UpdateJobPostingDto dto);
        Task<ResponseBase<bool>> CloseJobAsync(Guid id);
    }
}
