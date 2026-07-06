import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query';
import { subscriptionService } from '@/services/subscription.service';
import type { CreateSubscriptionDto, UpdateSubscriptionDto, SubscriptionStatus, UpdateCoinConfigDto } from '@/types/subscription.types';

export function useSubscriptions(params: {
  role?: string;
  status?: SubscriptionStatus;
  page?: number;
  pageSize?: number;
}) {
  return useQuery({
    queryKey: ['admin-subscriptions', params],
    queryFn: ({ signal }) => subscriptionService.getPagedSubscriptions(params, signal),
    placeholderData: keepPreviousData,
  });
}

export function useSubscriptionDetail(id: number, enabled = true) {
  return useQuery({
    queryKey: ['admin-subscription-detail', id],
    queryFn: () => subscriptionService.getSubscriptionById(id),
    enabled: enabled && !!id,
  });
}

export function useCreateSubscription() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateSubscriptionDto) => subscriptionService.createSubscription(data),
    onSuccess: (res) => {
      if (res.success) {
        queryClient.invalidateQueries({ queryKey: ['admin-subscriptions'] });
      }
    },
  });
}

export function useUpdateSubscription() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: UpdateSubscriptionDto }) =>
      subscriptionService.updateSubscription(id, data),
    onSuccess: (res, variables) => {
      if (res.success) {
        queryClient.invalidateQueries({ queryKey: ['admin-subscriptions'] });
        queryClient.invalidateQueries({ queryKey: ['admin-subscription-detail', variables.id] });
      }
    },
  });
}

export function useUpdateSubscriptionStatus() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, status }: { id: number; status: SubscriptionStatus }) =>
      subscriptionService.updateStatus(id, status),
    onSuccess: (res, variables) => {
      if (res.success) {
        queryClient.invalidateQueries({ queryKey: ['admin-subscriptions'] });
        queryClient.invalidateQueries({ queryKey: ['admin-subscription-detail', variables.id] });
      }
    },
  });
}

export function useDuplicateSubscription() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => subscriptionService.duplicateSubscription(id),
    onSuccess: (res) => {
      if (res.success) {
        queryClient.invalidateQueries({ queryKey: ['admin-subscriptions'] });
      }
    },
  });
}

export function useCoinConfig() {
  return useQuery({
    queryKey: ['admin-coin-config'],
    queryFn: () => subscriptionService.getCoinConfig(),
  });
}

export function useUpdateCoinConfig() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateCoinConfigDto) => subscriptionService.updateCoinConfig(data),
    onSuccess: (res) => {
      if (res.success) {
        queryClient.invalidateQueries({ queryKey: ['admin-coin-config'] });
      }
    },
  });
}
