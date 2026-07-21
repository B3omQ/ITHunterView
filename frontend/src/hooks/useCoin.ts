import { useQuery } from '@tanstack/react-query';
import { coinService } from '@/services/coin.service';

export const usePublicCoinConfig = () => {
  return useQuery({
    queryKey: ['publicCoinConfig'],
    queryFn: coinService.getPublicCoinConfig,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};
