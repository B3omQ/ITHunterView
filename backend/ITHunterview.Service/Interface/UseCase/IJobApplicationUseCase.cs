using System;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.JobApplication;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface IJobApplicationUseCase
    {
        Task<PagedResult<ApplicantDto>> GetApplicantsByJobIdAsync(Guid jobId, int page, int pageSize);
        Task<bool> ApplyForJobAsync(Guid userId, CreateJobApplicationRequestDto request);
        Task<bool> UpdateStatusAsync(Guid applicationId, ITHunterview.Domain.Enums.ApplicationStatus status);
        Task<JobApplicationDetailDto?> GetApplicationDetailAsync(Guid applicationId);
        Task<PagedResult<CandidateAppliedJobDto>> GetCandidateAppliedJobsAsync(Guid userId, int page, int pageSize);
    }
}
