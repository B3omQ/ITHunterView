using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.CoinConfig;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase
{
    public class CoinConfigUseCase : ICoinConfigUseCase
    {
        private readonly ISystemConfigRepository _configRepository;
        private const string FeatureCostsKey = "candidate_coin_feature_costs";
        private const string PackagesKey = "candidate_coin_packages";

        public CoinConfigUseCase(ISystemConfigRepository configRepository)
        {
            _configRepository = configRepository;
        }

        public async Task<ResponseBase<UpdateCoinConfigDto>> GetCoinConfigAsync()
        {
            var result = new UpdateCoinConfigDto();

            // Lấy chi phí tính năng
            var featureCostConfig = await _configRepository.GetByKeyAsync(FeatureCostsKey);
            if (featureCostConfig != null && !string.IsNullOrEmpty(featureCostConfig.ConfigValue))
            {
                try
                {
                    result.FeatureCosts = JsonSerializer.Deserialize<CoinFeatureCostsDto>(featureCostConfig.ConfigValue, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? GetDefaultFeatureCosts();
                }
                catch
                {
                    result.FeatureCosts = GetDefaultFeatureCosts();
                }
            }
            else
            {
                result.FeatureCosts = GetDefaultFeatureCosts();
            }

            // Lấy danh sách gói nạp coin
            var packagesConfig = await _configRepository.GetByKeyAsync(PackagesKey);
            if (packagesConfig != null && !string.IsNullOrEmpty(packagesConfig.ConfigValue))
            {
                try
                {
                    result.Packages = JsonSerializer.Deserialize<List<CoinPackageDto>>(packagesConfig.ConfigValue, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? GetDefaultPackages();
                }
                catch
                {
                    result.Packages = GetDefaultPackages();
                }
            }
            else
            {
                result.Packages = GetDefaultPackages();
            }

            return new ResponseBase<UpdateCoinConfigDto>(result, "Lấy cấu hình Coin thành công");
        }

        public async Task<ResponseBase<UpdateCoinConfigDto>> UpdateCoinConfigAsync(UpdateCoinConfigDto dto, Guid actorUserId)
        {
            // Validate dữ liệu đầu vào
            if (dto.FeatureCosts == null)
                return new ResponseBase<UpdateCoinConfigDto>("Cấu hình chi phí tính năng không được để trống");
            
            if (dto.FeatureCosts.CvJdMatching < 0 || dto.FeatureCosts.MockInterview < 0 || dto.FeatureCosts.CvOptimize < 0)
                return new ResponseBase<UpdateCoinConfigDto>("Chi phí Coin của các tính năng không được nhỏ hơn 0");

            if (dto.Packages == null || dto.Packages.Count == 0)
                return new ResponseBase<UpdateCoinConfigDto>("Danh sách gói nạp Coin không được để trống");

            foreach (var pkg in dto.Packages)
            {
                if (string.IsNullOrEmpty(pkg.Id) || !Guid.TryParse(pkg.Id, out _))
                    pkg.Id = Guid.NewGuid().ToString("D");
                if (string.IsNullOrEmpty(pkg.Name))
                    return new ResponseBase<UpdateCoinConfigDto>("Tên gói nạp Coin không được để trống");
                if (pkg.Coins <= 0)
                    return new ResponseBase<UpdateCoinConfigDto>("Số lượng Coin trong gói phải lớn hơn 0");
                if (pkg.Price <= 0)
                    return new ResponseBase<UpdateCoinConfigDto>("Giá của gói nạp phải lớn hơn 0");
            }

            // Serialize và lưu cấu hình chi phí tính năng
            var featureCostJson = JsonSerializer.Serialize(dto.FeatureCosts);
            var featureCostConfig = new SystemConfigs
            {
                ConfigKey = FeatureCostsKey,
                ConfigValue = featureCostJson,
                Description = "Cấu hình chi phí Coin cho các tính năng của Candidate",
                UpdatedBy = actorUserId
            };
            await _configRepository.SaveAsync(featureCostConfig);

            // Serialize và lưu cấu hình gói nạp Coin
            var packagesJson = JsonSerializer.Serialize(dto.Packages);
            var packagesConfig = new SystemConfigs
            {
                ConfigKey = PackagesKey,
                ConfigValue = packagesJson,
                Description = "Cấu hình danh sách các gói nạp Coin cho Candidate",
                UpdatedBy = actorUserId
            };
            await _configRepository.SaveAsync(packagesConfig);

            return new ResponseBase<UpdateCoinConfigDto>(dto, "Cập nhật cấu hình Coin thành công");
        }

        private CoinFeatureCostsDto GetDefaultFeatureCosts()
        {
            return new CoinFeatureCostsDto
            {
                CvJdMatching = 2,
                MockInterview = 10,
                CvOptimize = 3
            };
        }

        private List<CoinPackageDto> GetDefaultPackages()
        {
            return new List<CoinPackageDto>
            {
                new() { Id = "00000000-0000-0000-0000-000000000020", Name = "Gói nạp 20 Coin", Coins = 20, Price = 39000, IsActive = true },
                new() { Id = "00000000-0000-0000-0000-000000000050", Name = "Gói nạp 50 Coin", Coins = 50, Price = 89000, IsActive = true },
                new() { Id = "00000000-0000-0000-0000-000000000120", Name = "Gói nạp 120 Coin", Coins = 120, Price = 199000, IsActive = true }
            };
        }
    }
}
