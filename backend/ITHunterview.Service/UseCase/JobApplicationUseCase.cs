using System;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.JobApplication;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase
{
    public class JobApplicationUseCase : IJobApplicationUseCase
    {
        private readonly IJobApplicationRepository _jobApplicationRepository;
        private readonly ICandidateProfileRepository _candidateProfileRepository;
        private readonly IJobPostingRepository _jobPostingRepository;

        public JobApplicationUseCase(
            IJobApplicationRepository jobApplicationRepository, 
            ICandidateProfileRepository candidateProfileRepository,
            IJobPostingRepository jobPostingRepository)
        {
            _jobApplicationRepository = jobApplicationRepository;
            _candidateProfileRepository = candidateProfileRepository;
            _jobPostingRepository = jobPostingRepository;
        }

        public async Task<PagedResult<ApplicantDto>> GetApplicantsByJobIdAsync(Guid jobId, int page, int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;
            return await _jobApplicationRepository.GetApplicantsByJobIdAsync(jobId, page, pageSize);
        }

        public async Task<bool> ApplyForJobAsync(Guid userId, CreateJobApplicationRequestDto request)
        {
            var profile = await _candidateProfileRepository.GetByUserIdAsync(userId);
            if (profile == null)
            {
                throw new UnauthorizedAccessException("Only candidates can apply for jobs.");
            }

            var hasApplied = await _jobApplicationRepository.HasCandidateAppliedAsync(profile.Id, request.JobId);
            if (hasApplied)
            {
                throw new InvalidOperationException("You have already applied for this job.");
            }

            var application = new JobApplications
            {
                Id = Guid.NewGuid(),
                CandidateId = profile.Id,
                JobId = request.JobId,
                CvId = request.CvId,
                CoverLetter = request.CoverLetter ?? string.Empty,
                Status = ApplicationStatus.APPLIED,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _jobApplicationRepository.CreateAsync(application);

            // Increment ApplicationCount on the Job Posting
            var jobPosting = await _jobPostingRepository.GetByIdAsync(request.JobId);
            if (jobPosting != null)
            {
                jobPosting.ApplicationCount += 1;
                jobPosting.UpdatedAt = DateTime.UtcNow;
                await _jobPostingRepository.UpdateAsync(jobPosting);
            }

            return true;
        }

        public async Task<bool> UpdateStatusAsync(Guid applicationId, ApplicationStatus status)
        {
            var application = await _jobApplicationRepository.GetByIdAsync(applicationId);
            if (application == null)
            {
                throw new KeyNotFoundException("Job application not found.");
            }

            application.Status = status;
            application.UpdatedAt = DateTime.UtcNow;
            await _jobApplicationRepository.UpdateAsync(application);

            return true;
        }

        public async Task<JobApplicationDetailDto?> GetApplicationDetailAsync(Guid applicationId)
        {
            var detail = await _jobApplicationRepository.GetApplicationDetailAsync(applicationId);
            if (detail == null)
            {
                throw new KeyNotFoundException("Job application not found.");
            }
            return detail;
        }
    }
}
