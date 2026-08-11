import { useMutation, useQuery, useQueryClient, UseQueryOptions } from '@tanstack/react-query';
import { walletService } from '@/services/wallet.service';
import type {
  CreateCustomCoinTopupDto,
  CreatePaymentDto,
  CustomCoinTopupPriceDto,
  WalletBalanceDto
} from '@/types/wallet.types';
import type { ApiResponse } from '@/types/api.types';

export function useBuySubscription() {
  return useMutation({
    mutationFn: (data: CreatePaymentDto) => walletService.createPayment(data),
    onSuccess: (res) => {
      if (res.success && res.data?.checkoutUrl) {
        window.location.href = res.data.checkoutUrl; // Chuyển hướng sang cổng thanh toán
      }
    },
  });
}

export function useCustomCoinTopupPrice() {
  return useQuery({
    queryKey: ['custom-coin-topup-price'],
    queryFn: walletService.getCustomCoinTopupPrice,
    staleTime: 5 * 60 * 1000,
  });
}

export function useBuyCustomCoins() {
  return useMutation({
    mutationFn: (data: CreateCustomCoinTopupDto) => walletService.createCustomCoinTopup(data),
  });
}

export function useAdminCustomCoinTopupPrice() {
  return useQuery({
    queryKey: ['admin-custom-coin-topup-price'],
    queryFn: walletService.getAdminCustomCoinTopupPrice,
  });
}

export function useUpdateAdminCustomCoinTopupPrice() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CustomCoinTopupPriceDto) => walletService.updateAdminCustomCoinTopupPrice(data),
    onSuccess: (res) => {
      if (res.success) {
        queryClient.invalidateQueries({ queryKey: ['admin-custom-coin-topup-price'] });
        queryClient.invalidateQueries({ queryKey: ['custom-coin-topup-price'] });
      }
    },
  });
}

export function useWalletBalance(options?: Omit<UseQueryOptions<ApiResponse<WalletBalanceDto>, Error, ApiResponse<WalletBalanceDto>, string[]>, 'queryKey' | 'queryFn'>) {
  return useQuery({
    queryKey: ['wallet-balance'],
    queryFn: () => walletService.getBalance(),
    ...options,
  });
}

export function useWalletTransactions(params?: { page?: number; pageSize?: number }) {
  return useQuery({
    queryKey: ['wallet-transactions', params],
    queryFn: () => walletService.getTransactions(params),
  });
}

export function useMyPayments(params?: { page?: number; pageSize?: number; status?: string; targetType?: string }) {
  return useQuery({
    queryKey: ['my-payments', params],
    queryFn: () => walletService.getMyPayments(params),
  });
}
