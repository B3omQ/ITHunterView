import api from './api-client';
import type { ApiResponse, PaginatedResponse } from '@/types/api.types';
import type {
  UserDto,
  UserDetailDto,
  UserActivityLogDto,
  UpdateUserStatusDto,
  UpdateUserRoleDto,
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

  updateUserRole: (payload: { id: string; dto: UpdateUserRoleDto }) =>
    api
      .put<ApiResponse<any>>(`/api/user-governance/users/${payload.id}/role`, payload.dto)
      .then((res) => res.data),

  getPagedActivityLogs: (
    params: {
      page?: number;
      pageSize?: number;
      search?: string;
      category?: string;
      status?: string;
    },
    signal?: AbortSignal
  ) =>
    api
      .get<ApiResponse<PaginatedResponse<UserActivityLogDto>>>('/api/v1/audit-logs', { params, signal })
      .then((res) => res.data),
};
