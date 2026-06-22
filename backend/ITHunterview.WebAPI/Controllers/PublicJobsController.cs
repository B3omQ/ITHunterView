using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.JobSearch;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("public/jobs")]
    public class PublicJobsController : ControllerBase
    {
        private readonly IPublicJobUseCase _publicJobUseCase;

        public PublicJobsController(IPublicJobUseCase publicJobUseCase)
        {
            _publicJobUseCase = publicJobUseCase;
        }

        [HttpGet]
        public async Task<ActionResult<PaginatedDataResponse<JobCardDto>>> GetJobs([FromQuery] JobSearchQueryDto query)
        {
            var result = await _publicJobUseCase.SearchJobsAsync(query);
            return Ok(result);
        }
    }
}
