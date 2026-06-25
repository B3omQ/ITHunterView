using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Job;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase
{
    public class JobPostingsUseCase : IJobPostingsUseCase
    {
        private readonly IJobPostingRepository _jobPostingRepository;

        public JobPostingsUseCase(IJobPostingRepository jobPostingRepository)
        {
            _jobPostingRepository = jobPostingRepository;
        }

        public async Task<ResponseBase<PagedResult<JobPostingSummaryDto>>> GetJobsAsync(
            string? search, 
            JobStatus? status, 
            int page, 
            int pageSize,
            Guid? recruiterId = null)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 7; // Matching the mock UI showing 7 rows by default

            var (items, totalCount) = await _jobPostingRepository.GetPagedAsync(search, status, page, pageSize, recruiterId);

            var jobIds = items.Select(j => j.Id).ToList();
            var jobSkills = await _jobPostingRepository.GetSkillsForJobsAsync(jobIds);

            var summaryList = items.Select(j => new JobPostingSummaryDto
            {
                Id = j.Id,
                JobCode = j.JobCode,
                Title = j.Title,
                Location = j.Location,

                Status = j.Status,
                ApplicationCount = j.ApplicationCount,
                ViewCount = j.ViewCount,
                PublishedAt = j.PublishedAt,
                CreatedAt = j.CreatedAt,
                Level = j.Level,
                WorkingModel = j.WorkingModel,
                JobExpertise = j.JobExpertise,
                JobDomain = j.JobDomain,
                Skills = jobSkills.TryGetValue(j.Id, out var skills) ? skills : new List<string>()
            }).ToList();

            var pagedResult = new PagedResult<JobPostingSummaryDto>
            {
                Items = summaryList,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return new ResponseBase<PagedResult<JobPostingSummaryDto>>(pagedResult);
        }

        public async Task<ResponseBase<JobPostingDetailDto>> GetJobByIdAsync(Guid id)
        {
            var job = await _jobPostingRepository.GetByIdAsync(id);
            if (job == null)
            {
                return new ResponseBase<JobPostingDetailDto>("Job posting not found.");
            }

            var detail = MapToDetailDto(job);
            detail.Skills = await _jobPostingRepository.GetSkillsByJobIdAsync(id);
            return new ResponseBase<JobPostingDetailDto>(detail);
        }

        public async Task<ResponseBase<JobPostingDetailDto>> CreateJobAsync(CreateJobPostingDto dto, Guid recruiterId)
        {
            var companyId = await _jobPostingRepository.GetRecruiterCompanyIdAsync(recruiterId);
            if (companyId == null)
            {
                return new ResponseBase<JobPostingDetailDto>("Recruiter company not found. Please link recruiter to a company first.");
            }

            var job = new JobPostings
            {
                Id = Guid.NewGuid(),
                JobCode = string.IsNullOrWhiteSpace(dto.JobCode) ? $"JB-{new Random().Next(1000, 9999)}" : dto.JobCode,
                RecruiterId = recruiterId,
                CompanyId = companyId.Value,

                Title = dto.Title,
                Description = dto.Description,
                Responsibilities = dto.Responsibilities,
                Requirements = dto.Requirements,
                Benefits = dto.Benefits,
                MinSalary = dto.MinSalary,
                MaxSalary = dto.MaxSalary,
                Currency = dto.Currency,
                Location = dto.Location,

                Status = dto.Status,
                Level = dto.Level,
                WorkingModel = dto.WorkingModel,
                JobExpertise = dto.JobExpertise,
                JobDomain = dto.JobDomain,
                ApplicationCount = 0,
                ViewCount = 0,
                PublishedAt = dto.Status == JobStatus.PUBLISHED ? DateTime.UtcNow : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _jobPostingRepository.AddAsync(job);

            if (dto.Skills != null && dto.Skills.Any())
            {
                await _jobPostingRepository.UpdateJobSkillsAsync(job.Id, dto.Skills);
            }

            var detail = MapToDetailDto(job);
            detail.Skills = await _jobPostingRepository.GetSkillsByJobIdAsync(job.Id);
            return new ResponseBase<JobPostingDetailDto>(detail, "Job posting created successfully.");
        }

        public async Task<ResponseBase<JobPostingDetailDto>> UpdateJobAsync(Guid id, UpdateJobPostingDto dto)
        {
            var job = await _jobPostingRepository.GetByIdAsync(id);
            if (job == null)
            {
                return new ResponseBase<JobPostingDetailDto>("Job posting not found.");
            }

            job.JobCode = dto.JobCode;

            job.Title = dto.Title;
            job.Description = dto.Description;
            job.Responsibilities = dto.Responsibilities;
            job.Requirements = dto.Requirements;
            job.Benefits = dto.Benefits;
            job.MinSalary = dto.MinSalary;
            job.MaxSalary = dto.MaxSalary;
            job.Currency = dto.Currency;
            job.Location = dto.Location;

            job.Level = dto.Level;
            job.WorkingModel = dto.WorkingModel;
            job.JobExpertise = dto.JobExpertise;
            job.JobDomain = dto.JobDomain;
            job.UpdatedAt = DateTime.UtcNow;

            if (job.Status != dto.Status)
            {
                job.Status = dto.Status;
                if (dto.Status == JobStatus.PUBLISHED && job.PublishedAt == null)
                {
                    job.PublishedAt = DateTime.UtcNow;
                }
            }

            await _jobPostingRepository.UpdateAsync(job);

            if (dto.Skills != null)
            {
                await _jobPostingRepository.UpdateJobSkillsAsync(job.Id, dto.Skills);
            }

            var detail = MapToDetailDto(job);
            detail.Skills = await _jobPostingRepository.GetSkillsByJobIdAsync(job.Id);
            return new ResponseBase<JobPostingDetailDto>(detail, "Job posting updated successfully.");
        }

        public async Task<ResponseBase<bool>> CloseJobAsync(Guid id)
        {
            var job = await _jobPostingRepository.GetByIdAsync(id);
            if (job == null)
            {
                return new ResponseBase<bool>("Job posting not found.");
            }

            job.Status = JobStatus.CLOSED;
            job.UpdatedAt = DateTime.UtcNow;

            await _jobPostingRepository.UpdateAsync(job);

            return new ResponseBase<bool>(true, "Job posting closed successfully.");
        }

        private static JobPostingDetailDto MapToDetailDto(JobPostings j)
        {
            return new JobPostingDetailDto
            {
                Id = j.Id,
                JobCode = j.JobCode,
                RecruiterId = j.RecruiterId,
                CompanyId = j.CompanyId,

                Title = j.Title,
                Description = j.Description,
                Responsibilities = j.Responsibilities,
                Requirements = j.Requirements,
                Benefits = j.Benefits,
                MinSalary = j.MinSalary,
                MaxSalary = j.MaxSalary,
                Currency = j.Currency,
                Location = j.Location,

                Status = j.Status,
                Level = j.Level,
                WorkingModel = j.WorkingModel,
                JobExpertise = j.JobExpertise,
                JobDomain = j.JobDomain,
                ApplicationCount = j.ApplicationCount,
                ViewCount = j.ViewCount,
                PublishedAt = j.PublishedAt,
                CreatedAt = j.CreatedAt
            };
        }
    }
}
