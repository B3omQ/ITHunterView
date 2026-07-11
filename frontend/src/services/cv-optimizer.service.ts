import api from './api-client';
import { ApiResponse } from '@/types/api.types';
import { CvOptimization, OptimizeCvRequest } from '@/types/cv-optimizer.types';

export const cvOptimizerService = {
  optimize: (data: OptimizeCvRequest) =>
    api.post<ApiResponse<CvOptimization>>('/api/cv-optimizer/optimize', data, { timeout: 120000 }).then(r => r.data),

  getHistory: () =>
    api.get<ApiResponse<CvOptimization[]>>('/api/cv-optimizer').then(r => r.data),

  getById: (id: string) =>
    api.get<ApiResponse<CvOptimization>>(`/api/cv-optimizer/${id}`).then(r => r.data),
};
