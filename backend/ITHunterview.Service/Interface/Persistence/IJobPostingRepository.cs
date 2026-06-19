using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface IJobPostingRepository
    {
        Task<JobPostings?> GetByIdAsync(Guid id);
        Task<(List<JobPostings> Items, int TotalCount)> GetPagedAsync(
            string? search, 
            JobStatus? status, 
            int page, 
            int pageSize);
        Task AddAsync(JobPostings job);
        Task UpdateAsync(JobPostings job);
        Task SyncJobSkillsAsync(Guid jobId, List<JobSkillRequirements> newSkills);
        Task<Guid?> GetRecruiterCompanyIdAsync(Guid recruiterId);
    }
}
