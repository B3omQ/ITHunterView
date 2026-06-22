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
            return await _jobSearchRepository.SearchJobsAsync(query);
        }
    }
}
