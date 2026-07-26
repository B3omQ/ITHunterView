using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Job;
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
        private readonly INotificationUseCase _notificationUseCase;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<ITHunterview.Service.Hubs.NotificationHub> _hubContext;
        private readonly ILogger<JobPostingsUseCase> _logger;

        public JobPostingsUseCase(
            IJobPostingRepository jobPostingRepository,
            ICompanyRepository companyRepository,
            IServiceScopeFactory scopeFactory,
            INotificationUseCase notificationUseCase,
            Microsoft.AspNetCore.SignalR.IHubContext<ITHunterview.Service.Hubs.NotificationHub> hubContext,
            ILogger<JobPostingsUseCase> logger)
        {
            _jobPostingRepository = jobPostingRepository;
            _companyRepository = companyRepository;
            _scopeFactory = scopeFactory;
            _notificationUseCase = notificationUseCase;
            _hubContext = hubContext;
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
                DetailedLocation = j.DetailedLocation,

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
                IsBanned = j.IsBanned,
                BanReason = j.BanReason
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

            if (dto.Status == JobStatus.PUBLISHED)
            {
                var company = await _companyRepository.GetByIdAsync(companyId.Value);
                if (company == null || company.Status != CompanyStatus.VERIFIED)
                {
                    return new ResponseBase<JobPostingDetailDto>("Your company must be verified before you can publish a job posting.");
                }
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
                DetailedLocation = dto.DetailedLocation,

                Status = dto.Status,
                Level = dto.Level,
                WorkingModel = dto.WorkingModel,
                JobExpertise = dto.JobExpertise,
                JobDomain = dto.JobDomain,
                ApplicationCount = 0,
                ViewCount = 0,
                PublishedAt = dto.Status == JobStatus.PUBLISHED ? DateTime.UtcNow : null,
                ExpiresAt = dto.ExpiresAt,
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

            _ = ParseJdBackgroundAsync(job.Id);

            // Broadcast real-time update
            if (detail.Status == JobStatus.PUBLISHED)
            {
                await _hubContext.Clients.All.SendAsync("JobCreated", detail);
            }

            return new ResponseBase<JobPostingDetailDto>(detail, "Job posting created successfully.");
        }

        public async Task<ResponseBase<JobPostingDetailDto>> UpdateJobAsync(Guid id, UpdateJobPostingDto dto)
        {
            var job = await _jobPostingRepository.GetByIdAsync(id);
            if (job == null)
            {
                return new ResponseBase<JobPostingDetailDto>("Job posting not found.");
            }

            if (job.IsBanned)
            {
                return new ResponseBase<JobPostingDetailDto>("Job posting is banned and cannot be updated.");
            }

            if (dto.ExpiresAt.HasValue && dto.ExpiresAt.Value > job.CreatedAt.AddDays(30))
            {
                return new ResponseBase<JobPostingDetailDto>("Thời gian xuất bản tin không được vượt quá 30 ngày kể từ lúc tạo.");
            }

            job.JobCode = dto.JobCode;

            // Title is intentionally omitted from update to prevent changing the job title
            job.Description = dto.Description;
            job.Responsibilities = dto.Responsibilities;
            job.Requirements = dto.Requirements;
            job.Benefits = dto.Benefits;
            job.MinSalary = dto.MinSalary;
            job.MaxSalary = dto.MaxSalary;
            job.Currency = dto.Currency;
            job.Location = dto.Location;
            job.DetailedLocation = dto.DetailedLocation;
            job.ExpiresAt = dto.ExpiresAt;

            job.Level = dto.Level;
            job.WorkingModel = dto.WorkingModel;
            job.JobExpertise = dto.JobExpertise;
            job.JobDomain = dto.JobDomain;
            job.UpdatedAt = DateTime.UtcNow;

            if (job.Status != dto.Status)
            {
                if (dto.Status == JobStatus.PUBLISHED)
                {
                    var company = await _companyRepository.GetByIdAsync(job.CompanyId);
                    if (company == null || company.Status != CompanyStatus.VERIFIED)
                    {
                        return new ResponseBase<JobPostingDetailDto>("Your company must be verified before you can publish a job posting.");
                    }
                    if (job.PublishedAt == null)
                    {
                        job.PublishedAt = DateTime.UtcNow;
                    }
                }
                job.Status = dto.Status;
            }

            await _jobPostingRepository.UpdateAsync(job);

            if (dto.Skills != null)
            {
                await _jobPostingRepository.UpdateJobSkillsAsync(job.Id, dto.Skills);
            }

            var detail = MapToDetailDto(job);
            detail.Skills = await _jobPostingRepository.GetSkillsByJobIdAsync(job.Id);

            _ = ParseJdBackgroundAsync(job.Id);

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

        public async Task<ResponseBase<bool>> BanJobAsync(Guid id, string reason)
        {
            var job = await _jobPostingRepository.GetByIdAsync(id);
            if (job == null)
            {
                return new ResponseBase<bool>("Job posting not found.");
            }

            job.IsBanned = true;
            job.BanReason = reason;
            job.UpdatedAt = DateTime.UtcNow;

            await _jobPostingRepository.UpdateAsync(job);

            await _notificationUseCase.CreateNotificationAsync(new ITHunterview.Service.DTOs.Notification.CreateNotificationDto
            {
                UserId = job.RecruiterId,
                Title = "Bài đăng tuyển dụng của bạn đã bị khóa",
                Message = $"Bài đăng '{job.Title}' đã bị khóa bởi quản trị viên. Lý do: {reason}",
                Type = NotificationType.SYSTEM
            });

            await _hubContext.Clients.All.SendAsync("JobStatusChanged", id);

            return new ResponseBase<bool>(true, "Job posting banned successfully.");
        }

        public async Task<ResponseBase<bool>> UnbanJobAsync(Guid id)
        {
            var job = await _jobPostingRepository.GetByIdAsync(id);
            if (job == null)
            {
                return new ResponseBase<bool>("Job posting not found.");
            }

            job.IsBanned = false;
            job.BanReason = null;
            job.UpdatedAt = DateTime.UtcNow;

            await _jobPostingRepository.UpdateAsync(job);

            await _notificationUseCase.CreateNotificationAsync(new ITHunterview.Service.DTOs.Notification.CreateNotificationDto
            {
                UserId = job.RecruiterId,
                Title = "Bài đăng tuyển dụng đã được mở khóa",
                Message = $"Bài đăng '{job.Title}' đã được mở khóa và hoạt động bình thường.",
                Type = NotificationType.SYSTEM
            });

            await _hubContext.Clients.All.SendAsync("JobStatusChanged", id);

            return new ResponseBase<bool>(true, "Job posting unbanned successfully.");
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
                DetailedLocation = j.DetailedLocation,

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
                IsBanned = j.IsBanned,
                BanReason = j.BanReason
            };
        }

        private async Task ParseJdBackgroundAsync(Guid jobId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IJobPostingRepository>();
                var aiService = scope.ServiceProvider.GetRequiredService<IAiService>();

                var job = await repo.GetByIdAsync(jobId);
                if (job == null) return;

                var rawText = $"Title: {job.Title}\nDescription: {job.Description}\nRequirements: {job.Requirements}\nBenefits: {job.Benefits}";
                
                var prompt = JdExtractionPrompt.BuildUser(rawText);
                var aiResponse = await aiService.GenerateTextAsync(prompt, JdExtractionPrompt.System);

                // Clean json
                var cleanJson = aiResponse.Trim();
                if (cleanJson.StartsWith("```json")) cleanJson = cleanJson.Substring(7);
                if (cleanJson.StartsWith("```")) cleanJson = cleanJson.Substring(3);
                if (cleanJson.EndsWith("```")) cleanJson = cleanJson.Substring(0, cleanJson.Length - 3);
                cleanJson = cleanJson.Trim();

                job.ParsedData = cleanJson;
                await repo.UpdateAsync(job);
                _logger.LogInformation($"Successfully parsed and updated JD {jobId} in background.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to parse JD {jobId} in background");
            }
        }
    }
}
