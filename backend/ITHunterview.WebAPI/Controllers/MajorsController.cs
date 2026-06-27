using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.MasterData;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MajorsController : ControllerBase
    {
        private readonly IMajorUseCase _majorUseCase;

        public MajorsController(IMajorUseCase majorUseCase)
        {
            _majorUseCase = majorUseCase;
        }

        [HttpGet]
        public async Task<ActionResult<ResponseBase<PagedResult<MajorDto>>>> GetActiveMajors([FromQuery] int page = 1, [FromQuery] int pageSize = 1000)
        {
            var result = await _majorUseCase.GetPagedMajorsAsync(page, pageSize, null);
            return Ok(result);
        }
    }
}
