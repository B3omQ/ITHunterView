import api from './api-client';
import type { ApiResponse } from '@/types/api.types';
import type { 
  SubscriptionDto, 
  CreateSubscriptionDto, 
  UpdateSubscriptionDto, 
  SubscriptionStatus, 
  PagedResult,
  UpdateCoinConfigDto
} from '@/types/subscription.types';

export const subscriptionService = {
  getPagedSubscriptions: (params: {
    role?: string;
    status?: SubscriptionStatus;
    page?: number;
    pageSize?: number;
  }, signal?: AbortSignal) =>
    api
      .get<ApiResponse<PagedResult<SubscriptionDto>>>('/api/admin/subscriptions', { params, signal })
      .then((res) => res.data),

  getSubscriptionById: (id: number) =>
    api
      .get<ApiResponse<SubscriptionDto>>(`/api/admin/subscriptions/${id}`)
      .then((res) => res.data),

  createSubscription: (data: CreateSubscriptionDto) =>
    api
      .post<ApiResponse<SubscriptionDto>>('/api/admin/subscriptions', data)
      .then((res) => res.data),

  updateSubscription: (id: number, data: UpdateSubscriptionDto) =>
    api
      .put<ApiResponse<SubscriptionDto>>(`/api/admin/subscriptions/${id}`, data)
      .then((res) => res.data),

  updateStatus: (id: number, status: SubscriptionStatus) =>
    api
      .patch<ApiResponse<SubscriptionDto>>(`/api/admin/subscriptions/${id}/status`, { status })
      .then((res) => res.data),

  duplicateSubscription: (id: number) =>
    api
      .post<ApiResponse<SubscriptionDto>>(`/api/admin/subscriptions/${id}/duplicate`)
      .then((res) => res.data),

  getCoinConfig: () =>
    api
      .get<ApiResponse<UpdateCoinConfigDto>>('/api/admin/subscriptions/coin-config')
      .then((res) => res.data),

  updateCoinConfig: (data: UpdateCoinConfigDto) =>
    api
      .put<ApiResponse<UpdateCoinConfigDto>>('/api/admin/subscriptions/coin-config', data)
      .then((res) => res.data),
};
