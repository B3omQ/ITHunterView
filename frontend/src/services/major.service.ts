import api from './api-client';
import type { ApiResponse, PaginatedResponse } from '@/types/api.types';
import type {
  MajorDto,
  CreateMajorDto,
  UpdateMajorDto,
} from '@/types/master-data.types';

export const majorService = {
  getPagedMajors: (
    params: {
      page?: number;
      pageSize?: number;
      search?: string;
    },
    signal?: AbortSignal
  ) =>
    api
      .get<ApiResponse<PaginatedResponse<MajorDto>>>('/api/master-data/majors', { params, signal })
      .then((res) => res.data),

  createMajor: (dto: CreateMajorDto) =>
    api
      .post<ApiResponse<MajorDto>>('/api/master-data/majors', dto)
      .then((res) => res.data),

  updateMajor: (payload: { id: number; dto: UpdateMajorDto }) =>
    api
      .put<ApiResponse<MajorDto>>(`/api/master-data/majors/${payload.id}`, payload.dto)
      .then((res) => res.data),

  deleteMajor: (id: number) =>
    api
      .delete<ApiResponse<null>>(`/api/master-data/majors/${id}`)
      .then((res) => res.data),

  restoreMajor: (id: number) =>
    api
      .post<ApiResponse<MajorDto>>(`/api/master-data/majors/${id}/restore`)
      .then((res) => res.data),
};
