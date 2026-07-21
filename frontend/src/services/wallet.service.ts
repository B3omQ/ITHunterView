import api from './api-client';
import type { ApiResponse } from '@/types/api.types';
import type { PagedResult, SubscriptionDto, UpdateCoinConfigDto } from '@/types/subscription.types';
import type {
  WalletBalanceDto,
  WalletTransactionDto,
  CreatePaymentDto,
  PaymentDto,
  PaymentSimulationDto,
} from '@/types/wallet.types';

export const walletService = {
  getWalletBalance: () =>
    api
      .get<ApiResponse<WalletBalanceDto>>('/api/v1/wallet/balance')
      .then((res) => res.data),

  getWalletTransactions: (params?: { page?: number; pageSize?: number }) =>
    api
      .get<ApiResponse<PagedResult<WalletTransactionDto>>>('/api/v1/wallet/transactions', { params })
      .then((res) => res.data),

  getActiveCoinPackages: () =>
    api
      .get<ApiResponse<UpdateCoinConfigDto>>('/api/v1/wallet/coin-packages')
      .then((res) => res.data),

  getActiveSubscriptions: () =>
    api
      .get<ApiResponse<PagedResult<SubscriptionDto>>>('/api/v1/wallet/active-subscriptions')
      .then((res) => res.data),

  createPaymentRequest: (data: CreatePaymentDto) =>
    api
      .post<ApiResponse<PaymentDto>>('/api/v1/wallet/pay', data)
      .then((res) => res.data),

  getPagedPayments: (params?: { page?: number; pageSize?: number }) =>
    api
      .get<ApiResponse<PagedResult<PaymentDto>>>('/api/v1/wallet/admin/payments', { params })
      .then((res) => res.data),

  simulatePaymentCallback: (data: PaymentSimulationDto) =>
    api
      .post<ApiResponse<PaymentDto>>('/api/v1/wallet/admin/payments/simulate', data)
      .then((res) => res.data),
};
