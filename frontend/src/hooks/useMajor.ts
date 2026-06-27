import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { majorService } from '@/services/major.service';

export function useMajors(params: {
  page?: number;
  pageSize?: number;
  search?: string;
}) {
  return useQuery({
    queryKey: ['majors', params],
    queryFn: ({ signal }) => majorService.getPagedMajors(params, signal),
  });
}

export function useMajorTree(params: {
  page?: number;
  pageSize?: number;
  search?: string;
}) {
  return useQuery({
    queryKey: ['majors-tree', params],
    queryFn: ({ signal }) => majorService.getMajorTree(params, signal),
  });
}

export function useCreateMajor() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: majorService.createMajor,
    onSuccess: (res) => {
      if (res.success) {
        queryClient.invalidateQueries({ queryKey: ['majors'] });
        queryClient.invalidateQueries({ queryKey: ['majors-tree'] });
      }
    },
  });
}

export function useUpdateMajor() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: majorService.updateMajor,
    onSuccess: (res) => {
      if (res.success) {
        queryClient.invalidateQueries({ queryKey: ['majors'] });
        queryClient.invalidateQueries({ queryKey: ['majors-tree'] });
      }
    },
  });
}

export function useDeleteMajor() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: majorService.deleteMajor,
    onSuccess: (res) => {
      if (res.success) {
        queryClient.invalidateQueries({ queryKey: ['majors'] });
        queryClient.invalidateQueries({ queryKey: ['majors-tree'] });
      }
    },
  });
}

export function useRestoreMajor() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: majorService.restoreMajor,
    onSuccess: (res) => {
      if (res.success) {
        queryClient.invalidateQueries({ queryKey: ['majors'] });
        queryClient.invalidateQueries({ queryKey: ['majors-tree'] });
      }
    },
  });
}
