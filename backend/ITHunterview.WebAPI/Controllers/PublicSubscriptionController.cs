using System.Threading.Tasks;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("api/v1/subscriptions")]
    [AllowAnonymous] // Cho phép khách truy cập xem danh sách gói cước
    public class PublicSubscriptionController : ControllerBase
    {
        private readonly IPublicSubscriptionUseCase _subscriptionUseCase;

        public PublicSubscriptionController(IPublicSubscriptionUseCase subscriptionUseCase)
        {
            _subscriptionUseCase = subscriptionUseCase;
        }

        /// <summary>
        /// Lấy danh sách các gói cước đang ACTIVE (theo role nếu có)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetActiveSubscriptions([FromQuery] string? role)
        {
            var result = await _subscriptionUseCase.GetActiveSubscriptionsAsync(role);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}
