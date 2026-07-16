using System.Threading.Tasks;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("api/v1/coin-packages")]
    [Authorize(Policy = "CandidateOnly")] // Chỉ Candidate mới mua gói coin
    public class PublicCoinController : ControllerBase
    {
        private readonly ICoinConfigUseCase _coinConfigUseCase;

        public PublicCoinController(ICoinConfigUseCase coinConfigUseCase)
        {
            _coinConfigUseCase = coinConfigUseCase;
        }

        /// <summary>
        /// Lấy cấu hình gói Coin public (cho Candidate), chỉ các gói đang Active
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPublicCoinConfig()
        {
            var result = await _coinConfigUseCase.GetPublicCoinConfigAsync();
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}
