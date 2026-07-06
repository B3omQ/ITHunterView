using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Wallet;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase
{
    public class WalletUseCase : IWalletUseCase
    {
        private readonly ITHunterviewContext _context;

        public WalletUseCase(ITHunterviewContext context)
        {
            _context = context;
        }

        public async Task<ResponseBase<WalletBalanceDto>> GetWalletBalanceAsync(Guid userId)
        {
            var wallet = await _context.UserWallets.FirstOrDefaultAsync(w => w.UserId == userId);
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

            var dto = new WalletBalanceDto
            {
                UserId = wallet.UserId,
                Balance = wallet.Balance
            };

            return new ResponseBase<WalletBalanceDto>(dto, "Lấy số dư ví thành công");
        }

        public async Task<ResponseBase<PagedResult<WalletTransactionDto>>> GetWalletTransactionsAsync(Guid userId, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

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

        public async Task<ResponseBase<PaymentDto>> CreatePaymentRequestAsync(Guid userId, CreatePaymentDto dto)
        {
            decimal amount = 0;
            int? creditsGranted = null;
            Guid? targetIdGuid = null;

            if (dto.TargetType == PaymentTargetType.WALLET_TOPUP)
            {
                if (!Guid.TryParse(dto.TargetId, out var coinPkgId))
                {
                    return new ResponseBase<PaymentDto>("ID gói coin không đúng định dạng Guid");
                }

                var package = await _context.CoinPackages.FirstOrDefaultAsync(p => p.Id == coinPkgId);
                if (package == null || !package.IsActive)
                {
                    return new ResponseBase<PaymentDto>("Gói nạp Coin không tồn tại hoặc không hoạt động");
                }

                amount = package.Price;
                creditsGranted = package.Coins;
                targetIdGuid = package.Id;
            }
            else if (dto.TargetType == PaymentTargetType.SUBSCRIPTION)
            {
                if (!int.TryParse(dto.TargetId, out var subId))
                {
                    return new ResponseBase<PaymentDto>("ID gói dịch vụ không đúng định dạng số nguyên");
                }

                var sub = await _context.Subscriptions.FirstOrDefaultAsync(s => s.Id == subId);
                if (sub == null || sub.Status != SubscriptionStatus.ACTIVE)
                {
                    return new ResponseBase<PaymentDto>("Gói Subscription không tồn tại hoặc không hoạt động");
                }

                amount = sub.Price;
                creditsGranted = null;
                // Ánh xạ int ID thành Guid: 00000000-0000-0000-0000-XXXXXXXXXXXX
                targetIdGuid = Guid.Parse(sub.Id.ToString().PadLeft(32, '0'));
            }
            else
            {
                return new ResponseBase<PaymentDto>("Loại thanh toán không được hỗ trợ");
            }

            var payment = new Payments
            {
                Id = Guid.NewGuid(),
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

            var paymentDto = MapToDto(payment);
            return new ResponseBase<PaymentDto>(paymentDto, "Tạo yêu cầu thanh toán thành công");
        }

        public async Task<ResponseBase<PaymentDto>> ProcessPaymentCallbackAsync(Guid actorUserId, PaymentSimulationDto simulationDto)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Id == simulationDto.PaymentId);
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
                        payment.Status = PaymentStatus.SUCCESS;
                        payment.GatewayTransactionId = simulationDto.GatewayTransactionId;
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
                                await _context.SaveChangesAsync();
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
                            if (int.TryParse(targetIdHex.TrimStart('0'), out var subId))
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

        public async Task<ResponseBase<PagedResult<PaymentDto>>> GetPagedPaymentsAsync(int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _context.Payments.OrderByDescending(p => p.CreatedAt);
            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = items.Select(MapToDto).ToList();
            var result = new PagedResult<PaymentDto>
            {
                Items = dtos,
                Total = total,
                Page = page,
                PageSize = pageSize
            };

            return new ResponseBase<PagedResult<PaymentDto>>(result, "Lấy danh sách thanh toán thành công");
        }

        private PaymentDto MapToDto(Payments p)
        {
            return new PaymentDto
            {
                Id = p.Id,
                UserId = p.UserId,
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
