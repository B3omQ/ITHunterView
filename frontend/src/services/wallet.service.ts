import api from './api-client';
import type { ApiResponse, PaginatedResponse } from '@/types/api.types';
import type {
  CreatePaymentRequest,
  CreatePaymentResponse,
  WalletBalanceDto,
  WalletTransactionDto,
  PaymentDto,
} from '@/types/wallet.types';

export const walletService = {
  createPayment: (data: CreatePaymentRequest) =>
    api
      .post<ApiResponse<CreatePaymentResponse>>('/api/v1/wallet/pay', data)
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
};
