using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Job;
using ITHunterview.Service.Utils;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Infrastructure.Persistence;

namespace ITHunterview.Service.UseCase
{
    public class JobPostingsUseCase : IJobPostingsUseCase
    {
        private readonly IJobPostingRepository _jobPostingRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly IJobAnalysisInputBuilder _inputBuilder;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly INotificationUseCase _notificationUseCase;
        private readonly ICandidateFeatureUsageUseCase _featureUsageUseCase;
        private readonly ITHunterviewContext _context;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<ITHunterview.Service.Hubs.NotificationHub> _hubContext;
        private readonly ILogger<JobPostingsUseCase> _logger;

        public JobPostingsUseCase(
            IJobPostingRepository jobPostingRepository,
            ICompanyRepository companyRepository,
            IJobAnalysisInputBuilder inputBuilder,
            IServiceScopeFactory scopeFactory,
            INotificationUseCase notificationUseCase,
            ICandidateFeatureUsageUseCase featureUsageUseCase,
            ITHunterviewContext context,
            Microsoft.AspNetCore.SignalR.IHubContext<ITHunterview.Service.Hubs.NotificationHub> hubContext,
            ILogger<JobPostingsUseCase> logger)
        {
            _jobPostingRepository = jobPostingRepository;
            _companyRepository = companyRepository;
            _inputBuilder = inputBuilder;
            _scopeFactory = scopeFactory;
            _notificationUseCase = notificationUseCase;
            _featureUsageUseCase = featureUsageUseCase;
            _context = context;
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
                ApplicationDeadline = j.ApplicationDeadline,
                ExpiresAt = j.ExpiresAt,
                CreatedAt = j.CreatedAt,
                Level = j.Level,
                WorkingModel = j.WorkingModel,
                JobExpertise = j.JobExpertise,
                JobDomain = j.JobDomain,
                Skills = jobSkills.TryGetValue(j.Id, out var skills) ? skills : new List<string>(),

                IsBanned = j.IsBanned,
                BanReason = j.BanReason,

                ParseStatus = j.ParseStatus ?? "PENDING",
                ParseError = j.ParseError,
                AnalysisRevision = j.AnalysisRevision,
                PushedTopUntil = j.PushedTopUntil
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

            if (dto.ApplicationDeadline.HasValue && dto.ApplicationDeadline.Value < DateTime.UtcNow)
            {
                return new ResponseBase<JobPostingDetailDto>("Thời hạn ứng tuyển phải ở tương lai.");
            }
            var text = NormalizeRichTextFields(dto.Description, dto.Requirements, dto.Benefits, dto.IncomeText);

            var jobCode = string.IsNullOrWhiteSpace(dto.JobCode) 
                ? await GenerateUniqueJobCodeAsync(dto.Title) 
                : dto.JobCode;

            var job = new JobPostings
            {
                JobCode = jobCode,
                RecruiterId = recruiterId,
                CompanyId = companyId.Value,

                Title = dto.Title,
                Description = text.Description,
                Requirements = text.Requirements,
                Benefits = text.Benefits,
                IncomeText = text.IncomeText,
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
                ExpiresAt = null,
                ApplicationDeadline = dto.ApplicationDeadline,
                AnalysisRevision = 1,
                ParseStatus = "NOT_REQUESTED",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            job.SemanticContentHash = _inputBuilder.ComputeSemanticHash(_inputBuilder.Build(job));

            await _jobPostingRepository.AddAsync(job);

            var detail = MapToDetailDto(job);
            detail.Skills = new List<JobSkillRequirementDto>();

            return new ResponseBase<JobPostingDetailDto>(detail, "Job posting created successfully as DRAFT.");

            // Broadcast real-time update
            if (detail.Status == JobStatus.PUBLISHED)
            {
                await _hubContext.Clients.All.SendAsync("JobCreated", detail);
            }

            return new ResponseBase<JobPostingDetailDto>(detail, "Job posting created successfully.");
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

            if (job.Status == JobStatus.PENDING_REVIEW)
            {
                return new ResponseBase<JobPostingDetailDto>("Jobs pending review cannot be edited directly.");
            }

            if (job.Status != JobStatus.DRAFT && job.Status != JobStatus.PUBLISHED)
            {
                return new ResponseBase<JobPostingDetailDto>($"Jobs with status {job.Status} cannot be edited.");
            }

            if (job.IsBanned)
            {
                return new ResponseBase<JobPostingDetailDto>("Job posting is banned and cannot be updated.");
            }

            if (dto.ApplicationDeadline.HasValue && dto.ApplicationDeadline.Value < DateTime.UtcNow)
            {
                return new ResponseBase<JobPostingDetailDto>("Thời hạn ứng tuyển phải ở tương lai.");
            }
            var oldSnapshot = _inputBuilder.Build(job);
            var text = NormalizeRichTextFields(dto.Description, dto.Requirements, dto.Benefits, dto.IncomeText);

            job.JobCode = dto.JobCode;
            job.Title = dto.Title;
            job.Description = text.Description;
            job.Requirements = text.Requirements;
            job.Benefits = text.Benefits;
            job.IncomeText = text.IncomeText;
            job.WorkLocationText = dto.WorkLocationText;
            job.MinSalary = dto.MinSalary;
            job.MaxSalary = dto.MaxSalary;
            job.Currency = dto.Currency;
            job.Location = dto.Location;
            job.ApplicationDeadline = dto.ApplicationDeadline;

            job.Level = dto.Level;
            job.WorkingModel = dto.WorkingModel;
            job.JobExpertise = dto.JobExpertise;
            job.JobDomain = dto.JobDomain;
            job.UpdatedAt = DateTime.UtcNow;

            var newSnapshot = _inputBuilder.Build(job);
            var newSemanticHash = _inputBuilder.ComputeSemanticHash(newSnapshot);
            var analysisSourceChanged =
                !string.Equals(oldSnapshot.Description, newSnapshot.Description, StringComparison.Ordinal) ||
                !string.Equals(oldSnapshot.Requirements, newSnapshot.Requirements, StringComparison.Ordinal);
            job.SemanticContentHash = newSemanticHash;

            if (analysisSourceChanged)
            {
                if (job.Status == JobStatus.PUBLISHED && !job.EffectiveAnalysisRevision.HasValue)
                {
                    job.EffectiveAnalysisRevision = job.AnalysisRevision;
                }

                job.AnalysisRevision += 1;
                job.ActiveAnalysisRunId = null;
                job.AnalysisInputHash = null;
                job.ParseStatus = "STALE";
                job.ParseError = null;
            }

            await _jobPostingRepository.UpdateAsync(job);

            var detail = MapToDetailDto(job);
            detail.Skills = await _jobPostingRepository.GetSkillsByJobIdAsync(job.Id);

            return new ResponseBase<JobPostingDetailDto>(detail, "Job posting updated successfully.");
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

        public async Task<ResponseBase<JobPostingDetailDto>> ExtendJobAsync(Guid id, Guid recruiterId)
        {
            var job = await _jobPostingRepository.GetByIdAsync(id);
            if (job == null)
            {
                return new ResponseBase<JobPostingDetailDto>("Không tìm thấy tin tuyển dụng.");
            }

            if (job.RecruiterId != recruiterId)
            {
                return new ResponseBase<JobPostingDetailDto>("Bạn không có quyền gia hạn tin tuyển dụng này.");
            }

            if (job.IsBanned)
            {
                return new ResponseBase<JobPostingDetailDto>("Không thể gia hạn tin tuyển dụng đã bị khóa.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Consume and update run in one transaction. If saving the job fails,
                // the wallet deduction and usage log are rolled back together.
                await _featureUsageUseCase.TryConsumeFeatureAsync(recruiterId, "ExtendJob", job.Id.ToString());

                DateTime baseTime = (!job.ExpiresAt.HasValue || job.ExpiresAt.Value < DateTime.UtcNow)
                    ? DateTime.UtcNow
                    : job.ExpiresAt.Value;

                job.ExpiresAt = baseTime.AddDays(15);
                job.Status = JobStatus.PUBLISHED;
                job.UpdatedAt = DateTime.UtcNow;

                await _jobPostingRepository.UpdateAsync(job);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            var detail = MapToDetailDto(job);
            detail.Skills = await _jobPostingRepository.GetSkillsByJobIdAsync(job.Id);

            return new ResponseBase<JobPostingDetailDto>(detail, $"Đã gia hạn tin tuyển dụng đến {job.ExpiresAt.Value:dd/MM/yyyy} thành công.");
        }

        public async Task<ResponseBase<JobPostingDetailDto>> PushTopJobAsync(Guid id, Guid recruiterId)
        {
            var job = await _jobPostingRepository.GetByIdAsync(id);
            if (job == null)
            {
                return new ResponseBase<JobPostingDetailDto>("Không tìm thấy tin tuyển dụng.");
            }

            if (job.RecruiterId != recruiterId)
            {
                return new ResponseBase<JobPostingDetailDto>("Bạn không có quyền đẩy Top tin tuyển dụng này.");
            }

            if (job.IsBanned)
            {
                return new ResponseBase<JobPostingDetailDto>("Không thể đẩy Top tin tuyển dụng đã bị khóa.");
            }

            if (job.Status != JobStatus.PUBLISHED)
            {
                return new ResponseBase<JobPostingDetailDto>("Tin tuyển dụng phải ở trạng thái Đang hiển thị (PUBLISHED) để đẩy Lên Top.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Consume and update run in one transaction. If saving the job fails,
                // the wallet deduction and usage log are rolled back together.
                await _featureUsageUseCase.TryConsumeFeatureAsync(recruiterId, "PushTop", job.Id.ToString());

                DateTime baseTime = (!job.PushedTopUntil.HasValue || job.PushedTopUntil.Value < DateTime.UtcNow)
                    ? DateTime.UtcNow
                    : job.PushedTopUntil.Value;

                job.PushedTopUntil = baseTime.AddHours(24);
                job.UpdatedAt = DateTime.UtcNow;

                await _jobPostingRepository.UpdateAsync(job);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            var detail = MapToDetailDto(job);
            detail.Skills = await _jobPostingRepository.GetSkillsByJobIdAsync(job.Id);

            return new ResponseBase<JobPostingDetailDto>(detail, $"Đã đẩy tin tuyển dụng lên Top Trang chủ trong 24 giờ (đến {job.PushedTopUntil.Value:dd/MM/yyyy HH:mm}) thành công!");
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
                ApplicationDeadline = j.ApplicationDeadline,
                ExpiresAt = j.ExpiresAt,
                CreatedAt = j.CreatedAt,
                IsBanned = j.IsBanned,
                BanReason = j.BanReason,
                ParseStatus = j.ParseStatus ?? "PENDING",
                ParseError = j.ParseError,
                AnalysisRevision = j.AnalysisRevision,
                RequiresAnalysis = ComputeRequiresAnalysis(j),
                PushedTopUntil = j.PushedTopUntil
            };
        }

        private static bool ComputeRequiresAnalysis(JobPostings job)
        {
            var parseStatus = job.ParseStatus?.ToUpperInvariant();

            if (job.Status == JobStatus.DRAFT)
            {
                return parseStatus is "NOT_REQUESTED" or "STALE" or "PENDING" or "PROCESSING" or "FAILED";
            }

            return job.Status == JobStatus.PUBLISHED
                && job.EffectiveAnalysisRevision != job.AnalysisRevision
                && parseStatus is "STALE" or "PENDING" or "PROCESSING" or "READY" or "FAILED" or "RAW_FALLBACK";
        }

        private static NormalizedJobText NormalizeRichTextFields(
            string description,
            string requirements,
            string benefits,
            string incomeText)
        {
            return new NormalizedJobText(
                NormalizeRequiredRichText(description, "Description", 10000),
                NormalizeRequiredRichText(requirements, "Requirements", 10000),
                NormalizeRequiredRichText(benefits, "Benefits", 10000),
                NormalizeRequiredRichText(incomeText, "IncomeText", 4000));
        }

        private static string NormalizeRequiredRichText(string value, string fieldName, int maximumLength)
        {
            var normalized = JobPostingRichText.NormalizeForStorage(value);
            if (!JobPostingRichText.HasVisibleText(normalized.StoredMarkdown))
            {
                throw new ArgumentException($"{fieldName} must contain visible text.", fieldName);
            }

            if (normalized.StoredMarkdown.Length > maximumLength)
            {
                throw new ArgumentException($"{fieldName} exceeds the maximum length of {maximumLength} characters.", fieldName);
            }

            return normalized.StoredMarkdown;
        }

        private sealed record NormalizedJobText(
            string Description,
            string Requirements,
            string Benefits,
            string IncomeText);

        [Obsolete("Legacy V1 reparse is disabled. Use the V2 analysis endpoint for a draft job.")]
        public Task<ResponseBase<string>> ReparsePendingJobsAsync(int limit = 50)
        {
            return Task.FromResult(new ResponseBase<string>(string.Empty, "LEGACY_REPARSE_DISABLED: Request V2 analysis from the job preview instead."));
        }

        private async Task<string> GenerateUniqueJobCodeAsync(string? title)
        {
            string baseCode = JobCodeGenerator.GenerateSmartJobCode(title);

            bool exists = await _context.JobPostings.AnyAsync(j => j.JobCode == baseCode);
            if (!exists)
            {
                return baseCode;
            }

            int counter = 2;
            while (await _context.JobPostings.AnyAsync(j => j.JobCode == $"{baseCode}-{counter}"))
            {
                counter++;
            }

            return $"{baseCode}-{counter}";
        }

        public async Task<ResponseBase<bool>> DeleteSeedJobsAsync()
        {
            try
            {
                var sql = @"
DO $$ 
DECLARE
    target_job_ids UUID[];
BEGIN
    -- 1. Collect target job IDs
    SELECT array_agg(id) INTO target_job_ids
    FROM ""job_postings""
    WHERE ""company_id"" IN (
        SELECT ""id"" FROM ""companies"" WHERE ""name"" IN ('ITHunterView Corp', 'FPT Software', 'VNG Corporation')
    ) OR ""title"" LIKE '%(RealisticSeed)%' OR ""title"" LIKE '%(RealisticSeedV2)%';

    IF target_job_ids IS NOT NULL THEN
        -- 2. Break circular FK: job_postings -> job_analysis_runs
        UPDATE ""job_postings""
        SET ""active_analysis_run_id"" = NULL, ""effective_analysis_run_id"" = NULL
        WHERE ""id"" = ANY(target_job_ids);

        -- 3. Delete dependents of job_analysis_runs
        DELETE FROM ""job_skill_decisions""
        WHERE ""job_analysis_run_id"" IN (
            SELECT ""id"" FROM ""job_analysis_runs"" WHERE ""job_id"" = ANY(target_job_ids)
        );

        -- 4. Delete dependents of job_applications
        DELETE FROM ""application_history""
        WHERE ""application_id"" IN (
            SELECT ""id"" FROM ""job_applications"" WHERE ""job_id"" = ANY(target_job_ids)
        );

        -- 5. Delete dependents of interview_sessions
        DELETE FROM ""interview_answers""
        WHERE ""session_id"" IN (
            SELECT ""id"" FROM ""interview_sessions"" WHERE ""job_id"" = ANY(target_job_ids)
        );
        DELETE FROM ""interview_reports""
        WHERE ""session_id"" IN (
            SELECT ""id"" FROM ""interview_sessions"" WHERE ""job_id"" = ANY(target_job_ids)
        );
        DELETE FROM ""interview_sessions"" WHERE ""job_id"" = ANY(target_job_ids);

        -- 6. Delete all direct children of job_postings
        DELETE FROM ""job_skill_requirements"" WHERE ""job_id"" = ANY(target_job_ids);
        DELETE FROM ""cv_job_match_scores"" WHERE ""job_id"" = ANY(target_job_ids);
        DELETE FROM ""user_saved_jobs"" WHERE ""job_id"" = ANY(target_job_ids);
        DELETE FROM ""job_applications"" WHERE ""job_id"" = ANY(target_job_ids);
        DELETE FROM ""job_promotions"" WHERE ""job_id"" = ANY(target_job_ids);
        DELETE FROM ""job_reviews"" WHERE ""job_id"" = ANY(target_job_ids);
        DELETE FROM ""job_analysis_runs"" WHERE ""job_id"" = ANY(target_job_ids);

        -- 7. Finally delete the job postings themselves
        DELETE FROM ""job_postings"" WHERE ""id"" = ANY(target_job_ids);
    END IF;
END $$;";
                await _context.Database.ExecuteSqlRawAsync(sql);
                return new ResponseBase<bool>(true, "Seed jobs deleted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("DELETE SEED JOBS EXCEPTION: " + ex.ToString());
                return new ResponseBase<bool>("Failed to delete seed jobs: " + ex.Message + " | Inner: " + ex.InnerException?.Message);
            }
        }
    }
}
