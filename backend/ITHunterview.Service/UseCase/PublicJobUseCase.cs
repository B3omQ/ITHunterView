using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.JobSearch;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase
{
    public class PublicJobUseCase : IPublicJobUseCase
    {
        private readonly IJobSearchRepository _jobSearchRepository;

        public PublicJobUseCase(IJobSearchRepository jobSearchRepository)
        {
            _jobSearchRepository = jobSearchRepository;
        }

        public async Task<PaginatedDataResponse<JobCardDto>> SearchJobsAsync(JobSearchQueryDto query)
        {
            query.Status = Domain.Enums.JobStatus.PUBLISHED; // Force published status for public searches
            return await _jobSearchRepository.SearchJobsAsync(query);
        }

        public async Task<ResponseBase<JobDetailViewDto>> GetJobDetailAsync(System.Guid jobId)
        {
            var job = await _jobSearchRepository.GetJobDetailAsync(jobId);
            if (job == null)
            {
                return new ResponseBase<JobDetailViewDto>("Job not found or not published.");
            }
            return new ResponseBase<JobDetailViewDto>(job);
        }
    }
}
