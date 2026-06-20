import api from './api-client';
import type { ApiResponse, PaginatedResponse } from '@/types/api.types';
import type { AuditLogDto } from '@/types/audit-log.types';

export const auditLogService = {
  getPagedAuditLogs: (
    params: {
      page?: number;
      pageSize?: number;
      search?: string;
      category?: string;
      status?: string;
      startDate?: string;
      endDate?: string;
      userId?: string;
      operationType?: string;
    },
    signal?: AbortSignal
  ) =>
    api
      .get<ApiResponse<PaginatedResponse<AuditLogDto>>>('/api/v1/audit-logs', { params, signal })
      .then((res) => res.data),

  purgeAuditLogs: (daysRetention: number) =>
    api
      .delete<ApiResponse<number>>('/api/v1/audit-logs/purge', {
        params: { daysRetention }
      })
      .then((res) => res.data),
};
