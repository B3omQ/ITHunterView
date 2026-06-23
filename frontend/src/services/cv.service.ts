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
};
