using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Wallet;
using ITHunterview.Service.DTOs.Subscription;
using System.Text.Json;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.UseCase;
using PayOS;
using PayOS.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using ITHunterview.Service.Hubs;

namespace ITHunterview.Service.UseCase
{
    public class WalletUseCase : IWalletUseCase
    {
        private const string CustomCoinTopupPriceConfigKey = "candidate_custom_coin_price_vnd";
        private const int MaximumCustomCoinAmount = 100_000;

        private readonly ITHunterviewContext _context;
        private readonly PayOSClient _payOS;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<WalletUseCase> _logger;

        public WalletUseCase(
            ITHunterviewContext context,
            PayOSClient payOS,
            IConfiguration configuration,
            IHubContext<NotificationHub> hubContext,
            ILogger<WalletUseCase> logger)
        {
            _context = context;
            _payOS = payOS;
            _configuration = configuration;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<ResponseBase<WalletBalanceDto>> GetWalletBalanceAsync(Guid userId)
        {
            var wallet = await _context.UserWallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null)
            {
                // Atomic insert to guarantee row existence under high concurrency without raising SQL 23505 Duplicate Key
                await _context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO user_wallets (id, user_id, balance, updated_at) VALUES ({0}, {1}, 0, {2}) ON CONFLICT (user_id) DO NOTHING;",
                    Guid.NewGuid(), userId, DateTime.UtcNow);

                wallet = await _context.UserWallets.FirstOrDefaultAsync(w => w.UserId == userId);
                if (wallet == null)
                {
                    wallet = new UserWallets { Id = Guid.NewGuid(), UserId = userId, Balance = 0, UpdatedAt = DateTime.UtcNow };
                }
            }

            var activeSub = await _context.UserSubscriptions
                .Where(us => us.UserId == userId && us.Status == UserSubscriptionStatus.ACTIVE && us.EndDate >= DateTime.UtcNow)
                .OrderByDescending(us => us.EndDate)
                .FirstOrDefaultAsync();
                
            string? activeSubName = null;
            int? mockInterviewLimit = null;
            int? mockInterviewUsed = null;
            int? cvMatchLimit = null;
            int? cvMatchUsed = null;
            int? cvOptimizeLimit = null;
            int? cvOptimizeUsed = null;
            int? learningPathLimit = null;
            int? learningPathUsed = null;
            int? learningPathSlotLimit = null;
            int? learningPathSlotUsed = null;
            int? jobSlotsLimit = null;
            int? jobSlotsUsed = null;
            int? unlockCvLimit = null;
            int? unlockCvUsed = null;
            int? jobExtendLimit = null;
            int? jobExtendUsed = null;
            int? pushTopLimit = null;
            int? pushTopUsed = null;

            var isRecruiter = await _context.RecruiterProfiles.AnyAsync(r => r.UserId == userId) || 
                              await _context.Users.Where(u => u.Id == userId && u.Role != null && u.Role.Name != null && u.Role.Name.ToLower() == "recruiter").AnyAsync();

            if (activeSub != null)
            {
                var subscription = await _context.Subscriptions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == activeSub.SubId && s.Status == SubscriptionStatus.ACTIVE);

