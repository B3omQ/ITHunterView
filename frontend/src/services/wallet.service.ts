import api from './api-client';
import type { ApiResponse, PaginatedResponse } from '@/types/api.types';
import type {
  CreatePaymentRequest,
  CreatePaymentResponse,
  WalletBalanceDto,
  WalletTransactionDto,
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
};
