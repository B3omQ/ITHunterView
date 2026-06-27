import api from './api-client';
import type { ApiResponse, PaginatedResponse } from '@/types/api.types';
import type {
  UserDto,
  UserDetailDto,
  UpdateUserStatusDto,
  CreateStaffDto,
} from '@/types/user-governance.types';

export const userGovernanceService = {
  getPagedUsers: (
    params: {
      page?: number;
      pageSize?: number;
      search?: string;
      roleId?: number;
      status?: string;
    },
    signal?: AbortSignal
  ) =>
    api
      .get<ApiResponse<PaginatedResponse<UserDto>>>('/api/user-governance/users', { params, signal })
      .then((res) => res.data),

  getUserDetail: (id: string, signal?: AbortSignal) =>
    api
      .get<ApiResponse<UserDetailDto>>(`/api/user-governance/users/${id}`, { signal })
      .then((res) => res.data),

  updateUserStatus: (payload: { id: string; dto: UpdateUserStatusDto }) =>
    api
      .put<ApiResponse<any>>(`/api/user-governance/users/${payload.id}/status`, payload.dto)
      .then((res) => res.data),

  createStaff: (dto: CreateStaffDto) =>
    api
      .post<ApiResponse<string>>('/api/user-governance/staff', dto)
      .then((res) => res.data),
};
