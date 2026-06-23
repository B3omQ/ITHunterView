using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.DTOs.Subscription;
using ITHunterview.Service.DTOs.CoinConfig;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase
{
    public class CandidateFeatureUsageUseCase : ICandidateFeatureUsageUseCase
    {
        private readonly ITHunterviewContext _context;
        private readonly ISystemConfigRepository _configRepository;
        private const string FeatureCostsKey = "candidate_coin_feature_costs";

        public CandidateFeatureUsageUseCase(ITHunterviewContext context, ISystemConfigRepository configRepository)
        {
            _context = context;
            _configRepository = configRepository;
        }

        public async Task<bool> TryConsumeFeatureAsync(Guid userId, string featureKey)
        {
            if (string.IsNullOrEmpty(featureKey))
                throw new ArgumentException("Feature key không được để trống", nameof(featureKey));

            // 1. Kiểm tra Subscription đang hoạt động (ACTIVE) của người dùng
            var activeSub = await _context.UserSubscriptions
                .Where(us => us.UserId == userId && us.Status == UserSubscriptionStatus.ACTIVE && us.EndDate >= DateTime.UtcNow)
                .OrderByDescending(us => us.EndDate)
                .FirstOrDefaultAsync();

            if (activeSub != null)
            {
                // Lấy Subscription details bằng cách join thủ công tránh lỗi thiếu navigation property
                var subscription = await _context.Subscriptions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == activeSub.SubId && s.Status == SubscriptionStatus.ACTIVE);

                if (subscription != null && !string.IsNullOrEmpty(subscription.FeaturesConfig))
                {
                    FeaturesConfigDto? features = null;
                    try
                    {
                        features = JsonSerializer.Deserialize<FeaturesConfigDto>(subscription.FeaturesConfig, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    catch
                    {
                        // JSON lỗi, bỏ qua để đi tiếp luồng trừ Coin
                    }

                    if (features != null)
                    {
                        int limit = featureKey switch
                        {
                            "CvJdMatching" => features.CvMatchLimit ?? 0,
                            "MockInterview" => features.MockInterviewLimit ?? 0,
                            "CvOptimize" => features.CvOptimizeLimit ?? 0,
                            _ => 0
                        };

                        if (limit == -1) // Không giới hạn
                        {
                            return true;
                        }

                        if (limit > 0)
                        {
                            int usedCount = await GetUsedCountInPeriodAsync(userId, featureKey, activeSub.StartDate, activeSub.EndDate);
                            if (usedCount < limit)
                            {
                                return true; // Hạn mức Subscription còn, cho phép thực hiện
                            }
                        }
                    }
                }
            }

            // 2. Không có Subscription hoặc đã hết hạn mức -> Tiêu tốn Coin từ ví Pay-as-you-go
            var costConfig = await _configRepository.GetByKeyAsync(FeatureCostsKey);
            CoinFeatureCostsDto costs;
            if (costConfig != null && !string.IsNullOrEmpty(costConfig.ConfigValue))
            {
                try
                {
                    costs = JsonSerializer.Deserialize<CoinFeatureCostsDto>(costConfig.ConfigValue, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? GetDefaultCosts();
                }
                catch
                {
                    costs = GetDefaultCosts();
                }
            }
            else
            {
                costs = GetDefaultCosts();
            }

            int coinCost = featureKey switch
            {
                "CvJdMatching" => costs.CvJdMatching,
                "MockInterview" => costs.MockInterview,
                "CvOptimize" => costs.CvOptimize,
                _ => 0
            };

            if (coinCost == 0)
            {
                return true; // Tính năng miễn phí theo cấu hình
            }

            // Thực hiện trừ ví bằng Database Transaction kết hợp Row-Level Lock để tránh Race Condition (Spam click)
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Áp dụng Pessimistic Lock (SELECT FOR UPDATE) trên PostgreSQL để khóa dòng ví của người dùng
                    var wallet = await _context.UserWallets
                        .FromSqlRaw("SELECT * FROM user_wallets WHERE user_id = {0} LIMIT 1 FOR UPDATE", userId)
                        .FirstOrDefaultAsync();

                    if (wallet == null)
                    {
                        // Nếu chưa có ví, tạo mới với balance = 0
                        wallet = new UserWallets
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId,
                            Balance = 0,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _context.UserWallets.Add(wallet);
                        await _context.SaveChangesAsync();
                    }

                    if (wallet.Balance < coinCost)
                    {
                        throw new InvalidOperationException($"Số dư ví không đủ. Tính năng này yêu cầu {coinCost} Coin nhưng bạn hiện chỉ có {wallet.Balance} Coin. Vui lòng nạp thêm Coin.");
                    }

                    // Trừ số dư ví
                    wallet.Balance -= coinCost;
                    wallet.UpdatedAt = DateTime.UtcNow;
                    _context.UserWallets.Update(wallet);

                    // Tạo lịch sử giao dịch Coin
                    var creditTx = new CreditTransactions
                    {
                        Id = Guid.NewGuid(),
                        WalletId = wallet.Id,
                        Amount = -coinCost,
                        TransactionType = CreditTransactionType.DEDUCT,
                        Description = $"Sử dụng {coinCost} Coin cho tính năng {GetFeatureFriendlyName(featureKey)}",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.CreditTransactions.Add(creditTx);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return true;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        private async Task<int> GetUsedCountInPeriodAsync(Guid userId, string featureKey, DateTime start, DateTime end)
        {
            switch (featureKey)
            {
                case "CvJdMatching":
                    // Đếm số lần thực hiện matching trong chu kỳ dựa trên lịch sử cv_job_match_scores
                    return await (from match in _context.CvJobMatchScores
                                  join cv in _context.Cvs on match.CvId equals cv.Id
                                  where cv.UserId == userId && match.UpdatedAt >= start && match.UpdatedAt <= end
                                  select match.Id).CountAsync();

                case "MockInterview":
                    // Đếm số lần mock interview trong chu kỳ dựa trên lịch sử interview_sessions
                    return await _context.InterviewSessions
                        .Where(x => x.CandidateId == userId && x.StartedAt >= start && x.StartedAt <= end)
                        .CountAsync();

                case "CvOptimize":
                    // Đếm số lần tối ưu CV (Dựa trên số lượng log trong ai_api_usage_logs liên quan đến CV của user)
                    // Hoặc đơn giản là đếm số CV được cập nhật trong chu kỳ
                    return await _context.Cvs
                        .Where(x => x.UserId == userId && x.UpdatedAt >= start && x.UpdatedAt <= end)
                        .CountAsync();

                default:
                    return 0;
            }
        }

        private CoinFeatureCostsDto GetDefaultCosts()
        {
            return new CoinFeatureCostsDto
            {
                CvJdMatching = 2,
                MockInterview = 10,
                CvOptimize = 3
            };
        }

        private string GetFeatureFriendlyName(string featureKey)
        {
            return featureKey switch
            {
                "CvJdMatching" => "So khớp CV-JD AI",
                "MockInterview" => "Phỏng vấn thử AI Mock Interview",
                "CvOptimize" => "Tối ưu hóa CV AI",
                _ => featureKey
            };
        }
    }
}
