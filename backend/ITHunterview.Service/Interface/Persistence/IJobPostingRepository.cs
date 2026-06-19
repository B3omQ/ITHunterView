using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Job;

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
        Task<Guid?> GetRecruiterCompanyIdAsync(Guid recruiterId);
        Task<List<JobSkillRequirementDto>> GetSkillsByJobIdAsync(Guid jobId);
        Task UpdateJobSkillsAsync(Guid jobId, List<JobSkillRequirementInputDto> skills);
    }
}
