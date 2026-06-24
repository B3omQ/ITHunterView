using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Job;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobCategoriesController : ControllerBase
    {
        private readonly IJobCategoriesUseCase _jobCategoriesUseCase;

        public JobCategoriesController(IJobCategoriesUseCase jobCategoriesUseCase)
        {
            _jobCategoriesUseCase = jobCategoriesUseCase;
        }

        [HttpGet]
        public async Task<ActionResult<ResponseBase<List<CategoryDto>>>> GetCategories()
        {
            var result = await _jobCategoriesUseCase.GetCategoriesAsync();
            return Ok(result);
        }
    }
}
