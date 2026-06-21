import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { userGovernanceService } from '@/services/user-governance.service';

export function useUsers(params: {
  page?: number;
  pageSize?: number;
  search?: string;
  roleId?: number;
  status?: string;
}) {
  return useQuery({
    queryKey: ['users', params],
    queryFn: ({ signal }) => userGovernanceService.getPagedUsers(params, signal),
  });
}

export function useUserDetail(id: string) {
  return useQuery({
    queryKey: ['user-detail', id],
    queryFn: ({ signal }) => userGovernanceService.getUserDetail(id, signal),
    enabled: !!id,
  });
}

export function useUpdateUserStatus() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: userGovernanceService.updateUserStatus,
    onSuccess: (res, variables) => {
      if (res.success) {
        queryClient.invalidateQueries({ queryKey: ['users'] });
        queryClient.invalidateQueries({ queryKey: ['user-detail', variables.id] });
        queryClient.invalidateQueries({ queryKey: ['activity-logs'] });
      }
    },
  });
}

export function useUpdateUserRole() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: userGovernanceService.updateUserRole,
    onSuccess: (res, variables) => {
      if (res.success) {
        queryClient.invalidateQueries({ queryKey: ['users'] });
        queryClient.invalidateQueries({ queryKey: ['user-detail', variables.id] });
        queryClient.invalidateQueries({ queryKey: ['activity-logs'] });
      }
    },
  });
}

