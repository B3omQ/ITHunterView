using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.JobSearch;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase
{
    public class CandidateJobUseCase : ICandidateJobUseCase
    {
        private readonly IJobSearchRepository _jobSearchRepository;
        private readonly IUserSavedJobRepository _userSavedJobRepository;
        private readonly IJobPostingRepository _jobPostingsRepository;

        public CandidateJobUseCase(
            IJobSearchRepository jobSearchRepository,
            IUserSavedJobRepository userSavedJobRepository,
            IJobPostingRepository jobPostingsRepository)
        {
            _jobSearchRepository = jobSearchRepository;
            _userSavedJobRepository = userSavedJobRepository;
            _jobPostingsRepository = jobPostingsRepository;
        }

        public async Task<PaginatedDataResponse<JobCardDto>> SearchJobsAsync(JobSearchQueryDto query, Guid userId)
        {
            return await _jobSearchRepository.SearchJobsAsync(query, userId);
        }

        public async Task<PaginatedDataResponse<SavedJobDto>> GetSavedJobsAsync(Guid userId, int page, int pageSize)
        {
            return await _userSavedJobRepository.GetSavedJobsAsync(userId, page, pageSize);
        }

        public async Task SaveJobAsync(Guid userId, Guid jobId)
        {
            // Verify if job exists and is valid (optional but good practice to check if it's expired)
            var job = await _jobPostingsRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                throw new ArgumentException("Job does not exist.");
            }

            var alreadySaved = await _userSavedJobRepository.ExistsAsync(userId, jobId);
            if (alreadySaved)
            {
                throw new InvalidOperationException("Job is already saved.");
            }

            var userSavedJob = new UserSavedJobs
            {
                UserId = userId,
                JobId = jobId,
                CreatedAt = DateTime.UtcNow
            };

            await _userSavedJobRepository.AddAsync(userSavedJob);
        }

        public async Task UnsaveJobAsync(Guid userId, Guid jobId)
        {
            var exists = await _userSavedJobRepository.ExistsAsync(userId, jobId);
            if (!exists)
            {
                throw new KeyNotFoundException("Saved job not found.");
            }

            await _userSavedJobRepository.DeleteAsync(userId, jobId);
        }

        public async Task<ResponseBase<JobDetailViewDto>> GetJobDetailAsync(Guid jobId, Guid userId)
        {
            var job = await _jobSearchRepository.GetJobDetailAsync(jobId, userId);
            if (job == null)
            {
                return new ResponseBase<JobDetailViewDto>("Job not found or not published.");
            }
            return new ResponseBase<JobDetailViewDto>(job);
        }
    }
}
