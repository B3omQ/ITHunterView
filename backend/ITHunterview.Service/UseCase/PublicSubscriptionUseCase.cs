using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Subscription;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase
{
    public class PublicSubscriptionUseCase : IPublicSubscriptionUseCase
    {
        private readonly ISubscriptionRepository _subscriptionRepository;

        public PublicSubscriptionUseCase(ISubscriptionRepository subscriptionRepository)
        {
            _subscriptionRepository = subscriptionRepository;
        }

        public async Task<ResponseBase<List<SubscriptionDto>>> GetActiveSubscriptionsAsync(string? role)
        {
            var items = await _subscriptionRepository.GetAllAsync(role, SubscriptionStatus.ACTIVE);
            var dtos = new List<SubscriptionDto>();
            foreach (var item in items)
            {
                var configDto = DeserializeConfig(item.FeaturesConfig);
                dtos.Add(new SubscriptionDto
                {
                    Id = item.Id,
                    Name = item.Name,
                    Price = item.Price,
                    DurationDays = item.DurationDays,
                    FeaturesConfig = configDto,
                    Status = item.Status,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt
                });
            }
            return new ResponseBase<List<SubscriptionDto>>(dtos);
        }

        private FeaturesConfigDto DeserializeConfig(string? json)
        {
            if (string.IsNullOrEmpty(json)) return new FeaturesConfigDto { Role = "CANDIDATE" };
            try
            {
                var config = JsonSerializer.Deserialize<FeaturesConfigDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return config ?? new FeaturesConfigDto { Role = "CANDIDATE" };
            }
            catch
            {
                return new FeaturesConfigDto { Role = "CANDIDATE" };
            }
        }
    }
}
