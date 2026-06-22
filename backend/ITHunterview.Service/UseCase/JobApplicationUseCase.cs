using System;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.JobApplication;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase
{
    public class JobApplicationUseCase : IJobApplicationUseCase
    {
        private readonly IJobApplicationRepository _jobApplicationRepository;

        public JobApplicationUseCase(IJobApplicationRepository jobApplicationRepository)
        {
            _jobApplicationRepository = jobApplicationRepository;
        }

        public async Task<PagedResult<ApplicantDto>> GetApplicantsByJobIdAsync(Guid jobId, int page, int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;
            return await _jobApplicationRepository.GetApplicantsByJobIdAsync(jobId, page, pageSize);
        }
    }
}
