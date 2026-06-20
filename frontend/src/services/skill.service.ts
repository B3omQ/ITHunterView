import api from './api-client';
import type { ApiResponse, PaginatedResponse } from '@/types/api.types';
import type {
  SkillDto,
  SkillCategoryDto,
  CreateSkillDto,
  UpdateSkillDto,
  UpdateSkillStatusDto,
  SkillStatus,
} from '@/types/master-data.types';

export const skillService = {
  getPagedSkills: (
    params: {
      page?: number;
      pageSize?: number;
      search?: string;
      categoryId?: number;
      status?: SkillStatus;
    },
    signal?: AbortSignal
  ) =>
    api
      .get<ApiResponse<PaginatedResponse<SkillDto>>>('/api/master-data/skills', { params, signal })
      .then((res) => res.data),

  getCategories: () =>
    api
      .get<ApiResponse<SkillCategoryDto[]>>('/api/master-data/skills/categories')
      .then((res) => res.data),

  createSkill: (dto: CreateSkillDto) =>
    api
      .post<ApiResponse<SkillDto>>('/api/master-data/skills', dto)
      .then((res) => res.data),

  updateSkill: (payload: { id: number; dto: UpdateSkillDto }) =>
    api
      .put<ApiResponse<SkillDto>>(`/api/master-data/skills/${payload.id}`, payload.dto)
      .then((res) => res.data),

  updateSkillStatus: (payload: { id: number; dto: UpdateSkillStatusDto }) =>
    api
      .patch<ApiResponse<SkillDto>>(`/api/master-data/skills/${payload.id}/status`, payload.dto)
      .then((res) => res.data),

  deleteSkill: (id: number) =>
    api
      .delete<ApiResponse<null>>(`/api/master-data/skills/${id}`)
      .then((res) => res.data),
};
