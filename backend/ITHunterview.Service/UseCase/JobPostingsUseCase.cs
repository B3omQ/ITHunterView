using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Job;
using ITHunterview.Service.Helpers;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Constant.Prompts;

namespace ITHunterview.Service.UseCase
{
    public class JobPostingsUseCase : IJobPostingsUseCase
    {
        private readonly IJobPostingRepository _jobPostingRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<JobPostingsUseCase> _logger;

        public JobPostingsUseCase(
            IJobPostingRepository jobPostingRepository,
            ICompanyRepository companyRepository,
            IServiceScopeFactory scopeFactory,
            ILogger<JobPostingsUseCase> logger)
        {
            _jobPostingRepository = jobPostingRepository;
            _companyRepository = companyRepository;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<ResponseBase<PagedResult<JobPostingSummaryDto>>> GetJobsAsync(
            string? search, 
            JobStatus? status, 
            int page, 
            int pageSize,
            Guid? recruiterId = null)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 7;

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
                ExpiresAt = j.ExpiresAt,
                CreatedAt = j.CreatedAt,
                Level = j.Level,
                WorkingModel = j.WorkingModel,
                JobExpertise = j.JobExpertise,
                JobDomain = j.JobDomain,
                Skills = jobSkills.TryGetValue(j.Id, out var skills) ? skills : new List<string>(),
                ParseStatus = j.ParseStatus ?? "PENDING",
                ParseError = j.ParseError
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

            if (dto.ExpiresAt.HasValue && dto.ExpiresAt.Value > DateTime.UtcNow.AddDays(30))
            {
                return new ResponseBase<JobPostingDetailDto>("Thời gian xuất bản tin không được vượt quá 30 ngày.");
            }

            var job = new JobPostings
            {
                JobCode = string.IsNullOrWhiteSpace(dto.JobCode) 
                    ? $"JB-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}" 
                    : dto.JobCode,
                RecruiterId = recruiterId,
                CompanyId = companyId.Value,

                Title = dto.Title,
                Description = dto.Description,
                Requirements = dto.Requirements,
                Benefits = dto.Benefits,
                IncomeText = dto.IncomeText,
                WorkLocationText = dto.WorkLocationText,
                MinSalary = dto.MinSalary,
                MaxSalary = dto.MaxSalary,
                Currency = dto.Currency,
                Location = dto.Location,

                Status = JobStatus.DRAFT,
                Level = dto.Level,
                WorkingModel = dto.WorkingModel,
                JobExpertise = dto.JobExpertise,
                JobDomain = dto.JobDomain,
                ApplicationCount = 0,
                ViewCount = 0,
                PublishedAt = null,
                ExpiresAt = dto.ExpiresAt,
                AnalysisRevision = 1,
                ParseStatus = "NOT_REQUESTED",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _jobPostingRepository.AddAsync(job);

            var detail = MapToDetailDto(job);
            detail.Skills = new List<JobSkillRequirementDto>();

            return new ResponseBase<JobPostingDetailDto>(detail, "Job posting created successfully as DRAFT.");

        }

        public async Task<ResponseBase<JobPostingDetailDto>> UpdateJobAsync(Guid id, UpdateJobPostingDto dto, Guid recruiterId)
        {
            var job = await _jobPostingRepository.GetByIdAsync(id);
            if (job == null)
            {
                return new ResponseBase<JobPostingDetailDto>("Job posting not found.");
            }

            if (job.RecruiterId != recruiterId)
            {
                throw new UnauthorizedAccessException("You do not have permission to update this job posting.");
            }

            if (job.Status == JobStatus.PUBLISHED || job.Status == JobStatus.PENDING_REVIEW)
            {
                return new ResponseBase<JobPostingDetailDto>("Published or pending review jobs cannot be edited directly. Please clone as a new draft.");
            }

            if (dto.ExpiresAt.HasValue && dto.ExpiresAt.Value > job.CreatedAt.AddDays(30))
            {
                return new ResponseBase<JobPostingDetailDto>("Thời gian xuất bản tin không được vượt quá 30 ngày kể từ lúc tạo.");
            }

            bool semanticChanged = job.Description != dto.Description ||
                                  job.Requirements != dto.Requirements ||
                                  job.Level != dto.Level ||
                                  job.WorkingModel != dto.WorkingModel ||
                                  job.JobExpertise != dto.JobExpertise ||
                                  !AreDomainListsEqual(job.JobDomain, dto.JobDomain);

            job.JobCode = dto.JobCode;
            job.Description = dto.Description;
            job.Requirements = dto.Requirements;
            job.Benefits = dto.Benefits;
            job.IncomeText = dto.IncomeText;
            job.WorkLocationText = dto.WorkLocationText;
            job.MinSalary = dto.MinSalary;
            job.MaxSalary = dto.MaxSalary;
            job.Currency = dto.Currency;
            job.Location = dto.Location;
            job.ExpiresAt = dto.ExpiresAt;

            job.Level = dto.Level;
            job.WorkingModel = dto.WorkingModel;
            job.JobExpertise = dto.JobExpertise;
            job.JobDomain = dto.JobDomain;
            job.UpdatedAt = DateTime.UtcNow;

            if (semanticChanged)
            {
                job.AnalysisRevision += 1;
                job.ActiveAnalysisRunId = null;
                job.ParseStatus = "STALE";
            }

            await _jobPostingRepository.UpdateAsync(job);

            var detail = MapToDetailDto(job);
            detail.Skills = await _jobPostingRepository.GetSkillsByJobIdAsync(job.Id);

            return new ResponseBase<JobPostingDetailDto>(detail, "Job draft updated successfully.");
        }

        private static bool AreDomainListsEqual(List<string>? list1, List<string>? list2)
        {
            if (list1 == null && list2 == null) return true;
            if (list1 == null || list2 == null) return false;
            return list1.SequenceEqual(list2);
        }

        public async Task<ResponseBase<bool>> CloseJobAsync(Guid id, Guid recruiterId)
        {
            var job = await _jobPostingRepository.GetByIdAsync(id);
            if (job == null)
            {
                return new ResponseBase<bool>("Job posting not found.");
            }

            if (job.RecruiterId != recruiterId)
            {
                throw new UnauthorizedAccessException("You do not have permission to close this job posting.");
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
                Requirements = j.Requirements,
                Benefits = j.Benefits,
                IncomeText = j.IncomeText,
                WorkLocationText = j.WorkLocationText,
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
                ExpiresAt = j.ExpiresAt,
                CreatedAt = j.CreatedAt,
                ParseStatus = j.ParseStatus ?? "PENDING",
                ParseError = j.ParseError
            };
        }

        private async Task ParseJdBackgroundAsync(Guid jobId)
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IJobPostingRepository>();
            var aiService = scope.ServiceProvider.GetRequiredService<IAiService>();

            var job = await repo.GetByIdAsync(jobId);
            if (job == null) return;

            try
            {
                job.ParseStatus = "PROCESSING";
                job.UpdatedAt = DateTime.UtcNow;
                await repo.UpdateAsync(job);

                var rawText = JdTextHelper.BuildRawText(job);
                
                var prompt = JdExtractionPrompt.BuildUser(rawText);
                var aiResponse = await aiService.GenerateTextAsync(prompt, JdExtractionPrompt.System);

                var cleanJson = aiResponse.Trim();
                if (cleanJson.StartsWith("```json")) cleanJson = cleanJson.Substring(7);
                if (cleanJson.StartsWith("```")) cleanJson = cleanJson.Substring(3);
                if (cleanJson.EndsWith("```")) cleanJson = cleanJson.Substring(0, cleanJson.Length - 3);
                cleanJson = cleanJson.Trim();

                var freshJob = await repo.GetByIdAsync(jobId);
                if (freshJob == null) return;

                freshJob.ParsedData = cleanJson;
                freshJob.ParseStatus = "SUCCESS";
                freshJob.ParseError = null;
                freshJob.UpdatedAt = DateTime.UtcNow;
                await repo.UpdateAsync(freshJob);
                _logger.LogInformation($"Successfully parsed and updated JD {jobId} in background.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to parse JD {jobId} in background");
                var freshJob = await repo.GetByIdAsync(jobId);
                if (freshJob != null)
                {
                    freshJob.ParseStatus = "FAILED";
                    freshJob.ParseError = ex.Message;
                    freshJob.UpdatedAt = DateTime.UtcNow;
                    await repo.UpdateAsync(freshJob);
                }
            }
        }

        public async Task<ResponseBase<string>> ReparsePendingJobsAsync(int limit = 50)
        {
            limit = Math.Clamp(limit, 1, 20);
            var jobIds = await _jobPostingRepository.ClaimPendingParseJobIdsAsync(limit);
            if (!jobIds.Any())
            {
                return new ResponseBase<string>(string.Empty, "No pending jobs found to parse.");
            }

            using var concurrencyLimiter = new SemaphoreSlim(5);
            await Task.WhenAll(jobIds.Select(async jobId =>
            {
                await concurrencyLimiter.WaitAsync();
                try
                {
                    await ParseJdBackgroundAsync(jobId);
                }
                finally
                {
                    concurrencyLimiter.Release();
                }
            }));

            return new ResponseBase<string>($"Reparse completed for {jobIds.Count} jobs.");
        }
    }
}
