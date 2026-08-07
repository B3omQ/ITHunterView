import api from './api-client';
import { ApiResponse } from '@/types/api.types';
import { CvOptimizationResult, OptimizeHistoryItem, PagedResult } from '@/types/optimize.types';

export const optimizeService = {
  createSession: (payload: { cvUrl?: string; cvId?: string }) =>
    api.post<ApiResponse<CvOptimizationResult>>('/api/optimize-sessions', payload, { timeout: 180000 }).then(r => r.data),

  getSessionResult: (sessionId: string) =>
    api.get<ApiResponse<CvOptimizationResult>>(`/api/optimize-sessions/${sessionId}`).then(r => r.data),

  getPreview: (sessionId: string) =>
    api.get<ApiResponse<string | null>>(`/api/optimize-sessions/${sessionId}/preview`).then(r => r.data),

  generateFile: (sessionId: string) =>
    api.post<ApiResponse<string>>(`/api/optimize-sessions/${sessionId}/generate`).then(r => r.data),

  getHistory: (page = 1, pageSize = 6) =>
    api.get<ApiResponse<PagedResult<OptimizeHistoryItem>>>(`/api/optimize-sessions/history?page=${page}&pageSize=${pageSize}`).then(r => r.data),

  deleteSession: (sessionId: string) =>
    api.delete<ApiResponse<boolean>>(`/api/optimize-sessions/${sessionId}`).then(r => r.data),
};
