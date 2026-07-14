import apiClient from './api-client';
import { PromptDto, PromptVersionDto, CreatePromptVersionDto } from '@/types/prompt.types';
import { ApiResponse, PaginatedResponse } from '@/types/api.types';

export const PromptService = {
  getPagedPrompts: async (page: number = 1, size: number = 10) => {
    const response = await apiClient.get<ApiResponse<PaginatedResponse<PromptDto>>>('/api/admin/prompts', {
      params: { page, size },
    });
    return response.data;
  },

  getPromptHistory: async (id: string) => {
    const response = await apiClient.get<ApiResponse<PromptDto>>(`/api/admin/prompts/${id}`);
    return response.data;
  },

  getPromptVersion: async (versionId: string) => {
    const response = await apiClient.get<ApiResponse<PromptVersionDto>>(`/api/admin/prompts/versions/${versionId}`);
    return response.data;
  },

  createPromptVersion: async (id: string, dto: CreatePromptVersionDto) => {
    const response = await apiClient.post<ApiResponse<PromptVersionDto>>(`/api/admin/prompts/${id}/versions`, dto);
    return response.data;
  },

  activatePromptVersion: async (id: string, versionId: string) => {
    const response = await apiClient.patch<ApiResponse<{ message: string }>>(`/api/admin/prompts/${id}/versions/${versionId}/activate`);
    return response.data;
  },
};
