using System;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.JobApplication;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface IJobApplicationRepository
    {
        Task<PagedResult<ApplicantDto>> GetApplicantsByJobIdAsync(Guid jobId, int page, int pageSize);
        Task<JobApplications> CreateAsync(JobApplications entity);
        Task<bool> HasCandidateAppliedAsync(Guid candidateId, Guid jobId);
        Task<JobApplications?> GetByIdAsync(Guid id);
        Task UpdateAsync(JobApplications entity);
        Task<JobApplicationDetailDto?> GetApplicationDetailAsync(Guid id);
    }
}
