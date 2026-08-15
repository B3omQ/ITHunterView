import { useQuery } from '@tanstack/react-query';
import { walletService } from '@/services/wallet.service';

export function useAdminPayments(params?: any) {
  return useQuery({
    queryKey: ['admin-payments', params],
    queryFn: () => walletService.getAdminPayments(params),
  });
}
