using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.JobApplication;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobApplicationsController : ControllerBase
    {
        private readonly IJobApplicationUseCase _jobApplicationUseCase;

        public JobApplicationsController(IJobApplicationUseCase jobApplicationUseCase)
        {
            _jobApplicationUseCase = jobApplicationUseCase;
        }

        [HttpGet("job-posting/{jobId:guid}/applicants")]
        // [Authorize] // Assuming recruiters are authorized, but keeping consistent with existing if authorization is handled via attributes
        public async Task<ActionResult<ResponseBase<PagedResult<ApplicantDto>>>> GetApplicantsByJobId(Guid jobId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _jobApplicationUseCase.GetApplicantsByJobIdAsync(jobId, page, pageSize);
            return Ok(new ResponseBase<PagedResult<ApplicantDto>>(result));
        }
    }
}
