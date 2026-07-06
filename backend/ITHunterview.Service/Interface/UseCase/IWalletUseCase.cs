using System;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Wallet;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface IWalletUseCase
    {
        Task<ResponseBase<WalletBalanceDto>> GetWalletBalanceAsync(Guid userId);
        Task<ResponseBase<PagedResult<WalletTransactionDto>>> GetWalletTransactionsAsync(Guid userId, int page, int pageSize);
        Task<ResponseBase<PaymentDto>> CreatePaymentRequestAsync(Guid userId, CreatePaymentDto dto);
        Task<ResponseBase<PaymentDto>> ProcessPaymentCallbackAsync(Guid actorUserId, PaymentSimulationDto simulationDto);
        Task<ResponseBase<PagedResult<PaymentDto>>> GetPagedPaymentsAsync(int page, int pageSize);
    }
}
