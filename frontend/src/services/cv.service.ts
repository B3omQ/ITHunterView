import api from './api-client';
import type { ApiResponse } from '@/types/api.types';
import type { Cv, CreateCvRequest } from '@/types/cv.types';

export const cvService = {
  getMyCvs: () =>
    api.get<ApiResponse<Cv[]>>('/api/cvs').then((r) => r.data),

  createCv: (data: CreateCvRequest) =>
    api.post<ApiResponse<Cv>>('/api/cvs', data).then((r) => r.data),

  deleteCv: (id: string) =>
    api.delete<ApiResponse<string>>(`/api/cvs/${id}`).then((r) => r.data),

  matchCvJd: (data: import('@/types/cv.types').MatchJdRequest) =>
    api.post<ApiResponse<string>>('/api/cvs/match-jd', data, { timeout: 120000 }).then((r) => r.data),

  matchJobs: (id: string) =>
    api.post<ApiResponse<string>>(`/api/cvs/${id}/match-jobs`, null, { timeout: 120000 }).then((r) => r.data),

  matchJobsHardcode: (id: string) =>
    api.post<ApiResponse<string>>(`/api/cvs/${id}/match-jobs-hardcode`, null, { timeout: 120000 }).then((r) => r.data),

  getMatchResult: (jobId: string) =>
    api.get<ApiResponse<import('@/types/cv.types').MatchingResultDto>>(`/api/cvs/match-results/${jobId}`).then((r) => r.data),

  getMatchHistory: (page: number = 1, pageSize: number = 10, cvId?: string) =>
    api.get<ApiResponse<import('@/types/cv.types').PagedResult<import('@/types/cv.types').MatchHistoryDto>>>(`/api/cvs/match-history?page=${page}&pageSize=${pageSize}${cvId ? `&cvId=${cvId}` : ''}`).then((r) => r.data),

  deleteMatchHistory: (jobId: string) =>
    api.delete<ApiResponse<string>>(`/api/cvs/match-history/${jobId}`).then((r) => r.data),
};