                if (subscription != null)
                {
                    activeSubName = subscription.Name;

                    if (!string.IsNullOrEmpty(subscription.FeaturesConfig))
                    {
                        FeaturesConfigDto? features = null;
                        try
                        {
                            features = JsonSerializer.Deserialize<FeaturesConfigDto>(subscription.FeaturesConfig, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        }
                        catch
                        {
                            // Bỏ qua lỗi JSON
                        }

                        var start = activeSub.StartDate;
                        var end = activeSub.EndDate;

                        if (features != null && features.Role.Equals("CANDIDATE", StringComparison.OrdinalIgnoreCase))
                        {
                            mockInterviewLimit = features.MockInterviewLimit;
                            cvMatchLimit = features.CvMatchLimit;
                            cvOptimizeLimit = features.CvOptimizeLimit;
                            learningPathLimit = features.LearningPathLimit ?? features.LearningPathSlotLimit;
                            learningPathSlotLimit = features.LearningPathSlotLimit;

                            if (mockInterviewLimit.HasValue)
                            {
                                mockInterviewUsed = await _context.InterviewSessions
                                    .Where(x => x.CandidateId == userId && x.StartedAt >= start && x.StartedAt <= end)
                                    .CountAsync();
                            }

                            if (cvMatchLimit.HasValue)
                            {
                                cvMatchUsed = await _context.CvJobMatchScores
                                    .Where(m => m.UserId == userId &&
                                                m.UpdatedAt >= start &&
                                                m.UpdatedAt <= end &&
                                                m.Status != "Failed")
                                    .CountAsync();
                            }

                            if (cvOptimizeLimit.HasValue)
                            {
                                cvOptimizeUsed = await _context.OptimizeSessions
                                    .Where(x => x.UserId == userId && x.CreatedAt >= start && x.CreatedAt <= end)
                                    .CountAsync();
                            }

                            if (learningPathLimit.HasValue)
                            {
                                learningPathUsed = await _context.LearningPaths
                                    .Where(x => x.CandidateId == userId && x.CreatedAt >= start && x.CreatedAt <= end)
                                    .CountAsync();
                            }

                            if (learningPathSlotLimit.HasValue)
                            {
                                learningPathSlotUsed = await _context.LearningPaths
                                    .Where(x => x.CandidateId == userId)
                                    .CountAsync();
                            }
                        }
                        else if (features != null && features.Role.Equals("RECRUITER", StringComparison.OrdinalIgnoreCase))
                        {
                            jobSlotsLimit = features.JobSlots ?? 1;
                            unlockCvLimit = features.UnlockCvLimit ?? 0;
                            jobExtendLimit = features.JobExtendLimit ?? 0;
                            pushTopLimit = features.PushTopLimit ?? 0;

                            if (unlockCvLimit != 0)
                            {
                                unlockCvUsed = await _context.UserActivityLogs
                                    .Where(x => x.UserId == userId && x.Action == "ConsumeFeature:UnlockCv:Sub" && x.CreatedAt >= start && x.CreatedAt <= end)
                                    .CountAsync();
                            }

                            if (jobExtendLimit != 0)
                            {
                                jobExtendUsed = await _context.UserActivityLogs
                                    .Where(x => x.UserId == userId && x.Action == "ConsumeFeature:ExtendJob:Sub" && x.CreatedAt >= start && x.CreatedAt <= end)
                                    .CountAsync();
                            }

                            if (pushTopLimit != 0)
                            {
                                pushTopUsed = await _context.UserActivityLogs
                                    .Where(x => x.UserId == userId && x.Action == "ConsumeFeature:PushTop:Sub" && x.CreatedAt >= start && x.CreatedAt <= end)
                                    .CountAsync();
                            }
                        }
                    }
                }
            }

            // Mặc định gói Free cho Recruiter có 1 slot đăng tin active
            if (activeSub == null && isRecruiter)
            {
                jobSlotsLimit = 1;
                unlockCvLimit = 0;
                jobExtendLimit = 0;
                pushTopLimit = 0;
                unlockCvUsed = 0;
                jobExtendUsed = 0;
                pushTopUsed = 0;
            }

            if (jobSlotsLimit.HasValue)
            {
                jobSlotsUsed = await _context.JobPostings
                    .Where(x => x.RecruiterId == userId && 
                                x.Status == Domain.Enums.JobStatus.PUBLISHED && 
                                !x.IsBanned &&
                                x.DeletedAt == null &&
                                (!x.ExpiresAt.HasValue || x.ExpiresAt.Value >= DateTime.UtcNow))
                    .CountAsync();
            }

            var dto = new WalletBalanceDto
            {
                UserId = wallet.UserId,
                Balance = wallet.Balance,
                ActiveSubscriptionName = activeSubName,
                SubscriptionEndDate = activeSub?.EndDate,
                MockInterviewLimit = mockInterviewLimit,
                MockInterviewUsed = mockInterviewUsed,
                CvMatchLimit = cvMatchLimit,
                CvMatchUsed = cvMatchUsed,
                CvOptimizeLimit = cvOptimizeLimit,
                CvOptimizeUsed = cvOptimizeUsed,
                LearningPathLimit = learningPathLimit,
                LearningPathUsed = learningPathUsed,
                LearningPathSlotLimit = learningPathSlotLimit,
                LearningPathSlotUsed = learningPathSlotUsed,
                JobSlotsLimit = jobSlotsLimit,
                JobSlotsUsed = jobSlotsUsed,
                UnlockCvLimit = unlockCvLimit,
                UnlockCvUsed = unlockCvUsed,
                JobExtendLimit = jobExtendLimit,
                JobExtendUsed = jobExtendUsed,
                PushTopLimit = pushTopLimit,
                PushTopUsed = pushTopUsed
            };

            return new ResponseBase<WalletBalanceDto>(dto, "Lấy số dư ví thành công");
        }

