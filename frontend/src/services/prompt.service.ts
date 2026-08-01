import apiClient from './api-client';
import {
  ActivateCvAnalysisPromptPairDto,
  ActivateJdAnalysisPromptPairDto,
  CreatePromptVersionDto,
  CvAnalysisPromptPairDto,
  JdAnalysisPromptPairDto,
  PromptDto,
  PromptVersionDto,
} from '@/types/prompt.types';
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

  getCvAnalysisPromptPair: async () => {
    const response = await apiClient.get<ApiResponse<CvAnalysisPromptPairDto>>('/api/admin/prompts/cv-analysis');
    return response.data;
  },

  activateCvAnalysisPromptPair: async (dto: ActivateCvAnalysisPromptPairDto) => {
    const response = await apiClient.post<ApiResponse<object>>('/api/admin/prompts/cv-analysis/activate', dto);
    return response.data;
  },

  getJdAnalysisPromptPair: async () => {
    const response = await apiClient.get<ApiResponse<JdAnalysisPromptPairDto>>('/api/admin/prompts/jd-analysis');
    return response.data;
  },

  activateJdAnalysisPromptPair: async (dto: ActivateJdAnalysisPromptPairDto) => {
    const response = await apiClient.post<ApiResponse<object>>('/api/admin/prompts/jd-analysis/activate', dto);
    return response.data;
  },
};
