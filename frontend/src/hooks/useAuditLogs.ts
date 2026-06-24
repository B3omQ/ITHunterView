import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { auditLogService } from '@/services/audit-log.service';

export function useAuditLogs(params: {
  page?: number;
  pageSize?: number;
  search?: string;
  category?: string;
  status?: string;
  startDate?: string;
  endDate?: string;
  userId?: string;
  operationType?: string;
}) {
  return useQuery({
    queryKey: ['audit-logs', params],
    queryFn: ({ signal }) => auditLogService.getPagedAuditLogs(params, signal),
  });
}

export function usePurgeAuditLogs() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (daysRetention: number) => auditLogService.purgeAuditLogs(daysRetention),
    onSuccess: (res) => {
      if (res.success) {
        queryClient.invalidateQueries({ queryKey: ['audit-logs'] });
      }
    },
  });
}