        public async Task<ResponseBase<PagedResult<WalletTransactionDto>>> GetWalletTransactionsAsync(Guid userId, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var wallet = await _context.UserWallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null)
            {
                return new ResponseBase<PagedResult<WalletTransactionDto>>(new PagedResult<WalletTransactionDto>
                {
                    Items = new List<WalletTransactionDto>(),
                    Total = 0,
                    Page = page,
                    PageSize = pageSize
                }, "Chưa có giao dịch nào");
            }

            var query = _context.CreditTransactions
                .Where(t => t.WalletId == wallet.Id)
                .OrderByDescending(t => t.CreatedAt);

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = items.Select(t => new WalletTransactionDto
            {
                Id = t.Id,
                Amount = t.Amount,
                TransactionType = t.TransactionType.ToString(),
                ReferenceId = t.ReferenceId,
                Description = t.Description,
                CreatedAt = t.CreatedAt
            }).ToList();

            var result = new PagedResult<WalletTransactionDto>
            {
                Items = dtos,
                Total = total,
                Page = page,
                PageSize = pageSize
            };

            return new ResponseBase<PagedResult<WalletTransactionDto>>(result, "Lấy lịch sử giao dịch thành công");
        }

        public async Task<ResponseBase<CreatePaymentResponseDto>> CreatePaymentRequestAsync(Guid userId, CreatePaymentDto dto)
        {
            decimal amount = 0;
            int? creditsGranted = null;
            Guid? targetIdGuid = null;
            string descriptionText = "Thanh toan ITHunterview";

            if (dto.TargetType == PaymentTargetType.WALLET_TOPUP)
            {
                if (!Guid.TryParse(dto.TargetId, out var coinPkgId))
                {
                    return new ResponseBase<CreatePaymentResponseDto>("ID gói coin không đúng định dạng Guid");
                }

                var package = await _context.CoinPackages.FirstOrDefaultAsync(p => p.Id == coinPkgId);
                if (package == null || !package.IsActive)
                {
                    return new ResponseBase<CreatePaymentResponseDto>("Gói nạp Coin không tồn tại hoặc không hoạt động");
                }

                amount = package.Price;
                creditsGranted = package.Coins;
                targetIdGuid = package.Id;
                descriptionText = $"Nap {package.Coins} coin";
            }
            else if (dto.TargetType == PaymentTargetType.SUBSCRIPTION)
            {
                if (!int.TryParse(dto.TargetId, out var subId))
                {
                    return new ResponseBase<CreatePaymentResponseDto>("ID gói dịch vụ không đúng định dạng số nguyên");
                }

                var sub = await _context.Subscriptions.FirstOrDefaultAsync(s => s.Id == subId);
                if (sub == null || sub.Status != SubscriptionStatus.ACTIVE)
                {
                    return new ResponseBase<CreatePaymentResponseDto>("Gói Subscription không tồn tại hoặc không hoạt động");
                }

                // Check active subscription hierarchy
                var activeSub = await _context.UserSubscriptions
                    .Where(us => us.UserId == userId && us.Status == UserSubscriptionStatus.ACTIVE && us.EndDate >= DateTime.UtcNow)
                    .OrderByDescending(us => us.StartDate)
                    .FirstOrDefaultAsync();

                if (activeSub != null)
                {
                    if (activeSub.SubId == subId)
                    {
                        return new ResponseBase<CreatePaymentResponseDto>("Gói hiện tại đang sử dụng, không thể mua lại.");
                    }

                    var currentSubDetails = await _context.Subscriptions.FirstOrDefaultAsync(s => s.Id == activeSub.SubId);
                    if (currentSubDetails != null && sub.Price <= currentSubDetails.Price)
                    {
                        return new ResponseBase<CreatePaymentResponseDto>("Chỉ được mua gói cao hơn gói hiện tại.");
                    }
                }

                amount = sub.Price;
                // Parse features to get CoinCredit
                var features = System.Text.Json.JsonSerializer.Deserialize<DTOs.Subscription.FeaturesConfigDto>(sub.FeaturesConfig);
                // Snapshot coin bonus at purchase time so future package edits do not affect this payment.
                creditsGranted = features?.CoinCredit ?? 0;
                // Ánh xạ int ID thành Guid: 00000000-0000-0000-0000-XXXXXXXXXXXX
                targetIdGuid = Guid.Parse(sub.Id.ToString().PadLeft(32, '0'));
                descriptionText = $"Mua goi {sub.Name}";
            }
            else
            {
                return new ResponseBase<CreatePaymentResponseDto>("Loại thanh toán không được hỗ trợ");
            }

            // Sinh OrderCode duy nhất ngẫu nhiên
            long orderCode;
            int retryCount = 0;
            do
            {
                orderCode = Random.Shared.NextInt64(1_000_000_000L, 9_999_999_999L);
                retryCount++;
                if (retryCount > 3)
                    throw new InvalidOperationException("Không thể tạo OrderCode duy nhất sau 3 lần thử");
            } while (await _context.Payments.AnyAsync(p => p.OrderCode == orderCode));
            
            // Giới hạn description tối đa 25 ký tự theo quy định PayOS
            if (descriptionText.Length > 25)
            {
                descriptionText = descriptionText.Substring(0, 25);
            }

            var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:3000";
            var successUrl = $"{frontendUrl}/payment/success"; // Cần xử lý kết quả ở frontend nếu cần
            var cancelUrl = $"{frontendUrl}/payment/cancel";

            string checkoutUrl = "";
            string qrCode = "";

            if (dto.PaymentGateway == PaymentGateway.PAYOS)
            {
                try
                {
                    var item = new PayOS.Models.V2.PaymentRequests.PaymentLinkItem
                    {
                        Name = descriptionText,
                        Quantity = 1,
                        Price = (long)amount
                    };
                    
                    var items = new List<PayOS.Models.V2.PaymentRequests.PaymentLinkItem> { item };

                    var paymentRequest = new PayOS.Models.V2.PaymentRequests.CreatePaymentLinkRequest
                    {
                        OrderCode = orderCode,
                        Amount = (long)amount,
                        Description = descriptionText,
                        CancelUrl = cancelUrl,
                        ReturnUrl = successUrl,
                        Items = items
                    };

                    var createPaymentResult = await _payOS.PaymentRequests.CreateAsync(paymentRequest);
                    checkoutUrl = createPaymentResult.CheckoutUrl;
                    qrCode = createPaymentResult.QrCode;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi gọi API tạo link PayOS");
                    return new ResponseBase<CreatePaymentResponseDto>("Lỗi khi kết nối với cổng thanh toán PayOS");
                }
            }

            var payment = new Payments
            {
                Id = Guid.NewGuid(),
                OrderCode = orderCode,
                UserId = userId,
                Amount = amount,
                Currency = "VND",
                CreditsGranted = creditsGranted,
                PaymentGateway = dto.PaymentGateway,
                GatewayTransactionId = "",
                TargetType = dto.TargetType,
                TargetId = targetIdGuid,
                Status = PaymentStatus.PENDING,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            var responseDto = new CreatePaymentResponseDto
            {
                PaymentId = payment.Id,
                OrderCode = orderCode,
                CheckoutUrl = checkoutUrl,
                QrCode = qrCode
            };

            return new ResponseBase<CreatePaymentResponseDto>(responseDto, "Tạo yêu cầu thanh toán thành công");
        }

        public async Task<ResponseBase<CustomCoinTopupPriceDto>> GetCustomCoinTopupPriceAsync()
        {
            try
            {
                var price = await GetConfiguredCustomCoinTopupPriceAsync();
                return new ResponseBase<CustomCoinTopupPriceDto>(
                    new CustomCoinTopupPriceDto { PricePerCoinVnd = price },
                    "Lấy giá nạp Coin lẻ thành công");
            }
            catch (InvalidOperationException ex)
            {
                return new ResponseBase<CustomCoinTopupPriceDto>(ex.Message);
            }
        }

        public async Task<ResponseBase<CustomCoinTopupPriceDto>> UpdateCustomCoinTopupPriceAsync(
            CustomCoinTopupPriceDto dto,
            Guid actorUserId)
        {
            if (dto.PricePerCoinVnd <= 0)
            {
                return new ResponseBase<CustomCoinTopupPriceDto>("Giá nạp Coin lẻ phải lớn hơn 0 VND.");
            }

            var config = await _context.SystemConfigs
                .FirstOrDefaultAsync(x => x.ConfigKey == CustomCoinTopupPriceConfigKey);

            if (config == null)
            {
                _context.SystemConfigs.Add(new SystemConfigs
                {
                    ConfigKey = CustomCoinTopupPriceConfigKey,
                    ConfigValue = dto.PricePerCoinVnd.ToString(),
                    Description = "Đơn giá VND cho 1 Coin khi Candidate nạp Coin lẻ.",
                    UpdatedBy = actorUserId
                });
            }
            else
            {
                config.ConfigValue = dto.PricePerCoinVnd.ToString();
                config.Description = "Đơn giá VND cho 1 Coin khi Candidate nạp Coin lẻ.";
                config.UpdatedBy = actorUserId;
                _context.SystemConfigs.Update(config);
            }

            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceivePricingUpdate");
            return new ResponseBase<CustomCoinTopupPriceDto>(dto, "Cập nhật giá nạp Coin lẻ thành công");
        }

        public async Task<ResponseBase<CreatePaymentResponseDto>> CreateCustomCoinTopupPaymentAsync(
            Guid userId,
            CreateCustomCoinTopupDto dto)
        {
            if (dto.CoinAmount < 1 || dto.CoinAmount > MaximumCustomCoinAmount)
            {
                return new ResponseBase<CreatePaymentResponseDto>(
                    $"Số Coin nạp lẻ phải từ 1 đến {MaximumCustomCoinAmount:N0}.");
            }

            if (dto.PaymentGateway != PaymentGateway.PAYOS)
            {
                return new ResponseBase<CreatePaymentResponseDto>("Nạp Coin lẻ hiện chỉ hỗ trợ PayOS.");
            }

            var candidate = await _context.Users
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == userId);
            if (!string.Equals(candidate?.Role?.Name, "candidate", StringComparison.OrdinalIgnoreCase))
            {
                return new ResponseBase<CreatePaymentResponseDto>("Chỉ Candidate được nạp Coin lẻ.");
            }

            int pricePerCoin;
            try
            {
                pricePerCoin = await GetConfiguredCustomCoinTopupPriceAsync();
            }
            catch (InvalidOperationException ex)
            {
                return new ResponseBase<CreatePaymentResponseDto>(ex.Message);
            }

            var amount = (decimal)dto.CoinAmount * pricePerCoin;
            var descriptionText = $"Nap le {dto.CoinAmount} coin";
            var orderCode = await CreateUniqueOrderCodeAsync();
            var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:3000";

            try
            {
                var item = new PayOS.Models.V2.PaymentRequests.PaymentLinkItem
                {
                    Name = descriptionText,
                    Quantity = 1,
                    Price = (long)amount
                };
                var paymentRequest = new PayOS.Models.V2.PaymentRequests.CreatePaymentLinkRequest
                {
                    OrderCode = orderCode,
                    Amount = (long)amount,
                    Description = descriptionText,
                    CancelUrl = $"{frontendUrl}/payment/cancel",
                    ReturnUrl = $"{frontendUrl}/payment/success",
                    Items = new List<PayOS.Models.V2.PaymentRequests.PaymentLinkItem> { item }
                };

                var paymentLink = await _payOS.PaymentRequests.CreateAsync(paymentRequest);
                var payment = new Payments
                {
                    Id = Guid.NewGuid(),
                    OrderCode = orderCode,
                    UserId = userId,
                    Amount = amount,
                    Currency = "VND",
                    CreditsGranted = dto.CoinAmount,
                    PaymentGateway = dto.PaymentGateway,
                    GatewayTransactionId = string.Empty,
                    TargetType = PaymentTargetType.WALLET_TOPUP,
                    TargetId = null,
                    Status = PaymentStatus.PENDING,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                return new ResponseBase<CreatePaymentResponseDto>(new CreatePaymentResponseDto
                {
                    PaymentId = payment.Id,
                    OrderCode = orderCode,
                    CheckoutUrl = paymentLink.CheckoutUrl,
                    QrCode = paymentLink.QrCode
                }, "Tạo yêu cầu nạp Coin lẻ thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo payment nạp Coin lẻ qua PayOS");
                return new ResponseBase<CreatePaymentResponseDto>("Lỗi khi kết nối với cổng thanh toán PayOS");
            }
        }

        private async Task<int> GetConfiguredCustomCoinTopupPriceAsync()
        {
            var config = await _context.SystemConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ConfigKey == CustomCoinTopupPriceConfigKey);

            if (config == null)
            {
                throw new InvalidOperationException("Chưa cấu hình giá nạp Coin lẻ. Vui lòng liên hệ quản trị viên.");
            }

            if (!int.TryParse(config.ConfigValue, out var price) || price <= 0)
            {
                throw new InvalidOperationException("Giá nạp Coin lẻ hiện không hợp lệ. Vui lòng liên hệ quản trị viên.");
            }

            return price;
        }

        private async Task<long> CreateUniqueOrderCodeAsync()
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                var orderCode = Random.Shared.NextInt64(1_000_000_000L, 9_999_999_999L);
                if (!await _context.Payments.AnyAsync(p => p.OrderCode == orderCode))
                {
                    return orderCode;
                }
            }

            throw new InvalidOperationException("Không thể tạo OrderCode duy nhất sau 3 lần thử");
        }

        public async Task<ResponseBase<PaymentDto>> ProcessPaymentCallbackAsync(Guid actorUserId, PaymentSimulationDto simulationDto)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var payment = await _context.Payments
                        .FromSqlRaw("SELECT * FROM payments WHERE id = {0} LIMIT 1 FOR UPDATE", simulationDto.PaymentId)
                        .FirstOrDefaultAsync();
                    if (payment == null)
                    {
                        return new ResponseBase<PaymentDto>("Giao dịch thanh toán không tồn tại");
                    }

                    if (payment.Status != PaymentStatus.PENDING)
                    {
                        return new ResponseBase<PaymentDto>("Giao dịch đã được xử lý từ trước");
                    }

                    if (simulationDto.Success)
                    {
                        await _processSuccessfulPayment(payment, simulationDto.GatewayTransactionId);
                    }
                    else
                    {
                        payment.Status = PaymentStatus.FAILED;
                        payment.GatewayTransactionId = simulationDto.GatewayTransactionId;
                        payment.UpdatedAt = DateTime.UtcNow;
                        _context.Payments.Update(payment);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var paymentDto = MapToDto(payment);
                    return new ResponseBase<PaymentDto>(paymentDto, "Cập nhật kết quả thanh toán thành công");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return new ResponseBase<PaymentDto>($"Lỗi xử lý thanh toán: {ex.Message}");
                }
            }
        }

        public async Task ProcessWebhookAsync(long orderCode, string transactionDateTime)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var payment = await _context.Payments
                        .FromSqlRaw("SELECT * FROM payments WHERE order_code = {0} LIMIT 1 FOR UPDATE", orderCode)
                        .FirstOrDefaultAsync();
                    if (payment == null)
                    {
                        _logger.LogWarning("Webhook received for unknown OrderCode: {OrderCode}", orderCode);
                        return;
                    }

                    if (payment.Status != PaymentStatus.PENDING)
                    {
                        _logger.LogInformation("Webhook ignored - Payment {PaymentId} is already {Status}", payment.Id, payment.Status);
                        return;
                    }

                    await _processSuccessfulPayment(payment, orderCode.ToString());

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Lỗi khi xử lý webhook OrderCode {OrderCode}", orderCode);
                    throw; // Ném ra ngoài để Controller bắt
                }
            }
        }

        private async Task _processSuccessfulPayment(Payments payment, string gatewayTxId)
        {
            payment.Status = PaymentStatus.SUCCESS;
            payment.GatewayTransactionId = gatewayTxId;
            payment.UpdatedAt = DateTime.UtcNow;
            _context.Payments.Update(payment);

            if (payment.TargetType == PaymentTargetType.WALLET_TOPUP)
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO user_wallets (id, user_id, balance, updated_at) VALUES ({0}, {1}, 0, {2}) ON CONFLICT (user_id) DO NOTHING;",
                    Guid.NewGuid(), payment.UserId, DateTime.UtcNow);

                var wallet = await _context.UserWallets
                    .FromSqlRaw("SELECT * FROM user_wallets WHERE user_id = {0} LIMIT 1 FOR UPDATE", payment.UserId)
                    .FirstOrDefaultAsync();

                if (wallet != null)
                {
                    wallet.Balance += payment.CreditsGranted ?? 0;
                    wallet.UpdatedAt = DateTime.UtcNow;
                    _context.UserWallets.Update(wallet);
                }
                else
                {
                    throw new InvalidOperationException($"Could not acquire lock or find wallet for user {payment.UserId}");
                }

                var creditTx = new CreditTransactions
                {
                    Id = Guid.NewGuid(),
                    WalletId = wallet.Id,
                    Amount = payment.CreditsGranted ?? 0,
                    TransactionType = CreditTransactionType.TOPUP,
                    ReferenceId = payment.Id,
                    Description = $"Nạp thành công {payment.CreditsGranted} Coin từ cổng {payment.PaymentGateway}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.CreditTransactions.Add(creditTx);
            }
            else if (payment.TargetType == PaymentTargetType.SUBSCRIPTION)
            {
                // Lấy lại int ID từ target_id dạng Guid
                var targetIdHex = payment.TargetId.HasValue ? payment.TargetId.Value.ToString("N") : "";
                var trimmed = targetIdHex.TrimStart('0');
                if (string.IsNullOrEmpty(trimmed)) trimmed = "0"; // guard: Guid.Empty → subId = 0
                if (int.TryParse(trimmed, out var subId))
                {
                    var sub = await _context.Subscriptions.FirstOrDefaultAsync(s => s.Id == subId);
                    if (sub != null)
                    {
                        // Vô hiệu hóa các subscription cũ đang active
                        var activeSubs = await _context.UserSubscriptions
                            .Where(us => us.UserId == payment.UserId && us.Status == UserSubscriptionStatus.ACTIVE)
                            .ToListAsync();

                        foreach (var activeSub in activeSubs)
                        {
                            activeSub.Status = UserSubscriptionStatus.EXPIRED;
                            _context.UserSubscriptions.Update(activeSub);
                        }

                        // Tạo subscription mới
                        var userSub = new UserSubscriptions
                        {
                            Id = Guid.NewGuid(),
                            UserId = payment.UserId,
                            SubId = sub.Id,
                            StartDate = DateTime.UtcNow,
                            EndDate = DateTime.UtcNow.AddDays(sub.DurationDays),
                            Status = UserSubscriptionStatus.ACTIVE
                        };
                        _context.UserSubscriptions.Add(userSub);

                        await GrantSubscriptionCoinBonusAsync(payment, sub.Name);
                    }
                }
            }

            var paymentDto = MapToDto(payment);
            
            var user = await _context.Users
                .Include(u => u.CandidateProfile)
                .Include(u => u.RecruiterProfile)
                .FirstOrDefaultAsync(u => u.Id == payment.UserId);
                
            if (user != null)
            {
                paymentDto.UserName = user.RecruiterProfile?.FullName ?? 
                                     $"{user.CandidateProfile?.FirstName} {user.CandidateProfile?.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(paymentDto.UserName)) paymentDto.UserName = "Unknown User";
                
                paymentDto.UserEmail = user.Email;
            }

            await PopulateSubscriptionNamesAsync(new List<PaymentDto> { paymentDto });

            await _hubContext.Clients.All.SendAsync("ReceiveNewPayment", paymentDto);
        }

        public async Task<ResponseBase<PagedResult<PaymentDto>>> GetPagedPaymentsAsync(int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var query = _context.Payments.OrderByDescending(p => p.CreatedAt);
            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = items.Select(MapToDto).ToList();
            await PopulateSubscriptionNamesAsync(dtos);

            var userIds = dtos.Select(d => d.UserId).Distinct().ToList();
            var users = await _context.Users
                .Include(u => u.CandidateProfile)
                .Include(u => u.RecruiterProfile)
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => new { 
                    Email = u.Email, 
                    Name = u.RecruiterProfile != null ? u.RecruiterProfile.FullName : 
                           u.CandidateProfile != null ? $"{u.CandidateProfile.FirstName} {u.CandidateProfile.LastName}".Trim() : "Unknown User"
                });

            foreach(var dto in dtos)
            {
                if (users.TryGetValue(dto.UserId, out var userInfo))
                {
                    dto.UserName = string.IsNullOrWhiteSpace(userInfo.Name) ? "Unknown User" : userInfo.Name;
                    dto.UserEmail = userInfo.Email;
                }
            }
            
            var result = new PagedResult<PaymentDto>
            {
                Items = dtos,
                Total = total,
                Page = page,
                PageSize = pageSize
            };

            return new ResponseBase<PagedResult<PaymentDto>>(result, "Lấy danh sách thanh toán thành công");
        }

        public async Task<ResponseBase<PagedResult<PaymentDto>>> GetMyPaymentsAsync(Guid userId, int page, int pageSize, string? status = null, string? targetType = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var query = _context.Payments.Where(p => p.UserId == userId);
            
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<PaymentStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(p => p.Status == parsedStatus);
            }
            
            if (!string.IsNullOrEmpty(targetType) && Enum.TryParse<PaymentTargetType>(targetType, true, out var parsedTargetType))
            {
                query = query.Where(p => p.TargetType == parsedTargetType);
            }

            query = query.OrderByDescending(p => p.CreatedAt);
            
            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = items.Select(MapToDto).ToList();
            await PopulateSubscriptionNamesAsync(dtos);

            var result = new PagedResult<PaymentDto>
            {
                Items = dtos,
                Total = total,
                Page = page,
                PageSize = pageSize
            };

            return new ResponseBase<PagedResult<PaymentDto>>(result, "Lấy danh sách thanh toán cá nhân thành công");
        }

        private async Task PopulateSubscriptionNamesAsync(List<PaymentDto> dtos)
        {
            var subIds = new List<int>();
            foreach (var dto in dtos)
            {
                if (dto.TargetType == PaymentTargetType.SUBSCRIPTION.ToString() && dto.TargetId.HasValue)
                {
                    var targetIdHex = dto.TargetId.Value.ToString("N");
                    var trimmed = targetIdHex.TrimStart('0');
                    if (string.IsNullOrEmpty(trimmed)) trimmed = "0";
                    if (int.TryParse(trimmed, out var subId))
                    {
                        subIds.Add(subId);
                    }
                }
            }

            if (subIds.Any())
            {
                var subs = await _context.Subscriptions
                    .Where(s => subIds.Contains(s.Id))
                    .ToDictionaryAsync(s => s.Id, s => s.Name);

                foreach (var dto in dtos)
                {
                    if (dto.TargetType == PaymentTargetType.SUBSCRIPTION.ToString() && dto.TargetId.HasValue)
                    {
                        var targetIdHex = dto.TargetId.Value.ToString("N");
                        var trimmed = targetIdHex.TrimStart('0');
                        if (string.IsNullOrEmpty(trimmed)) trimmed = "0";
                        if (int.TryParse(trimmed, out var subId))
                        {
                            if (subs.TryGetValue(subId, out var name))
                            {
                                dto.SubscriptionName = name;
                            }
                        }
                    }
                }
            }
        }

        private PaymentDto MapToDto(Payments p)
        {
            return new PaymentDto
            {
                Id = p.Id,
                UserId = p.UserId,
                OrderCode = p.OrderCode,
                Amount = p.Amount,
                Currency = p.Currency,
                CreditsGranted = p.CreditsGranted,
                PaymentGateway = p.PaymentGateway.ToString(),
                GatewayTransactionId = p.GatewayTransactionId,
                TargetType = p.TargetType.ToString(),
                TargetId = p.TargetId,
                Status = p.Status.ToString(),
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            };
        }

        public async Task AddBonusCoinsAsync(Guid userId, int amount, string description)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO user_wallets (id, user_id, balance, updated_at) VALUES ({0}, {1}, 0, {2}) ON CONFLICT (user_id) DO NOTHING;",
                    Guid.NewGuid(), userId, DateTime.UtcNow);

                var wallet = await _context.UserWallets
                    .FromSqlRaw("SELECT * FROM user_wallets WHERE user_id = {0} LIMIT 1 FOR UPDATE", userId)
                    .FirstOrDefaultAsync();

                if (wallet != null)
                {
                    wallet.Balance += amount;
                    wallet.UpdatedAt = DateTime.UtcNow;
                    _context.UserWallets.Update(wallet);
                }
                else
                {
                    throw new InvalidOperationException($"Could not obtain lock on user_wallets for user {userId}");
                }

                var creditTx = new CreditTransactions
                {
                    Id = Guid.NewGuid(),
                    WalletId = wallet.Id,
                    Amount = amount,
                    TransactionType = ITHunterview.Domain.Enums.CreditTransactionType.BONUS,
                    Description = description,
                    CreatedAt = DateTime.UtcNow
                };
                _context.CreditTransactions.Add(creditTx);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static FeaturesConfigDto? DeserializeSubscriptionFeatures(string? featuresConfig)
        {
            if (string.IsNullOrWhiteSpace(featuresConfig))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<FeaturesConfigDto>(featuresConfig, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static bool IsSubscriptionRoleCompatible(string userRole, string subscriptionRole)
        {
            return (userRole.Equals("candidate", StringComparison.OrdinalIgnoreCase) &&
                    subscriptionRole.Equals("CANDIDATE", StringComparison.OrdinalIgnoreCase)) ||
                   (userRole.Equals("recruiter", StringComparison.OrdinalIgnoreCase) &&
                    subscriptionRole.Equals("RECRUITER", StringComparison.OrdinalIgnoreCase));
        }

        private async Task GrantSubscriptionCoinBonusAsync(Payments payment, string subscriptionName)
        {
            var bonusCoins = payment.CreditsGranted ?? 0;
            if (bonusCoins <= 0)
            {
                return;
            }

            await _context.Database.ExecuteSqlRawAsync(
                "INSERT INTO user_wallets (id, user_id, balance, updated_at) VALUES ({0}, {1}, 0, {2}) ON CONFLICT (user_id) DO NOTHING;",
                Guid.NewGuid(), payment.UserId, DateTime.UtcNow);

            var wallet = await _context.UserWallets
                .FromSqlRaw("SELECT * FROM user_wallets WHERE user_id = {0} LIMIT 1 FOR UPDATE", payment.UserId)
                .FirstOrDefaultAsync();

            if (wallet == null)
            {
                throw new InvalidOperationException($"Could not acquire lock or find wallet for user {payment.UserId}");
            }

            wallet.Balance += bonusCoins;
            wallet.UpdatedAt = DateTime.UtcNow;
            _context.UserWallets.Update(wallet);

            _context.CreditTransactions.Add(new CreditTransactions
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Amount = bonusCoins,
                TransactionType = CreditTransactionType.BONUS,
                ReferenceId = payment.Id,
                Description = $"Tặng {bonusCoins} Coin khi mua gói {subscriptionName}",
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}
