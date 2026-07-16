import { useMutation, useQuery } from '@tanstack/react-query';
import { walletService } from '@/services/wallet.service';
import type { CreatePaymentRequest } from '@/types/wallet.types';

export function useBuySubscription() {
  return useMutation({
    mutationFn: (data: CreatePaymentRequest) => walletService.createPayment(data),
    onSuccess: (res) => {
      if (res.success && res.data?.checkoutUrl) {
        window.location.href = res.data.checkoutUrl; // Chuyển hướng sang cổng thanh toán
      }
    },
  });
}

export function useWalletBalance() {
  return useQuery({
    queryKey: ['wallet-balance'],
    queryFn: () => walletService.getBalance(),
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
