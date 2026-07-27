import api from './api-client';
import type { ApiResponse, PaginatedResponse } from '@/types/api.types';
import type {
  CreatePaymentDto,
  CreatePaymentResponseDto,
  WalletBalanceDto,
  WalletTransactionDto,
  PaymentDto,
} from '@/types/wallet.types';
import type { CoinPackageDto } from '@/types/subscription.types';

export const walletService = {
  getActiveCoinPackages: () =>
    api
      .get<ApiResponse<CoinPackageDto[]>>('/api/v1/wallet/coin-packages')
      .then((res) => res.data),

  createPayment: (data: CreatePaymentDto) =>
    api
      .post<ApiResponse<CreatePaymentResponseDto>>('/api/v1/wallet/pay', data)
      .then((res) => res.data),

  getBalance: () =>
    api
      .get<ApiResponse<WalletBalanceDto>>('/api/v1/wallet/balance')
      .then((res) => res.data),

  getTransactions: (params?: { page?: number; pageSize?: number }) =>
    api
      .get<ApiResponse<PaginatedResponse<WalletTransactionDto>>>('/api/v1/wallet/transactions', { params })
      .then((res) => res.data),

  getMyPayments: (params?: { page?: number; pageSize?: number; status?: string; targetType?: string }) =>
    api
      .get<ApiResponse<PaginatedResponse<PaymentDto>>>('/api/v1/wallet/my-payments', { params })
      .then((res) => res.data),

  getAdminPayments: (params?: { page?: number; pageSize?: number }) =>
    api
      .get<ApiResponse<PaginatedResponse<PaymentDto>>>('/api/v1/wallet/admin/payments', { params })
      .then((res) => res.data),
};
