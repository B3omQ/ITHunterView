import api from './api-client';
import type { ApiResponse, PaginatedResponse } from '@/types/api.types';
import type { 
  TargetRoleTemplateDto, 
  CreateTargetRoleTemplateDto, 
  UpdateTargetRoleTemplateDto,
  SfiaSkillDto
} from '@/types/master-data.types';

export const targetRoleService = {
  getPagedRoles: (
    page: number = 1,
    pageSize: number = 10,
    search?: string
  ) => {
    const params: any = { page, pageSize };
    if (search) params.search = search;
    return api
      .get<ApiResponse<PaginatedResponse<TargetRoleTemplateDto>>>('/api/master-data/target-roles', { params })
      .then((res) => res.data);
  },

  createRole: (dto: CreateTargetRoleTemplateDto) =>
    api
      .post<ApiResponse<TargetRoleTemplateDto>>('/api/master-data/target-roles', dto)
      .then((res) => res.data),

  updateRole: (id: string, dto: UpdateTargetRoleTemplateDto) =>
    api
      .put<ApiResponse<TargetRoleTemplateDto>>(`/api/master-data/target-roles/${id}`, dto)
      .then((res) => res.data),

  deleteRole: (id: string) =>
    api
      .delete<ApiResponse<boolean>>(`/api/master-data/target-roles/${id}`)
      .then((res) => res.data),
  
  getAllSfiaSkills: () =>
    api
      .get<ApiResponse<SfiaSkillDto[]>>('/api/master-data/target-roles/sfia-skills')
      .then((res) => res.data),

  importTargetRoles: (file: File) => {
    const formData = new FormData();
    formData.append('file', file);
    return api
      .post<ApiResponse<any>>('/api/master-data/target-roles/import', formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      })
      .then((res) => res.data);
  }
};
