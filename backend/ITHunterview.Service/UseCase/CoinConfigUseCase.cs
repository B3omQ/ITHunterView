using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.CoinConfig;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.SignalR;
using ITHunterview.Service.Hubs;

namespace ITHunterview.Service.UseCase
{
    public class CoinConfigUseCase : ICoinConfigUseCase
    {
        private readonly ITHunterviewContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public CoinConfigUseCase(
            ITHunterviewContext context,
            IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<ResponseBase<UpdateCoinConfigDto>> GetCoinConfigAsync()
        {
            var result = new UpdateCoinConfigDto();

            // 1. Lấy chi phí tính năng từ bảng coin_features
            var dbFeatures = await _context.CoinFeatures.AsNoTracking().ToListAsync();
            result.FeatureCosts = new CoinFeatureCostsDto
            {
                CvJdMatching = dbFeatures.FirstOrDefault(f => f.FeatureKey == "CvJdMatching")?.CoinCost ?? 1000,
                MockInterview = dbFeatures.FirstOrDefault(f => f.FeatureKey == "MockInterview")?.CoinCost ?? 2000,
                LearningPath = dbFeatures.FirstOrDefault(f => f.FeatureKey == "LearningPath")?.CoinCost ?? 500,
                UnlockCv = dbFeatures.FirstOrDefault(f => f.FeatureKey == "UnlockCv")?.CoinCost ?? 3000,
                PostJob = dbFeatures.FirstOrDefault(f => f.FeatureKey == "PostJob")?.CoinCost ?? 20000,
                ExtendJob = dbFeatures.FirstOrDefault(f => f.FeatureKey == "ExtendJob")?.CoinCost ?? 10000,
                PushTop = dbFeatures.FirstOrDefault(f => f.FeatureKey == "PushTop")?.CoinCost ?? 5000
            };

            // 2. Lấy danh sách gói nạp coin từ bảng coin_packages
            var dbPackages = await _context.CoinPackages
                .AsNoTracking()
                .OrderBy(p => p.Price)
                .ToListAsync();

            if (dbPackages.Count > 0)
            {
                result.Packages = dbPackages.Select(p => new CoinPackageDto
                {
                    Id = p.Id.ToString("D"),
                    Name = p.Name,
                    Coins = p.Coins,
                    Price = p.Price,
                    IsActive = p.IsActive
                }).ToList();
            }
            else
            {
                // Fallback default packages if DB is empty
                result.Packages = GetDefaultPackages();
            }

            return new ResponseBase<UpdateCoinConfigDto>(result, "Lấy cấu hình Coin thành công");
        }

        public async Task<ResponseBase<UpdateCoinConfigDto>> GetPublicCoinConfigAsync()
        {
            var result = new UpdateCoinConfigDto();

            // 1. Lấy chi phí tính năng
            var dbFeatures = await _context.CoinFeatures.AsNoTracking().ToListAsync();
            result.FeatureCosts = new CoinFeatureCostsDto
            {
                CvJdMatching = dbFeatures.FirstOrDefault(f => f.FeatureKey == "CvJdMatching")?.CoinCost ?? 1000,
                MockInterview = dbFeatures.FirstOrDefault(f => f.FeatureKey == "MockInterview")?.CoinCost ?? 2000,
                LearningPath = dbFeatures.FirstOrDefault(f => f.FeatureKey == "LearningPath")?.CoinCost ?? 500,
                UnlockCv = dbFeatures.FirstOrDefault(f => f.FeatureKey == "UnlockCv")?.CoinCost ?? 3000,
                PostJob = dbFeatures.FirstOrDefault(f => f.FeatureKey == "PostJob")?.CoinCost ?? 20000,
                ExtendJob = dbFeatures.FirstOrDefault(f => f.FeatureKey == "ExtendJob")?.CoinCost ?? 10000,
                PushTop = dbFeatures.FirstOrDefault(f => f.FeatureKey == "PushTop")?.CoinCost ?? 5000
            };

            // 2. Lấy danh sách gói nạp coin (chỉ lấy IsActive = true)
            var dbPackages = await _context.CoinPackages
                .Where(p => p.IsActive)
                .AsNoTracking()
                .OrderBy(p => p.Price)
                .ToListAsync();

            if (dbPackages.Count > 0)
            {
                result.Packages = dbPackages.Select(p => new CoinPackageDto
                {
                    Id = p.Id.ToString("D"),
                    Name = p.Name,
                    Coins = p.Coins,
                    Price = p.Price,
                    IsActive = p.IsActive
                }).ToList();
            }
            else
            {
                // Fallback default packages if DB is empty, but filter by IsActive conceptually
                result.Packages = GetDefaultPackages().Where(p => p.IsActive).ToList();
            }

            return new ResponseBase<UpdateCoinConfigDto>(result, "Lấy cấu hình gói Coin public thành công");
        }

        public async Task<ResponseBase<UpdateCoinConfigDto>> UpdateCoinConfigAsync(UpdateCoinConfigDto dto, Guid actorUserId)
        {
            // Validate dữ liệu đầu vào
            if (dto.FeatureCosts == null)
                return new ResponseBase<UpdateCoinConfigDto>("Cấu hình chi phí tính năng không được để trống");
            
            if (dto.FeatureCosts.CvJdMatching < 0 || dto.FeatureCosts.MockInterview < 0 || dto.FeatureCosts.LearningPath < 0 || dto.FeatureCosts.UnlockCv < 0 || dto.FeatureCosts.PostJob < 0 || dto.FeatureCosts.ExtendJob < 0 || dto.FeatureCosts.PushTop < 0)
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

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Cập nhật CoinFeatures
                    var existingFeatures = await _context.CoinFeatures.ToListAsync();
                    var featuresToUpdate = new List<(string Key, int Cost, string Desc)>
                    {
                        ("CvJdMatching", dto.FeatureCosts.CvJdMatching, "So khớp CV-JD AI"),
                        ("MockInterview", dto.FeatureCosts.MockInterview, "Phỏng vấn thử AI Mock Interview"),
                        ("LearningPath", dto.FeatureCosts.LearningPath, "Tạo Learning Path"),
                        ("UnlockCv", dto.FeatureCosts.UnlockCv, "Mở khóa thông tin CV"),
                        ("PostJob", dto.FeatureCosts.PostJob, "Đăng Job (30 ngày)"),
                        ("ExtendJob", dto.FeatureCosts.ExtendJob, "Gia hạn Job (15 ngày)"),
                        ("PushTop", dto.FeatureCosts.PushTop, "Đẩy Job lên Top (1 ngày)")
                    };

                    foreach (var f in featuresToUpdate)
                    {
                        var dbFeature = existingFeatures.FirstOrDefault(x => x.FeatureKey == f.Key);
                        if (dbFeature != null)
                        {
                            dbFeature.CoinCost = f.Cost;
                            dbFeature.Description = f.Desc;
                            dbFeature.UpdatedBy = actorUserId;
                            dbFeature.UpdatedAt = DateTime.UtcNow;
                            _context.CoinFeatures.Update(dbFeature);
                        }
                        else
                        {
                            var newFeature = new CoinFeatures
                            {
                                FeatureKey = f.Key,
                                CoinCost = f.Cost,
                                Description = f.Desc,
                                UpdatedBy = actorUserId,
                                UpdatedAt = DateTime.UtcNow
                            };
                            _context.CoinFeatures.Add(newFeature);
                        }
                    }

                    // 2. Cập nhật CoinPackages
                    var existingPackages = await _context.CoinPackages.ToListAsync();
                    var dtoPackageIds = dto.Packages.Select(p => Guid.Parse(p.Id)).ToList();

                    // Xóa các gói không có trong danh sách cập nhật mới
                    var toDelete = existingPackages.Where(p => !dtoPackageIds.Contains(p.Id)).ToList();
                    if (toDelete.Any())
                    {
                        _context.CoinPackages.RemoveRange(toDelete);
                    }

                    // Cập nhật hoặc thêm mới các gói nạp
                    foreach (var pkgDto in dto.Packages)
                    {
                        var pkgId = Guid.Parse(pkgDto.Id);
                        var dbPkg = existingPackages.FirstOrDefault(p => p.Id == pkgId);
                        if (dbPkg != null)
                        {
                            dbPkg.Name = pkgDto.Name;
                            dbPkg.Coins = pkgDto.Coins;
                            dbPkg.Price = pkgDto.Price;
                            dbPkg.IsActive = pkgDto.IsActive;
                            dbPkg.UpdatedBy = actorUserId;
                            dbPkg.UpdatedAt = DateTime.UtcNow;
                            _context.CoinPackages.Update(dbPkg);
                        }
                        else
                        {
                            var newPkg = new CoinPackages
                            {
                                Id = pkgId,
                                Name = pkgDto.Name,
                                Coins = pkgDto.Coins,
                                Price = pkgDto.Price,
                                IsActive = pkgDto.IsActive,
                                UpdatedBy = actorUserId,
                                UpdatedAt = DateTime.UtcNow
                            };
                            _context.CoinPackages.Add(newPkg);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    await _hubContext.Clients.All.SendAsync("ReceivePricingUpdate");

                    return new ResponseBase<UpdateCoinConfigDto>(dto, "Cập nhật cấu hình Coin thành công");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            return new ResponseBase<UpdateCoinConfigDto>(dto, "Cập nhật cấu hình Coin thành công");
        }

        private List<CoinPackageDto> GetDefaultPackages()
        {
            return new List<CoinPackageDto>
            {
                new() { Id = "00000000-0000-0000-0000-000000000100", Name = "Starter Package", Coins = 10500, Price = 100000, IsActive = true },
                new() { Id = "00000000-0000-0000-0000-000000000500", Name = "Value Package", Coins = 60000, Price = 500000, IsActive = true },
                new() { Id = "00000000-0000-0000-0000-000000002000", Name = "Pro Funder Package", Coins = 300000, Price = 2000000, IsActive = true }
            };
        }
    }
}
