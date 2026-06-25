using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Subscription;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase
{
    public class SubscriptionAdminUseCase : ISubscriptionAdminUseCase
    {
        private readonly ISubscriptionRepository _subscriptionRepository;

        public SubscriptionAdminUseCase(ISubscriptionRepository subscriptionRepository)
        {
            _subscriptionRepository = subscriptionRepository;
        }

        public async Task<ResponseBase<PagedResult<SubscriptionDto>>> GetPagedSubscriptionsAsync(
            string? role, 
            SubscriptionStatus? status, 
            int page, 
            int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var (items, total) = await _subscriptionRepository.GetPagedAsync(role, status, page, pageSize);

            var dtos = new List<SubscriptionDto>();
            foreach (var item in items)
            {
                var isUsed = await _subscriptionRepository.IsSubscriptionUsedAsync(item.Id);
                var configDto = DeserializeConfig(item.FeaturesConfig);
                
                dtos.Add(new SubscriptionDto
                {
                    Id = item.Id,
                    Name = item.Name,
                    Price = item.Price,
                    DurationDays = item.DurationDays,
                    FeaturesConfig = configDto,
                    Status = item.Status,
                    CreatedBy = item.CreatedBy,
                    UpdatedBy = item.UpdatedBy,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt,
                    IsUsed = isUsed
                });
            }

            var result = new PagedResult<SubscriptionDto>
            {
                Items = dtos,
                Total = total,
                TotalItems = total,
                Page = page,
                PageSize = pageSize
            };

            return new ResponseBase<PagedResult<SubscriptionDto>>(result);
        }

        public async Task<ResponseBase<SubscriptionDto>> GetSubscriptionByIdAsync(int id)
        {
            var item = await _subscriptionRepository.GetByIdAsync(id);
            if (item == null)
            {
                return new ResponseBase<SubscriptionDto>("Gói dịch vụ không tồn tại.");
            }

            var isUsed = await _subscriptionRepository.IsSubscriptionUsedAsync(item.Id);
            var configDto = DeserializeConfig(item.FeaturesConfig);

            var dto = new SubscriptionDto
            {
                Id = item.Id,
                Name = item.Name,
                Price = item.Price,
                DurationDays = item.DurationDays,
                FeaturesConfig = configDto,
                Status = item.Status,
                CreatedBy = item.CreatedBy,
                UpdatedBy = item.UpdatedBy,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                IsUsed = isUsed
            };

            return new ResponseBase<SubscriptionDto>(dto);
        }

        public async Task<ResponseBase<SubscriptionDto>> CreateSubscriptionAsync(CreateSubscriptionDto dto, Guid userId)
        {
            SanitizeFeaturesConfig(dto.FeaturesConfig);
            var configJson = SerializeConfig(dto.FeaturesConfig);

            var subscription = new Subscriptions
            {
                Name = dto.Name.Trim(),
                Price = dto.Price,
                DurationDays = dto.DurationDays,
                FeaturesConfig = configJson,
                Status = SubscriptionStatus.INACTIVE, // Mặc định tạo mới ở trạng thái INACTIVE
                CreatedBy = userId
            };

            await _subscriptionRepository.CreateAsync(subscription);

            var resultDto = new SubscriptionDto
            {
                Id = subscription.Id,
                Name = subscription.Name,
                Price = subscription.Price,
                DurationDays = subscription.DurationDays,
                FeaturesConfig = dto.FeaturesConfig,
                Status = subscription.Status,
                CreatedBy = subscription.CreatedBy,
                CreatedAt = subscription.CreatedAt,
                IsUsed = false
            };

            return new ResponseBase<SubscriptionDto>(resultDto, "Tạo gói dịch vụ thành công ở trạng thái INACTIVE.");
        }

        public async Task<ResponseBase<SubscriptionDto>> UpdateSubscriptionAsync(int id, UpdateSubscriptionDto dto, Guid userId)
        {
            using var transaction = await _subscriptionRepository.BeginTransactionAsync();
            try
            {
                var subscription = await _subscriptionRepository.GetByIdForUpdateAsync(id);
                if (subscription == null)
                {
                    return new ResponseBase<SubscriptionDto>("Gói dịch vụ không tồn tại.");
                }

                var isUsed = await _subscriptionRepository.IsSubscriptionUsedAsync(id);
                if (isUsed)
                {
                    // Kiểm tra xem có thay đổi các thuộc tính nhạy cảm hay không
                    var oldConfig = DeserializeConfig(subscription.FeaturesConfig);
                    var isPriceChanged = subscription.Price != dto.Price;
                    var isDurationChanged = subscription.DurationDays != dto.DurationDays;
                    var isConfigChanged = !AreConfigsEqual(oldConfig, dto.FeaturesConfig);

                    if (isPriceChanged || isDurationChanged || isConfigChanged)
                    {
                        return new ResponseBase<SubscriptionDto>(
                            "Không thể sửa đổi giá, thời hạn hoặc các giới hạn tính năng của gói dịch vụ đã bán (đã có người đăng ký). Hãy nhân bản (Duplicate) thành gói mới và thay đổi.");
                    }

                    // Gói đã bán chỉ được sửa Name (và UpdatedBy)
                    subscription.Name = dto.Name.Trim();
                    subscription.UpdatedBy = userId;
                    await _subscriptionRepository.UpdateAsync(subscription);
                }
                else
                {
                    // Gói chưa bán, cho phép sửa tất cả.
                    SanitizeFeaturesConfig(dto.FeaturesConfig);

                    subscription.Name = dto.Name.Trim();
                    subscription.Price = dto.Price;
                    subscription.DurationDays = dto.DurationDays;
                    subscription.FeaturesConfig = SerializeConfig(dto.FeaturesConfig);
                    subscription.UpdatedBy = userId;
                    
                    await _subscriptionRepository.UpdateAsync(subscription);
                }

                await transaction.CommitAsync();

                var updatedConfig = DeserializeConfig(subscription.FeaturesConfig);
                var resultDto = new SubscriptionDto
                {
                    Id = subscription.Id,
                    Name = subscription.Name,
                    Price = subscription.Price,
                    DurationDays = subscription.DurationDays,
                    FeaturesConfig = updatedConfig,
                    Status = subscription.Status,
                    CreatedBy = subscription.CreatedBy,
                    UpdatedBy = subscription.UpdatedBy,
                    CreatedAt = subscription.CreatedAt,
                    UpdatedAt = subscription.UpdatedAt,
                    IsUsed = isUsed
                };

                return new ResponseBase<SubscriptionDto>(resultDto, "Cập nhật gói dịch vụ thành công.");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<ResponseBase<SubscriptionDto>> UpdateStatusAsync(int id, SubscriptionStatus status, Guid userId)
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(id);
            if (subscription == null)
            {
                return new ResponseBase<SubscriptionDto>("Gói dịch vụ không tồn tại.");
            }

            subscription.Status = status;
            subscription.UpdatedBy = userId;

            await _subscriptionRepository.UpdateAsync(subscription);

            var configDto = DeserializeConfig(subscription.FeaturesConfig);
            var isUsed = await _subscriptionRepository.IsSubscriptionUsedAsync(id);

            var resultDto = new SubscriptionDto
            {
                Id = subscription.Id,
                Name = subscription.Name,
                Price = subscription.Price,
                DurationDays = subscription.DurationDays,
                FeaturesConfig = configDto,
                Status = subscription.Status,
                CreatedBy = subscription.CreatedBy,
                UpdatedBy = subscription.UpdatedBy,
                CreatedAt = subscription.CreatedAt,
                UpdatedAt = subscription.UpdatedAt,
                IsUsed = isUsed
            };

            return new ResponseBase<SubscriptionDto>(resultDto, $"Cập nhật trạng thái gói dịch vụ thành {status} thành công.");
        }

        public async Task<ResponseBase<SubscriptionDto>> DuplicateSubscriptionAsync(int id, Guid userId)
        {
            var oldSubscription = await _subscriptionRepository.GetByIdAsync(id);
            if (oldSubscription == null)
            {
                return new ResponseBase<SubscriptionDto>("Gói dịch vụ gốc không tồn tại.");
            }

            var oldConfig = DeserializeConfig(oldSubscription.FeaturesConfig) ?? new FeaturesConfigDto();

            var newSubscription = new Subscriptions
            {
                Name = $"{oldSubscription.Name} (Duplicate)".Trim(),
                Price = oldSubscription.Price,
                DurationDays = oldSubscription.DurationDays,
                FeaturesConfig = oldSubscription.FeaturesConfig,
                Status = SubscriptionStatus.INACTIVE, // Luôn mặc định nhân bản ở trạng thái INACTIVE để Admin có thể chỉnh sửa tự do trước khi kích hoạt
                CreatedBy = userId
            };

            await _subscriptionRepository.CreateAsync(newSubscription);

            var resultDto = new SubscriptionDto
            {
                Id = newSubscription.Id,
                Name = newSubscription.Name,
                Price = newSubscription.Price,
                DurationDays = newSubscription.DurationDays,
                FeaturesConfig = oldConfig,
                Status = newSubscription.Status,
                CreatedBy = newSubscription.CreatedBy,
                CreatedAt = newSubscription.CreatedAt,
                IsUsed = false
            };

            return new ResponseBase<SubscriptionDto>(resultDto, "Nhân bản gói dịch vụ thành công ở trạng thái INACTIVE.");
        }

        private FeaturesConfigDto DeserializeConfig(string? json)
        {
            if (string.IsNullOrEmpty(json)) return new FeaturesConfigDto();
            try
            {
                var config = System.Text.Json.JsonSerializer.Deserialize<FeaturesConfigDto>(json, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return config ?? new FeaturesConfigDto();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Dữ liệu cấu hình JSON trong cơ sở dữ liệu bị lỗi và không hợp lệ: " + ex.Message);
            }
        }

        private void SanitizeFeaturesConfig(FeaturesConfigDto config)
        {
            if (config == null || string.IsNullOrEmpty(config.Role)) return;

            if (config.Role.Equals("CANDIDATE", StringComparison.OrdinalIgnoreCase))
            {
                config.ActiveJobPostings = null;
                config.ActiveSourcingLimit = null;
                config.HighlightedJobs = null;
                config.Analytics = null;
            }
            else if (config.Role.Equals("RECRUITER", StringComparison.OrdinalIgnoreCase))
            {
                config.CvMatchLimit = null;
                config.MockInterviewLimit = null;
                config.CvOptimizeLimit = null;
            }
        }

        private string SerializeConfig(FeaturesConfigDto dto)
        {
            return System.Text.Json.JsonSerializer.Serialize(dto, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
        }

        private bool AreConfigsEqual(FeaturesConfigDto? a, FeaturesConfigDto? b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;

            return string.Equals(a.Role, b.Role, StringComparison.OrdinalIgnoreCase) &&
                   a.CvMatchLimit == b.CvMatchLimit &&
                   a.MockInterviewLimit == b.MockInterviewLimit &&
                   a.CvOptimizeLimit == b.CvOptimizeLimit &&
                   a.ActiveJobPostings == b.ActiveJobPostings &&
                   a.ActiveSourcingLimit == b.ActiveSourcingLimit &&
                   a.HighlightedJobs == b.HighlightedJobs &&
                   a.Analytics == b.Analytics;
        }
    }
}
