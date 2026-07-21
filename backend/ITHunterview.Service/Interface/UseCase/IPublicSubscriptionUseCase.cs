using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Subscription;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface IPublicSubscriptionUseCase
    {
        Task<ResponseBase<List<SubscriptionDto>>> GetActiveSubscriptionsAsync(string? role);
    }
}
