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

namespace ITHunterview.Service.UseCase
{
    public class WalletUseCase : IWalletUseCase
    {
        private readonly ITHunterviewContext _context;
        private readonly PayOSClient _payOS;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WalletUseCase> _logger;

        public WalletUseCase(
            ITHunterviewContext context,
            PayOSClient payOS,
            IConfiguration configuration,
            ILogger<WalletUseCase> logger)
        {
            _context = context;
            _payOS = payOS;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ResponseBase<WalletBalanceDto>> GetWalletBalanceAsync(Guid userId)
        {
            var wallet = await _context.UserWallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null)
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        wallet = await _context.UserWallets
                            .FromSqlRaw("SELECT * FROM user_wallets WHERE user_id = {0} LIMIT 1 FOR UPDATE", userId)
                            .FirstOrDefaultAsync();

                        if (wallet == null)
                        {
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
                        await transaction.CommitAsync();
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
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
            int? learningPathLimit = null;
            int? learningPathUsed = null;
            int? learningPathSlotLimit = null;
            int? learningPathSlotUsed = null;

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

                        if (features != null && features.Role.Equals("CANDIDATE", StringComparison.OrdinalIgnoreCase))
                        {
                            mockInterviewLimit = features.MockInterviewLimit;
                            cvMatchLimit = features.CvMatchLimit;
                            learningPathLimit = features.LearningPathLimit ?? features.LearningPathSlotLimit;
                            learningPathSlotLimit = features.LearningPathSlotLimit;

                            var start = activeSub.StartDate;
                            var end = activeSub.EndDate;

                            if (mockInterviewLimit.HasValue)
                            {
                                mockInterviewUsed = await _context.InterviewSessions
                                    .Where(x => x.CandidateId == userId && x.StartedAt >= start && x.StartedAt <= end)
                                    .CountAsync();
                            }

                            if (cvMatchLimit.HasValue)
                            {
                                cvMatchUsed = await _context.CvJobMatchScores
                                    .Where(m => m.UserId == userId && m.UpdatedAt >= start && m.UpdatedAt <= end)
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
                    }
                }
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
                LearningPathLimit = learningPathLimit,
                LearningPathUsed = learningPathUsed,
                LearningPathSlotLimit = learningPathSlotLimit,
                LearningPathSlotUsed = learningPathSlotUsed
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

                amount = sub.Price;
                creditsGranted = null;
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
                var wallet = await _context.UserWallets
                    .FromSqlRaw("SELECT * FROM user_wallets WHERE user_id = {0} LIMIT 1 FOR UPDATE", payment.UserId)
                    .FirstOrDefaultAsync();

                if (wallet == null)
                {
                    wallet = new UserWallets
                    {
                        Id = Guid.NewGuid(),
                        UserId = payment.UserId,
                        Balance = 0,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.UserWallets.Add(wallet);
                }

                wallet.Balance += payment.CreditsGranted ?? 0;
                wallet.UpdatedAt = DateTime.UtcNow;
                _context.UserWallets.Update(wallet);

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
                    }
                }
            }
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
    }
}
