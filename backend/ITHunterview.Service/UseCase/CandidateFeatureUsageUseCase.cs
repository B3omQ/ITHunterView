using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.DTOs.Subscription;
using ITHunterview.Service.DTOs.CoinConfig;
using ITHunterview.Service.DTOs.FeatureUsage;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase
{
    public class CandidateFeatureUsageUseCase : ICandidateFeatureUsageUseCase
    {
        private readonly ITHunterviewContext _context;
        private readonly ISystemConfigRepository _configRepository;
        private readonly IFeatureUsageReservationRepository _reservationRepository;
        private const string FeatureCostsKey = "candidate_coin_feature_costs";

        public CandidateFeatureUsageUseCase(
            ITHunterviewContext context,
            ISystemConfigRepository configRepository,
            IFeatureUsageReservationRepository reservationRepository)
        {
            _context = context;
            _configRepository = configRepository;
            _reservationRepository = reservationRepository;
        }

        public async Task<FeatureConsumptionResult> TryConsumeFeatureAsync(Guid userId, string featureKey, string? referenceId = null)
        {
            if (string.IsNullOrEmpty(featureKey))
                throw new ArgumentException("Feature key không được để trống", nameof(featureKey));

            var transaction = _context.Database.CurrentTransaction;
            var ownsTransaction = transaction == null;
            if (ownsTransaction)
            {
                transaction = await _context.Database.BeginTransactionAsync();
            }

            try
            {
                    // 1. Đảm bảo record ví luôn tồn tại trong user_wallets một cách atomic trên PostgreSQL
                    await _context.Database.ExecuteSqlRawAsync(
                        "INSERT INTO user_wallets (id, user_id, balance, updated_at) VALUES ({0}, {1}, 0, {2}) ON CONFLICT (user_id) DO NOTHING;",
                        Guid.NewGuid(), userId, DateTime.UtcNow);

                    // 2. Áp dụng Pessimistic Lock (SELECT FOR UPDATE) trên PostgreSQL để khóa dòng ví của người dùng
                    // Điều này đóng vai trò như một mutex per-user cho toàn bộ luồng check subscription & trừ coin
                    var wallet = await _context.UserWallets
                        .FromSqlRaw("SELECT * FROM user_wallets WHERE user_id = {0} LIMIT 1 FOR UPDATE", userId)
                        .FirstOrDefaultAsync();

                    if (wallet == null)
                    {
                        throw new InvalidOperationException($"Could not obtain lock on user_wallets for user {userId}");
                    }

                    // 2. Kiểm tra Subscription đang hoạt động (ACTIVE) của người dùng
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
                                    "LearningPath" => features.LearningPathLimit ?? (features.LearningPathSlotLimit ?? 0),
                                    "PostJob" => features.JobSlots ?? 1, // Gói mặc định hoặc configured slot
                                    "UnlockCv" => features.UnlockCvLimit ?? 0,
                                    "ExtendJob" => features.JobExtendLimit ?? 0,
                                    "PushTop" => features.PushTopLimit ?? 0,
                                    _ => 0
                                };

                                if (limit == -1) // Không giới hạn
                                {
                                    var usageLogId = await RecordFeatureUsageLogAsync(userId, featureKey, referenceId, true);
                                    await _context.SaveChangesAsync();
                                    if (ownsTransaction)
                                    {
                                        await transaction!.CommitAsync();
                                    }
                                    return new FeatureConsumptionResult { UsageLogId = usageLogId };
                                }

                                if (limit > 0)
                                {
                                    int usedCount = await GetUsedCountInPeriodAsync(userId, featureKey, activeSub.StartDate, activeSub.EndDate);
                                    if (usedCount < limit)
                                    {
                                        var usageLogId = await RecordFeatureUsageLogAsync(userId, featureKey, referenceId, true);
                                        await _context.SaveChangesAsync();
                                        if (ownsTransaction)
                                        {
                                            await transaction!.CommitAsync();
                                        }
                                        return new FeatureConsumptionResult { UsageLogId = usageLogId }; // Hạn mức Subscription còn, cho phép thực hiện
                                    }
                                }
                            }
                        }
                    }

                    // Nếu không có gói Active, kiểm tra quyền hạn miễn phí cho gói Free mặc định
                    if (activeSub == null && featureKey == "PostJob")
                    {
                        int defaultFreeSlotLimit = 1;
                        int usedCount = await GetUsedCountInPeriodAsync(userId, featureKey, DateTime.MinValue, DateTime.MaxValue);
                        if (usedCount < defaultFreeSlotLimit)
                        {
                            if (ownsTransaction)
                            {
                                await transaction!.CommitAsync();
                            }
                            return new FeatureConsumptionResult(); // Gói Free được miễn phí 1 slot đăng việc Active
                        }
                    }

                    // 3. Không có Subscription hoặc đã hết hạn mức -> Tiêu tốn Coin từ ví Pay-as-you-go
                    // Truy vấn từ bảng chuyên biệt CoinFeatures
                    var dbFeature = await _context.CoinFeatures
                        .AsNoTracking()
                        .FirstOrDefaultAsync(cf => cf.FeatureKey == featureKey);

                    int coinCost;
                    if (dbFeature != null)
                    {
                        coinCost = dbFeature.CoinCost;
                    }
                    else
                    {
                        // Fallback default
                        var defaultCosts = GetDefaultCosts();
                        coinCost = featureKey switch
                        {
                            "CvJdMatching" => defaultCosts.CvJdMatching,
                            "MockInterview" => defaultCosts.MockInterview,
                            "LearningPath" => defaultCosts.LearningPath,
                            "PostJob" => defaultCosts.PostJob,
                            "UnlockCv" => defaultCosts.UnlockCv,
                            "ExtendJob" => defaultCosts.ExtendJob,
                            "PushTop" => defaultCosts.PushTop,
                            _ => 0
                        };
                    }

                    if (coinCost == 0)
                    {
                        if (ownsTransaction)
                        {
                            await transaction!.CommitAsync();
                        }
                        return new FeatureConsumptionResult(); // Tính năng miễn phí theo cấu hình
                    }

                    if (wallet.Balance < coinCost)
                    {
                        if (featureKey == "PostJob")
                        {
                            throw new InvalidOperationException($"Bạn đã sử dụng hết số slot đăng tin Active miễn phí trong gói hiện tại. Để đăng thêm tin mới, bạn cần trả {coinCost:N0} Coin nhưng số dư ví không đủ (hiện có {wallet.Balance:N0} Coin). Vui lòng nạp thêm Coin hoặc nâng cấp gói dịch vụ để nhận thêm slot.");
                        }
                        if (featureKey == "ExtendJob")
                        {
                            throw new InvalidOperationException($"Bạn đã sử dụng hết lượt gia hạn tin miễn phí trong gói hiện tại. Để gia hạn tin (thêm 15 ngày), bạn cần trả {coinCost:N0} Coin nhưng số dư ví không đủ (hiện có {wallet.Balance:N0} Coin). Vui lòng nạp thêm Coin hoặc nâng cấp gói dịch vụ.");
                        }
                        throw new InvalidOperationException($"Số dư ví không đủ. Tính năng này yêu cầu {coinCost:N0} Coin nhưng bạn hiện chỉ có {wallet.Balance:N0} Coin. Vui lòng nạp thêm Coin.");
                    }

                    // Trừ số dư ví
                    wallet.Balance -= coinCost;
                    wallet.UpdatedAt = DateTime.UtcNow;
                    _context.UserWallets.Update(wallet);

                    // Parse reference GUID nếu có
                    Guid? refGuid = null;
                    if (!string.IsNullOrEmpty(referenceId) && Guid.TryParse(referenceId, out var parsedGuid))
                    {
                        refGuid = parsedGuid;
                    }

                    // Tạo lịch sử giao dịch Coin
                    var creditTx = new CreditTransactions
                    {
                        Id = Guid.NewGuid(),
                        WalletId = wallet.Id,
                        Amount = -coinCost,
                        TransactionType = CreditTransactionType.DEDUCT,
                        ReferenceId = refGuid,
                        Description = $"Sử dụng {coinCost} Coin cho tính năng {GetFeatureFriendlyName(featureKey)}",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.CreditTransactions.Add(creditTx);

                    var coinUsageLogId = await RecordFeatureUsageLogAsync(userId, featureKey, referenceId, false);

                    await _context.SaveChangesAsync();
                    if (ownsTransaction)
                    {
                        await transaction!.CommitAsync();
                    }

                    return new FeatureConsumptionResult
                    {
                        ChargedCoins = coinCost,
                        DeductTransactionId = creditTx.Id,
                        UsageLogId = coinUsageLogId
                    };
            }
            catch (Exception)
            {
                if (ownsTransaction && transaction != null)
                {
                    await transaction.RollbackAsync();
                }
                throw;
            }
            finally
            {
                if (ownsTransaction && transaction != null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }

        public async Task RefundFeatureUsageAsync(Guid userId, FeatureConsumptionResult consumption, string description)
        {
            if (consumption == null)
            {
                return;
            }

            var existingTransaction = _context.Database.CurrentTransaction;
            var ownsTransaction = existingTransaction == null;
            if (ownsTransaction)
            {
                existingTransaction = await _context.Database.BeginTransactionAsync();
            }

            try
            {
                if (consumption.UsageLogId.HasValue)
                {
                    var usageLog = await _context.UserActivityLogs
                        .FirstOrDefaultAsync(x => x.Id == consumption.UsageLogId.Value && x.UserId == userId);
                    if (usageLog != null)
                    {
                        _context.UserActivityLogs.Remove(usageLog);
                    }
                }

                if (consumption.DeductTransactionId.HasValue)
                {
                    var deductTransaction = await _context.CreditTransactions
                        .FirstOrDefaultAsync(x => x.Id == consumption.DeductTransactionId.Value && x.TransactionType == CreditTransactionType.DEDUCT);

                    if (deductTransaction != null)
                    {
                        var alreadyRefunded = await _context.CreditTransactions.AnyAsync(x =>
                            x.TransactionType == CreditTransactionType.REFUND &&
                            x.ReferenceId == deductTransaction.Id);

                        if (!alreadyRefunded)
                        {
                            var wallet = await _context.UserWallets
                                .FromSqlRaw("SELECT * FROM user_wallets WHERE id = {0} LIMIT 1 FOR UPDATE", deductTransaction.WalletId)
                                .FirstOrDefaultAsync();

                            if (wallet == null || wallet.UserId != userId)
                            {
                                throw new InvalidOperationException("Không tìm thấy ví cần hoàn Coin.");
                            }

                            var amountToRefund = Math.Abs(deductTransaction.Amount);
                            wallet.Balance += amountToRefund;
                            wallet.UpdatedAt = DateTime.UtcNow;
                            _context.UserWallets.Update(wallet);

                            _context.CreditTransactions.Add(new CreditTransactions
                            {
                                Id = Guid.NewGuid(),
                                WalletId = wallet.Id,
                                Amount = amountToRefund,
                                TransactionType = CreditTransactionType.REFUND,
                                ReferenceId = deductTransaction.Id,
                                Description = description,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();
                if (ownsTransaction)
                {
                    await existingTransaction!.CommitAsync();
                }
            }
            catch
            {
                if (ownsTransaction && existingTransaction != null)
                {
                    await existingTransaction.RollbackAsync();
                }
                throw;
            }
            finally
            {
                if (ownsTransaction && existingTransaction != null)
                {
                    await existingTransaction.DisposeAsync();
                }
            }
        }

        public async Task RefundFeatureUsageByReferenceAsync(Guid userId, Guid referenceId, string description)
        {
            var deductTransaction = await _context.CreditTransactions
                .Where(x => x.TransactionType == CreditTransactionType.DEDUCT && x.ReferenceId == referenceId)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (deductTransaction == null)
            {
                return;
            }

            await RefundFeatureUsageAsync(userId, new FeatureConsumptionResult
            {
                ChargedCoins = Math.Abs(deductTransaction.Amount),
                DeductTransactionId = deductTransaction.Id
            }, description);
        }

        public async Task<FeatureReservationResult> ReserveFeatureAsync(
            Guid userId,
            string featureKey,
            Guid referenceId,
            CancellationToken cancellationToken = default)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User id is required.", nameof(userId));
            if (string.IsNullOrWhiteSpace(featureKey))
                throw new ArgumentException("Feature key is required.", nameof(featureKey));
            if (referenceId == Guid.Empty)
                throw new ArgumentException("Reference id is required.", nameof(referenceId));

            var (transaction, ownsTransaction) = await BeginReservationTransactionAsync(cancellationToken);
            try
            {
                var wallet = await EnsureWalletAndLockAsync(userId, cancellationToken);
                var existing = await _reservationRepository.GetByReferenceForUpdateAsync(referenceId, cancellationToken);
                if (existing != null)
                {
                    if (existing.UserId != userId || !string.Equals(existing.FeatureKey, featureKey, StringComparison.Ordinal))
                        throw new InvalidOperationException("The billing reference is already owned by another feature request.");

                    if (ownsTransaction)
                        await transaction!.CommitAsync(cancellationToken);
                    return ToReservationResult(existing);
                }

                var now = DateTime.UtcNow;
                var source = "Coin";
                var coinAmount = 0;
                var activeSubscription = await _context.UserSubscriptions
                    .Where(us => us.UserId == userId
                                 && us.Status == UserSubscriptionStatus.ACTIVE
                                 && us.EndDate >= now)
                    .OrderByDescending(us => us.EndDate)
                    .FirstOrDefaultAsync(cancellationToken);

                if (activeSubscription != null)
                {
                    var subscription = await _context.Subscriptions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == activeSubscription.SubId && s.Status == SubscriptionStatus.ACTIVE, cancellationToken);
                    var configuredLimit = GetFeatureLimit(featureKey, subscription);
                    var usedCount = configuredLimit > 0
                        ? await GetActiveMatchingUsageCountAsync(
                            userId,
                            featureKey,
                            activeSubscription.StartDate,
                            activeSubscription.EndDate,
                            referenceId,
                            cancellationToken)
                        : 0;
                    if (configuredLimit == -1 || (configuredLimit > 0 && usedCount < configuredLimit))
                        source = "Subscription";
                }

                if (source == "Coin")
                {
                    coinAmount = await GetCoinCostAsync(featureKey, cancellationToken);
                    if (coinAmount > 0 && wallet.Balance < coinAmount)
                        throw new InvalidOperationException("Insufficient coin balance for this feature.");
                    if (coinAmount == 0)
                        source = "Free";
                }

                var reservation = new FeatureUsageReservations
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    FeatureKey = featureKey,
                    ReferenceId = referenceId,
                    Source = source,
                    Status = "Reserved",
                    CoinAmount = coinAmount,
                    CreatedAt = now
                };
                _reservationRepository.Add(reservation);
                await _context.SaveChangesAsync(cancellationToken);

                if (ownsTransaction)
                    await transaction!.CommitAsync(cancellationToken);
                return ToReservationResult(reservation);
            }
            catch
            {
                if (ownsTransaction && transaction != null)
                    await transaction.RollbackAsync(cancellationToken);
                throw;
            }
            finally
            {
                if (ownsTransaction && transaction != null)
                    await transaction.DisposeAsync();
            }
        }

        public async Task CaptureFeatureReservationAsync(
            Guid reservationId,
            CancellationToken cancellationToken = default)
        {
            if (reservationId == Guid.Empty)
                throw new ArgumentException("Reservation id is required.", nameof(reservationId));

            var (transaction, ownsTransaction) = await BeginReservationTransactionAsync(cancellationToken);
            try
            {
                var reservation = await _reservationRepository.GetByIdForUpdateAsync(reservationId, cancellationToken)
                    ?? throw new InvalidOperationException("Billing reservation was not found.");
                if (reservation.Status == "Captured")
                {
                    if (ownsTransaction)
                        await transaction!.CommitAsync(cancellationToken);
                    return;
                }
                if (reservation.Status == "Released" || reservation.Status == "Refunded")
                    throw new InvalidOperationException("A released or refunded reservation cannot be captured.");
                if (reservation.Status != "Reserved")
                    throw new InvalidOperationException("Billing reservation is in an invalid state.");

                if (reservation.Source == "Coin" && reservation.CoinAmount > 0)
                {
                    var wallet = await EnsureWalletAndLockAsync(reservation.UserId, cancellationToken);
                    if (wallet.Balance < reservation.CoinAmount)
                        throw new InvalidOperationException("Insufficient coin balance for this feature.");

                    wallet.Balance -= reservation.CoinAmount;
                    wallet.UpdatedAt = DateTime.UtcNow;
                    var deductTransaction = new CreditTransactions
                    {
                        Id = Guid.NewGuid(),
                        WalletId = wallet.Id,
                        Amount = -reservation.CoinAmount,
                        TransactionType = CreditTransactionType.DEDUCT,
                        ReferenceId = reservation.ReferenceId,
                        Description = "Feature usage reservation captured",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.UserWallets.Update(wallet);
                    _context.CreditTransactions.Add(deductTransaction);
                    reservation.DeductTransactionId = deductTransaction.Id;
                }

                reservation.Status = "Captured";
                reservation.CapturedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                if (ownsTransaction)
                    await transaction!.CommitAsync(cancellationToken);
            }
            catch
            {
                if (ownsTransaction && transaction != null)
                    await transaction.RollbackAsync(cancellationToken);
                throw;
            }
            finally
            {
                if (ownsTransaction && transaction != null)
                    await transaction.DisposeAsync();
            }
        }

        public async Task RefundFeatureReservationAsync(
            Guid userId,
            Guid referenceId,
            string reasonCode,
            CancellationToken cancellationToken = default)
        {
            if (userId == Guid.Empty || referenceId == Guid.Empty)
                return;

            var (transaction, ownsTransaction) = await BeginReservationTransactionAsync(cancellationToken);
            try
            {
                var reservation = await _reservationRepository.GetByReferenceForUpdateAsync(referenceId, cancellationToken);
                if (reservation == null || reservation.UserId != userId || reservation.Status == "Released" || reservation.Status == "Refunded")
                {
                    if (ownsTransaction)
                        await transaction!.CommitAsync(cancellationToken);
                    return;
                }

                if (reservation.Status == "Reserved")
                {
                    reservation.Status = "Released";
                    reservation.ReleasedAt = DateTime.UtcNow;
                }
                else if (reservation.Status == "Captured")
                {
                    if (reservation.Source == "Coin" && reservation.CoinAmount > 0)
                    {
                        if (!reservation.DeductTransactionId.HasValue)
                            throw new InvalidOperationException("Captured coin reservation is missing its deduction.");

                        var deduction = await _context.CreditTransactions
                            .FirstOrDefaultAsync(x => x.Id == reservation.DeductTransactionId.Value
                                                      && x.TransactionType == CreditTransactionType.DEDUCT,
                                cancellationToken);
                        if (deduction == null || deduction.ReferenceId != reservation.ReferenceId)
                            throw new InvalidOperationException("Captured coin reservation has an invalid deduction.");

                        var wallet = await LockWalletByIdAsync(deduction.WalletId, cancellationToken);
                        if (wallet == null || wallet.UserId != userId)
                            throw new InvalidOperationException("The billing wallet could not be verified.");

                        var existingRefund = await _context.CreditTransactions
                            .FirstOrDefaultAsync(x => x.TransactionType == CreditTransactionType.REFUND
                                                      && x.ReferenceId == deduction.Id,
                                cancellationToken);
                        if (existingRefund != null)
                        {
                            reservation.RefundTransactionId = existingRefund.Id;
                        }
                        else
                        {
                            wallet.Balance += Math.Abs(deduction.Amount);
                            wallet.UpdatedAt = DateTime.UtcNow;
                            _context.UserWallets.Update(wallet);
                            var refund = new CreditTransactions
                            {
                                Id = Guid.NewGuid(),
                                WalletId = wallet.Id,
                                Amount = Math.Abs(deduction.Amount),
                                TransactionType = CreditTransactionType.REFUND,
                                ReferenceId = deduction.Id,
                                Description = NormalizeReasonCode(reasonCode),
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.CreditTransactions.Add(refund);
                            reservation.RefundTransactionId = refund.Id;
                        }
                    }

                    reservation.Status = "Refunded";
                    reservation.RefundedAt = DateTime.UtcNow;
                }
                else
                {
                    throw new InvalidOperationException("Billing reservation is in an invalid state.");
                }

                await _context.SaveChangesAsync(cancellationToken);
                if (ownsTransaction)
                    await transaction!.CommitAsync(cancellationToken);
            }
            catch
            {
                if (ownsTransaction && transaction != null)
                    await transaction.RollbackAsync(cancellationToken);
                throw;
            }
            finally
            {
                if (ownsTransaction && transaction != null)
                    await transaction.DisposeAsync();
            }
        }

        private async Task<(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? Transaction, bool OwnsTransaction)> BeginReservationTransactionAsync(CancellationToken cancellationToken)
        {
            var current = _context.Database.CurrentTransaction;
            if (current != null || IsInMemoryProvider())
                return (current, false);

            return (await _context.Database.BeginTransactionAsync(cancellationToken), true);
        }

        private async Task<UserWallets> EnsureWalletAndLockAsync(Guid userId, CancellationToken cancellationToken)
        {
            if (IsInMemoryProvider())
            {
                var inMemoryWallet = await _context.UserWallets.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
                if (inMemoryWallet != null)
                    return inMemoryWallet;

                inMemoryWallet = new UserWallets { Id = Guid.NewGuid(), UserId = userId, Balance = 0, UpdatedAt = DateTime.UtcNow };
                _context.UserWallets.Add(inMemoryWallet);
                await _context.SaveChangesAsync(cancellationToken);
                return inMemoryWallet;
            }

            await _context.Database.ExecuteSqlRawAsync(
                "INSERT INTO user_wallets (id, user_id, balance, updated_at) VALUES ({0}, {1}, 0, {2}) ON CONFLICT (user_id) DO NOTHING;",
                Guid.NewGuid(), userId, DateTime.UtcNow);
            return await _context.UserWallets
                .FromSqlRaw("SELECT * FROM user_wallets WHERE user_id = {0} LIMIT 1 FOR UPDATE", userId)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("Could not obtain the billing wallet lock.");
        }

        private async Task<UserWallets?> LockWalletByIdAsync(Guid walletId, CancellationToken cancellationToken)
        {
            if (IsInMemoryProvider())
                return await _context.UserWallets.FirstOrDefaultAsync(x => x.Id == walletId, cancellationToken);

            return await _context.UserWallets
                .FromSqlRaw("SELECT * FROM user_wallets WHERE id = {0} LIMIT 1 FOR UPDATE", walletId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<int> GetActiveMatchingUsageCountAsync(
            Guid userId,
            string featureKey,
            DateTime startUtc,
            DateTime endUtc,
            Guid referenceId,
            CancellationToken cancellationToken)
        {
            var reservedCount = await _reservationRepository.CountActiveAsync(
                userId, featureKey, startUtc, endUtc, referenceId, cancellationToken);
            var legacyCount = await _context.CvJobMatchScores
                .Where(m => m.UserId == userId
                            && m.Id != referenceId
                            && m.BillingReservationId == null
                            && m.UpdatedAt >= startUtc
                            && m.UpdatedAt <= endUtc
                            && m.Status != "Failed")
                .CountAsync(cancellationToken);
            return reservedCount + legacyCount;
        }

        private async Task<int> GetCoinCostAsync(string featureKey, CancellationToken cancellationToken)
        {
            var dbFeature = await _context.CoinFeatures
                .AsNoTracking()
                .FirstOrDefaultAsync(cf => cf.FeatureKey == featureKey, cancellationToken);
            if (dbFeature != null)
                return Math.Max(0, dbFeature.CoinCost);

            var defaults = GetDefaultCosts();
            return featureKey switch
            {
                "CvJdMatching" => defaults.CvJdMatching,
                "MockInterview" => defaults.MockInterview,
                "LearningPath" => defaults.LearningPath,
                "PostJob" => defaults.PostJob,
                "UnlockCv" => defaults.UnlockCv,
                "ExtendJob" => defaults.ExtendJob,
                "PushTop" => defaults.PushTop,
                _ => 0
            };
        }

        private static int GetFeatureLimit(string featureKey, Subscriptions? subscription)
        {
            if (subscription == null || string.IsNullOrWhiteSpace(subscription.FeaturesConfig))
                return 0;

            try
            {
                var features = JsonSerializer.Deserialize<FeaturesConfigDto>(
                    subscription.FeaturesConfig,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return featureKey switch
                {
                    "CvJdMatching" => features?.CvMatchLimit ?? 0,
                    "MockInterview" => features?.MockInterviewLimit ?? 0,
                    "LearningPath" => features?.LearningPathLimit ?? features?.LearningPathSlotLimit ?? 0,
                    "PostJob" => features?.JobSlots ?? 1,
                    "UnlockCv" => features?.UnlockCvLimit ?? 0,
                    "ExtendJob" => features?.JobExtendLimit ?? 0,
                    "PushTop" => features?.PushTopLimit ?? 0,
                    _ => 0
                };
            }
            catch (JsonException)
            {
                return 0;
            }
        }

        private static FeatureReservationResult ToReservationResult(FeatureUsageReservations reservation)
            => new(
                reservation.Id,
                reservation.ReferenceId,
                reservation.FeatureKey,
                reservation.Source,
                reservation.Status,
                reservation.CoinAmount,
                reservation.DeductTransactionId);

        private static string NormalizeReasonCode(string reasonCode)
        {
            if (string.IsNullOrWhiteSpace(reasonCode))
                return "technical_failure";
            var normalized = reasonCode.Trim();
            return normalized.Length <= 64 ? normalized : normalized[..64];
        }

        private bool IsInMemoryProvider()
            => string.Equals(_context.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal);

        private Task<Guid?> RecordFeatureUsageLogAsync(Guid userId, string featureKey, string? referenceId, bool fromSubscription)
        {
            if (featureKey == "ExtendJob" || featureKey == "UnlockCv" || featureKey == "PushTop")
            {
                string actionTag = fromSubscription ? $"ConsumeFeature:{featureKey}:Sub" : $"ConsumeFeature:{featureKey}:Coin";
                var log = UserActivityLogs.Create(
                    userId,
                    "recruiter",
                    ActivityLogCategory.DATA_MUTATION,
                    "recruiter@ithunterview.com",
                    actionTag,
                    ActivityLogStatus.SUCCESS,
                    "127.0.0.1",
                    "System/FeatureUsage",
                    "JobPostings",
                    featureKey,
                    referenceId
                );
                _context.UserActivityLogs.Add(log);
                return Task.FromResult<Guid?>(log.Id);
            }
            return Task.FromResult<Guid?>(null);
        }

        private async Task<int> GetUsedCountInPeriodAsync(Guid userId, string featureKey, DateTime start, DateTime end)
        {
            switch (featureKey)
            {
                case "CvJdMatching":
                    return await _context.CvJobMatchScores
                        .Where(m => m.UserId == userId &&
                                    m.UpdatedAt >= start &&
                                    m.UpdatedAt <= end &&
                                    m.Status != "Failed")
                        .CountAsync();

                case "MockInterview":
                    return await _context.InterviewSessions
                        .Where(x => x.CandidateId == userId && x.StartedAt >= start && x.StartedAt <= end)
                        .CountAsync();

                case "LearningPath":
                    return await _context.LearningPaths
                        .Where(x => x.CandidateId == userId && x.CreatedAt >= start && x.CreatedAt <= end)
                        .CountAsync();

                case "PostJob":
                    return await _context.JobPostings
                        .Where(x => x.RecruiterId == userId && 
                                    x.Status == Domain.Enums.JobStatus.PUBLISHED && 
                                    !x.IsBanned &&
                                    x.DeletedAt == null &&
                                    (!x.ExpiresAt.HasValue || x.ExpiresAt.Value >= DateTime.UtcNow))
                        .CountAsync();

                case "ExtendJob":
                case "UnlockCv":
                case "PushTop":
                    string targetAction = $"ConsumeFeature:{featureKey}:Sub";
                    return await _context.UserActivityLogs
                        .Where(x => x.UserId == userId && x.Action == targetAction && x.CreatedAt >= start && x.CreatedAt <= end)
                        .CountAsync();

                default:
                    return 0;
            }
        }

        private CoinFeatureCostsDto GetDefaultCosts()
        {
            return new CoinFeatureCostsDto
            {
                CvJdMatching = 1000,
                MockInterview = 2000,
                LearningPath = 500,
                UnlockCv = 3000,
                PostJob = 20000,
                ExtendJob = 10000,
                PushTop = 5000
            };
        }

        private string GetFeatureFriendlyName(string featureKey)
        {
            return featureKey switch
            {
                "CvJdMatching" => "So khớp CV-JD AI",
                "MockInterview" => "Phỏng vấn thử AI Mock Interview",
                "LearningPath" => "Tạo Learning Path",
                "UnlockCv" => "Mở khóa CV",
                "PostJob" => "Đăng tin",
                "ExtendJob" => "Gia hạn tin",
                "PushTop" => "Đẩy lên Top",
                _ => featureKey
            };
        }
    }
}
