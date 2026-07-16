import api from '@/services/api-client';
import { ApiResponse } from '@/types/api.types';
import { CoinConfigResponseDto } from '@/types/coin.types';

export const coinService = {
  getPublicCoinConfig: () => {
    return api.get<ApiResponse<CoinConfigResponseDto>>('/api/v1/coin-packages').then(res => res.data);
  },
};
