import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { optimizeService } from '@/services/optimize.service';
import { toast } from 'sonner';

export function useCreateOptimizeSession() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: { cvUrl?: string; cvId?: string }) => optimizeService.createSession(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['optimize-history'] });
    },
    onError: (err: any) => {
      console.error('Failed to create optimize session:', err);
      toast.error(err?.response?.data?.message || 'Không thể tạo phiên tối ưu hóa CV. Vui lòng thử lại sau.');
    }
  });
}

export function useGetOptimizeSessionResult(sessionId: string | null) {
  return useQuery({
    queryKey: ['optimize-session', sessionId],
    queryFn: () => optimizeService.getSessionResult(sessionId!),
    enabled: !!sessionId,
  });
}

export function useGetOptimizeHistory(page = 1, pageSize = 6) {
  return useQuery({
    queryKey: ['optimize-history', page, pageSize],
    queryFn: () => optimizeService.getHistory(page, pageSize),
  });
}

export function useDeleteOptimizeHistory() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (sessionId: string) => optimizeService.deleteSession(sessionId),
    onSuccess: () => {
      toast.success('Đã xóa bản ghi lịch sử CV');
      queryClient.invalidateQueries({ queryKey: ['optimize-history'] });
    },
    onError: (err: any) => {
      toast.error(err?.response?.data?.message || 'Không thể xóa lịch sử.');
    }
  });
}
